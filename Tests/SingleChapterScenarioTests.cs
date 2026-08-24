// dotnet 전용 — 유니티에서는 통째로 빠진다.
//
// 픽스처를 System.Text.Json으로 읽는데 유니티에는 그 어셈블리가 없고(컴파일 자체가 안 된다),
// Tests/Fixtures/*.json도 EditMode로 복사되지 않는다. 규율 2대로 역직렬화는 호스트의 일이라
// 코어가 파서를 갖지 않기 때문이고, 그래서 이 파일들은 dotnet CI에서만 돈다.
#if !UNITY_2017_1_OR_NEWER

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
    /// <b>챕터 하나를 시나리오로 감싼다</b> — 챕터만 떼어 테스트 플레이하는 길.
    ///
    /// 툴은 챕터 JSON만 내는데 시나리오 층은 시나리오를 요구한다. 그런데 이 길은 시나리오
    /// 저작이 생긴 뒤에도 사라지지 않는다 — 작가가 ch03만 돌려 보고 싶은 때가 영원히 있다.
    /// 호스트 둘이 각자 감싸는 코드를 만들지 않게 코어에 둔다.
    ///
    /// <b>여기가 호스트 루프의 예행이다.</b> 아래 <c>Walk</c>가 하는 일이 곧 유니티
    /// <c>ProgressionDriver</c>가 할 일이고, 다른 점은 요청 수행에 시간이 걸리느냐뿐이다.
    ///
    /// ⚠ 픽스처를 읽으므로 <b>dotnet 전용</b>이다(유니티 EditMode에는 Fixtures/가 없다).
    /// </summary>
    public sealed class SingleChapterScenarioTests
    {
        // ── 끝까지 걷는다 — 호스트 루프 그대로 ──────────────────────

        [Test]
        public void 믿는_길로_걸으면_좋은끝에_닿는다()
        {
            WalkResult walk = Walk(chooseIndex: 0);

            Assert.That(walk.Outcome.Kind, Is.EqualTo(ScenarioAdvanceKind.ScenarioEnded));
            Assert.That(walk.Outcome.EndingKey, Is.EqualTo("ch01_true"));

            Assert.That(walk.FinalState.GetStat("trust"), Is.EqualTo(2));
            Assert.That(walk.FinalState.GetStat("fatigue"), Is.EqualTo(0));

            // 대사는 에피소드마다 한 번. 마지막 엔딩 노드까지 포함해 셋이다.
            Assert.That(walk.DialogueNodes,
                Is.EqualTo(new[] { "시작", "믿는길", "좋은끝" }));
        }

        [Test]
        public void 혼자_가면_쓸쓸한끝에_닿는다()
        {
            WalkResult walk = Walk(chooseIndex: 1);

            Assert.That(walk.Outcome.EndingKey, Is.EqualTo("ch01_alone"));

            Assert.That(walk.FinalState.GetStat("fatigue"), Is.EqualTo(1));
            Assert.That(walk.FinalState.GetStat("trust"), Is.EqualTo(0));

            Assert.That(walk.DialogueNodes,
                Is.EqualTo(new[] { "시작", "혼자길", "쓸쓸한끝" }));
        }

        [Test]
        public void 감싼_시나리오의_이름은_챕터에서_온다()
        {
            ScenarioProgression scenario = LoadWrapped();

            Assert.That(scenario.ScenarioId, Is.EqualTo("ch01"));
            Assert.That(scenario.StartChapterId, Is.EqualTo("ch01"));
            Assert.That(scenario.Chapters, Has.Count.EqualTo(1));

            // 스탯의 주인은 챕터다. 시나리오는 껍데기라 옮길 것이 없다.
            Assert.That(scenario.StartChapter.Stats.Select(s => s.Key),
                Is.EquivalentTo(new[] { "trust", "fatigue" }));

            // 진행 상태도 챕터가 만든다 — 수명이 챕터라서.
            Assert.That(scenario.StartChapter.CreateEntryState().CurrentEpisodeId,
                Is.EqualTo("시작"));
        }

        // ── 거부 — 조용히 감싸지 않는다 ─────────────────────────────

        [Test]
        public void 챕터_ID가_비면_거부한다()
        {
            // 시나리오 ID와 시작 챕터 ID를 둘 다 여기서 가져오므로 감쌀 이름이 없다.
            ChapterProgressionDto dto = ReadChapter();
            dto.ChapterId = "";

            ScenarioLoadResult result = ProgressionLoader.LoadAsSingleChapterScenario(dto);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics[0].Path, Is.EqualTo("ChapterId"));
        }

        [Test]
        public void null은_역직렬화_실패로_진단한다()
        {
            ScenarioLoadResult result = ProgressionLoader.LoadAsSingleChapterScenario(null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics[0].Message, Does.Contain("역직렬화"));
        }

        [Test]
        public void 스탯이_없는_챕터도_실린다()
        {
            // 스탯의 주인이 챕터가 된 뒤로, 스탯이 없는 챕터는 정당하다 —
            // 조건도 증감도 없는 챕터일 뿐이다. 오타는 ChapterInvariants가
            // "정의되지 않은 스탯"으로 잡는다.
            ChapterProgressionDto dto = ReadChapter();

            dto.Stats = null;
            StripStatUses(dto);

            Assert.That(ProgressionLoader.LoadAsSingleChapterScenario(dto).IsValid, Is.True);
        }

        [Test]
        public void 다음_챕터로_가는_규칙이_있으면_허공_간선으로_잡힌다()
        {
            // 단일 챕터인데 다음 챕터를 적었다 — 갈 곳이 없다.
            // ScenarioInvariants가 잡는 것이 맞는 동작이다. 조용히 끝으로 바꾸지 않는다.
            ChapterProgressionDto dto = ReadChapter();
            dto.EndingRules = new List<EndingRuleDto>
            {
                new EndingRuleDto
                {
                    EndingKey = "ch01_true",
                    Outcome = "NextChapter",
                    NextChapterId = "ch02",
                },
                new EndingRuleDto { EndingKey = "ch01_alone", Outcome = "ScenarioEnd" },
            };

            ScenarioLoadResult result = ProgressionLoader.LoadAsSingleChapterScenario(dto);

            Assert.That(result.IsValid, Is.False);
            Assert.That(Joined(result), Does.Contain("ch02"));
        }

        [Test]
        public void 엔딩_규칙이_없으면_정상_종착이다()
        {
            // 표본의 EndingRules는 빈 배열이다. 규칙이 아예 없는 것은 오류가 아니라
            // 단일 챕터 시나리오의 정상적인 끝이다 — ScenarioTransition이 그렇게 읽는다.
            Assert.That(ReadChapter().EndingRules, Is.Empty);
            Assert.That(LoadWrapped(), Is.Not.Null);
        }

        // ── 잔손 ────────────────────────────────────────────────────

        private sealed class WalkResult
        {
            public ScenarioAdvance Outcome;
            public ProgressionState FinalState;
            public List<string> DialogueNodes = new List<string>();
        }

        /// <summary>
        /// 호스트 루프를 그대로 돈다 — 유니티 <c>ProgressionDriver.PumpAsync</c>와 같은
        /// 순서이고, 다른 점은 요청 수행에 시간이 안 걸린다는 것뿐이다.
        ///
        /// 바깥이 챕터, 안쪽이 에피소드. 진행 상태의 수명이 챕터라 챕터마다 새로 만든다.
        /// </summary>
        private static WalkResult Walk(int chooseIndex)
        {
            var walk = new WalkResult();
            ScenarioProgression scenario = LoadWrapped();

            ChapterProgression chapter = scenario.StartChapter;
            int guard = 0;

            while (true)
            {
                ProgressionState state = chapter.CreateEntryState();

                while (true)
                {
                    Assert.That(++guard, Is.LessThan(50), "루프가 안 끝난다 — 흐름이 도는 중이다.");

                    chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode node);
                    walk.DialogueNodes.Add(node.DialogueEntryId);

                    // 판정은 대사 뒤 한 번. 아래는 이미 정해진 목록에서 고르기만 한다.
                    ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);

                    if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                    {
                        ScenarioAdvance next = ScenarioTransition.Resolve(chapter, state);

                        if (next.Kind != ScenarioAdvanceKind.NextChapter)
                        {
                            walk.Outcome = next;
                            walk.FinalState = state;

                            return walk;
                        }

                        Assert.That(scenario.TryGetChapter(next.NextChapterId, out chapter), Is.True);
                        break;
                    }

                    EpisodeOption chosen = advance.Kind == ChapterAdvanceKind.AutoAdvance
                        ? advance.AutoOption
                        : advance.Options[chooseIndex].Option;

                    state = state.Commit(chapter, chosen);
                }
            }
        }

        private static ScenarioProgression LoadWrapped()
        {
            ScenarioLoadResult result =
                ProgressionLoader.LoadAsSingleChapterScenario(ReadChapter());

            Assert.That(result.Diagnostics, Is.Empty, Joined(result));

            return result.Scenario;
        }

        private static ChapterProgressionDto ReadChapter()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "chapter-ch01-sample.json");

            Assert.That(File.Exists(path), Is.True, $"픽스처가 없다: {path}");

            return JsonSerializer.Deserialize<ChapterProgressionDto>(File.ReadAllText(path));
        }

        // 스탯을 지우면 그것을 가리키던 조건·증감이 미정의가 된다. 스탯 없는 챕터를
        // 만들려면 가리키는 쪽도 같이 걷어야 한다.
        private static void StripStatUses(ChapterProgressionDto dto)
        {
            foreach (EpisodeNodeDto node in dto.Nodes)
            {
                if (node.NextOptions == null)
                    continue;

                foreach (EpisodeOptionDto option in node.NextOptions)
                {
                    option.Conditions = null;
                    option.VisibleConditions = null;
                    option.StatChanges = null;
                }
            }
        }

        private static string Joined(ScenarioLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(d => d.ToString()));
    }
}

#endif
