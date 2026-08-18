using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// <b>게이트 G4 — 껐다 켜도 같다.</b>
    ///
    /// 어려운 것은 직렬화가 아니다. 세이브는 <b>어제 만든 콘텐츠</b>로 저장되고
    /// <b>오늘 고친 콘텐츠</b>로 열린다 — 그 사이에 스탯이 생기고, 사라지고, 경계가 바뀌고,
    /// 에피소드가 지워진다. <b>무엇을 조용히 해도 되고 무엇을 말해야 하는가</b>가 여기서
    /// 고정된다.
    /// </summary>
    public sealed class ProgressionSaveTests
    {
        private static ScenarioProgressionDto ReadDto()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "scenario-two-chapters.json");

            return JsonSerializer.Deserialize<ScenarioProgressionDto>(File.ReadAllText(path));
        }

        private static ScenarioProgression Load(ScenarioProgressionDto dto)
        {
            ScenarioLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.False,
                string.Join(" | ", result.Diagnostics.Select(d => d.ToString())));

            return result.Scenario;
        }

        /// <summary>ch01에서 "믿는다"(+2)를 골라 ep_end까지 간 뒤 ch02_trusted로 넘어간 상태.</summary>
        private static ProgressionState Played(ScenarioProgression scenario)
        {
            ChapterProgression ch01 = scenario.StartChapter;
            ProgressionState state = scenario.CreateInitialState();

            ch01.TryGetNode(state.CurrentEpisodeId, out EpisodeNode start);
            state = state.Commit(ch01, start.NextOptions[0]);

            ScenarioAdvance advance = ScenarioTransition.Resolve(scenario, state);

            return state.CommitChapterEnding(scenario, advance);
        }

        private static string Text(ProgressionRestoreResult result) =>
            string.Join(" | ", result.Diagnostics.Select(d => d.ToString()));

        // ── 왕복 ────────────────────────────────────────────────────

        [Test]
        public void 굽고_되살리면_같다()
        {
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionState before = Played(scenario);

            ProgressionSaveDto save = ProgressionSave.Capture(scenario, before);
            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.HasErrors, Is.False, Text(result));
            Assert.That(result.Diagnostics, Is.Empty, "온전한 왕복에는 할 말이 없어야 한다");

            ProgressionState after = result.State;

            Assert.That(after.CurrentChapterId, Is.EqualTo(before.CurrentChapterId));
            Assert.That(after.CurrentEpisodeId, Is.EqualTo(before.CurrentEpisodeId));
            Assert.That(after.GetStat("trust"), Is.EqualTo(before.GetStat("trust")));
            Assert.That(after.ClearedEpisodeIds, Is.EquivalentTo(before.ClearedEpisodeIds));
            Assert.That(after.ClearedChapterIds, Is.EquivalentTo(before.ClearedChapterIds));
            Assert.That(after.EndingHistory.Select(e => e.ToString()),
                Is.EqualTo(before.EndingHistory.Select(e => e.ToString())));
        }

        [Test]
        public void 되살린_상태로_이어서_걸을_수_있다()
        {
            // 왕복이 필드만 같은 게 아니라 **실제로 이어진다**는 것까지 본다.
            ScenarioProgression scenario = Load(ReadDto());

            ProgressionState resumed = ProgressionSave
                .Restore(scenario, ProgressionSave.Capture(scenario, Played(scenario)))
                .State;

            scenario.TryGetChapter(resumed.CurrentChapterId, out ChapterProgression chapter);

            ChapterAdvance advance = ChapterTransition.Resolve(chapter, resumed);

            Assert.That(advance.Kind, Is.EqualTo(ChapterAdvanceKind.AutoAdvance));

            ProgressionState next = resumed.Commit(chapter, advance.AutoOption);

            Assert.That(next.CurrentEpisodeId, Is.EqualTo("ep_b"));
            Assert.That(next.GetStat("trust"), Is.EqualTo(2), "넘어온 스탯이 그대로다");
        }

        // ── 조용해도 되는 유일한 경우 ───────────────────────────────

        [Test]
        public void 새로_생긴_스탯은_초기값으로_조용히_채운다()
        {
            // 스탯이 새로 생기는 것은 콘텐츠가 자라는 정상 경로다. 그때 옛 세이브를
            // 못 열게 하면 개발이 멈춘다 — **여기만 조용해도 된다.**
            ScenarioProgression before = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(before, Played(before));

            ScenarioProgressionDto grown = ReadDto();
            grown.Stats.Add(new StatDto
            {
                Key = "courage", DisplayName = "용기", Type = "Number",
                Initial = 3, Minimum = 0, Maximum = 5,
            });

            ProgressionRestoreResult result = ProgressionSave.Restore(Load(grown), save);

            Assert.That(result.HasErrors, Is.False, Text(result));
            Assert.That(result.Diagnostics, Is.Empty, "새 스탯을 채우는 것은 말할 일이 아니다");
            Assert.That(result.State.GetStat("courage"), Is.EqualTo(3));
            Assert.That(result.State.GetStat("trust"), Is.EqualTo(2), "있던 값은 그대로");
        }

        // ── 말해야 하는 것 — 경고 (이어할 수는 있다) ────────────────

        [Test]
        public void 정의에_없는_스탯은_버리고_말한다()
        {
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.Stats["없어진스탯"] = 7;

            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.HasErrors, Is.False, "이어할 수는 있다");
            Assert.That(result.IsValid, Is.True);
            Assert.That(Text(result), Does.Contain("없어진스탯"));
        }

        [Test]
        public void 경계_밖_값은_자르고_말한다()
        {
            // 조용히 자르면 작가가 정한 경계와 플레이어가 겪은 값이 갈린다.
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.Stats["trust"] = 99;

            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.State.GetStat("trust"), Is.EqualTo(5), "시나리오 경계로 잘린다");
            Assert.That(Text(result), Does.Contain("잘렸다"));
        }

        [Test]
        public void 사라진_클리어_기록은_버리고_말한다()
        {
            // 조용히 버리면 그것을 보던 관문이 다시 잠긴다 —
            // 플레이어에게는 "열려 있던 길이 닫힌" 것으로 보인다.
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.ClearedEpisodeIds.Add("지워진에피소드");
            save.ClearedChapterIds.Add("지워진챕터");

            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.State.IsEpisodeCleared("지워진에피소드"), Is.False);
            Assert.That(Text(result), Does.Contain("다시 잠긴다"));
        }

        // ── 막아야 하는 것 — 오류 (이어할 수 없다) ──────────────────

        [Test]
        public void 사라진_에피소드는_조용히_처음으로_보내지_않는다()
        {
            // ★ G4의 판정 문장. 여기서 조용히 시작점으로 보내면 플레이어가 어디까지
            //   왔는지가 사라지고, 아무도 그것을 모른다.
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.CurrentEpisodeId = "지워진에피소드";

            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.State, Is.Null, "반쯤 되살린 진행으로 시작하지 않는다");
            Assert.That(Text(result), Does.Contain("조용히"));
        }

        [Test]
        public void 사라진_챕터도_오류다()
        {
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.CurrentChapterId = "지워진챕터";

            Assert.That(ProgressionSave.Restore(scenario, save).IsValid, Is.False);
        }

        [Test]
        public void 다른_시나리오의_세이브를_거부한다()
        {
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.ScenarioId = "다른시나리오";

            Assert.That(ProgressionSave.Restore(scenario, save).IsValid, Is.False);
        }

        [Test]
        public void 더_새로운_세이브를_추측해_읽지_않는다()
        {
            // 앞으로 필드가 사라지거나 뜻이 바뀌면 버전이 오른다. 그때 옛 코드가
            // "읽을 수 있는 데까지" 읽으면 조용히 틀린 진행이 선다.
            ScenarioProgression scenario = Load(ReadDto());
            ProgressionSaveDto save = ProgressionSave.Capture(scenario, Played(scenario));
            save.SchemaVersion = ProgressionSave.CurrentSchemaVersion + 1;

            ProgressionRestoreResult result = ProgressionSave.Restore(scenario, save);

            Assert.That(result.IsValid, Is.False);
            Assert.That(Text(result), Does.Contain("미래 세이브"));
        }

        // ── 구조 ────────────────────────────────────────────────────

        [Test]
        public void 세이브에는_해석이_들어가지_않는다()
        {
            // P3 — 잠김·표시·도달 가능은 매번 다시 계산하는 해석이지 상태가 아니다.
            // 저장하면 콘텐츠가 바뀔 때 옛 판정이 되살아난다.
            Assert.That(typeof(ProgressionSaveDto).GetProperty("LockedEpisodeIds"), Is.Null);
            Assert.That(typeof(ProgressionSaveDto).GetProperty("VisibleEpisodeIds"), Is.Null);
            Assert.That(typeof(ProgressionSaveDto).GetProperty("ReachableEpisodeIds"), Is.Null);

            // 대사 위치도 없다 — 호스트가 자기 블록에 담는다.
            Assert.That(typeof(ProgressionSaveDto).GetProperty("LineId"), Is.Null);
            Assert.That(typeof(ProgressionSaveDto).GetProperty("NodeName"), Is.Null);
        }

        [Test]
        public void 임의의_상태를_손으로_만들_수_없다()
        {
            // 되살리기는 검증을 마친 값으로만 상태를 세운다. 공개 팩토리를 열면
            // "그래프에 없는 자리에 있는 상태"가 만들어진다.
            Assert.That(typeof(ProgressionState).GetMethod("FromSave"), Is.Null,
                "internal이어야 한다");
        }
    }
}
