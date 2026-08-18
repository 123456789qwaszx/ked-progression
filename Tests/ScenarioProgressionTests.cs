using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// <b>게이트 G1 · G3 — 시나리오가 서고, 챕터가 이어지고, 스탯이 넘어간다.</b>
    ///
    /// 마스터 플랜 §0-2의 *"Tier 2는 저장되고 챕터를 넘나든다"*가 참이 되는 자리다.
    /// 픽스처는 손으로 쓴 시나리오 JSON이다 — 툴에 시나리오 저작이 아직 없다(X4).
    /// </summary>
    public sealed class ScenarioProgressionTests
    {
        private static ScenarioProgressionDto ReadFixture()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "scenario-two-chapters.json");

            Assert.That(File.Exists(path), Is.True, $"픽스처가 없다: {path}");

            return JsonSerializer.Deserialize<ScenarioProgressionDto>(File.ReadAllText(path));
        }

        private static ScenarioProgression Load()
        {
            ScenarioLoadResult result = ProgressionLoader.Load(ReadFixture());

            Assert.That(result.HasErrors, Is.False,
                string.Join(" | ", result.Diagnostics.Select(d => d.ToString())));

            return result.Scenario;
        }

        [Test]
        public void 손으로_쓴_시나리오가_오류_0으로_실린다()
        {
            ScenarioProgression scenario = Load();

            Assert.That(scenario.ScenarioId, Is.EqualTo("demo"));
            Assert.That(scenario.Chapters.Count, Is.EqualTo(3));
            Assert.That(scenario.StartChapter.ChapterId, Is.EqualTo("ch01"));
        }

        // ── D1 — 스탯 정의의 주인은 시나리오다 ──────────────────────

        [Test]
        public void 시작값은_시나리오가_정한다()
        {
            // 픽스처의 ch01은 trust 초기값을 5로 적어 두었다. 그건 **챕터를 단독으로
            // 검증할 때의 진입 가정**이고, 실제 플레이의 시작값이 아니다.
            ScenarioProgression scenario = Load();

            Assert.That(scenario.StartChapter.Stats[0].Initial, Is.EqualTo(5), "챕터의 진입 가정");
            Assert.That(scenario.StatsByKey["trust"].Initial, Is.EqualTo(0), "시나리오의 시작값");

            Assert.That(scenario.CreateInitialState().GetStat("trust"), Is.EqualTo(0));
        }

        [Test]
        public void 스탯을_안_적은_챕터는_시나리오_것을_쓴다()
        {
            // 픽스처의 ch02_trusted에는 Stats가 없다. 조용한 기본값이 아니라 소유 규칙
            // (D1 — 정의의 주인은 시나리오다)을 그대로 적용한 것이다.
            ScenarioProgression scenario = Load();

            Assert.That(scenario.TryGetChapter("ch02_trusted", out ChapterProgression second), Is.True);
            Assert.That(second.StatsByKey.ContainsKey("trust"), Is.True);
        }

        [Test]
        public void 경계가_챕터마다_다르면_거부한다()
        {
            // 초기값과 달리 경계는 갈리면 안 된다 — 도달성 증명이 걷는 상태공간과 실제
            // 플레이가 갈린다(계약서 §G7이 경고한 상황).
            ScenarioProgressionDto dto = ReadFixture();
            dto.Chapters[0].Stats[0].Maximum = 99;

            ScenarioLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(string.Join(" | ", result.Diagnostics.Select(d => d.ToString())),
                Does.Contain("경계가 시나리오와 다르다"));
        }

        [Test]
        public void 허공으로_가는_챕터_간선을_거부한다()
        {
            ScenarioProgressionDto dto = ReadFixture();
            dto.Chapters[0].EndingRules[0].NextChapterId = "없는챕터";

            Assert.That(ProgressionLoader.Load(dto).HasErrors, Is.True);
        }

        // ── D2 · G3 — 엔딩키가 다음 챕터를 고르고 스탯이 넘어간다 ───

        [Test]
        public void 엔딩키로_다음_챕터가_갈린다()
        {
            ScenarioProgression scenario = Load();
            ChapterProgression ch01 = scenario.StartChapter;

            // "믿는다"(+2) → 같은 엔딩키인데 trust >= 2 규칙이 먼저 맞는다.
            ProgressionState trusted = WalkChapterOne(scenario, choiceIndex: 0);
            ScenarioAdvance a = ScenarioTransition.Resolve(scenario, trusted);

            Assert.That(a.Kind, Is.EqualTo(ScenarioAdvanceKind.NextChapter));
            Assert.That(a.EndingKey, Is.EqualTo("ch01_done"));
            Assert.That(a.NextChapterId, Is.EqualTo("ch02_trusted"));

            // "의심한다" → 조건이 미달이라 뒤의 무조건 규칙이 맞는다.
            ProgressionState plain = WalkChapterOne(scenario, choiceIndex: 1);
            ScenarioAdvance b = ScenarioTransition.Resolve(scenario, plain);

            Assert.That(b.NextChapterId, Is.EqualTo("ch02_plain"));

            // 엔딩키는 노드가 정한다 — 두 갈래 모두 같은 키다(D2).
            Assert.That(ch01.TryGetNode("ep_end", out EpisodeNode ending), Is.True);
            Assert.That(ending.EndingKey, Is.EqualTo("ch01_done"));
        }

        [Test]
        public void 스탯이_챕터를_넘어간다()
        {
            // ★ 마스터 플랜 §0-2가 참이 되는 지점.
            ScenarioProgression scenario = Load();

            ProgressionState state = WalkChapterOne(scenario, choiceIndex: 0);
            Assert.That(state.GetStat("trust"), Is.EqualTo(2));

            ScenarioAdvance advance = ScenarioTransition.Resolve(scenario, state);
            state = state.CommitChapterEnding(scenario, advance);

            Assert.That(state.CurrentChapterId, Is.EqualTo("ch02_trusted"));
            Assert.That(state.CurrentEpisodeId, Is.EqualTo("ep_a"), "다음 챕터의 시작 에피소드");
            Assert.That(state.GetStat("trust"), Is.EqualTo(2), "넘어와도 그대로다");
            Assert.That(state.IsChapterCleared("ch01"), Is.True);
            Assert.That(state.EndingHistory.Count, Is.EqualTo(1));
            Assert.That(state.EndingHistory[0].EndingKey, Is.EqualTo("ch01_done"));
        }

        [Test]
        public void 시나리오가_끝까지_걸어진다()
        {
            ScenarioProgression scenario = Load();
            ProgressionState state = scenario.CreateInitialState();

            var visitedChapters = new System.Collections.Generic.List<string>();

            for (int guard = 0; guard < 20; guard++)
            {
                ChapterProgression chapter;
                Assert.That(scenario.TryGetChapter(state.CurrentChapterId, out chapter), Is.True);

                if (!visitedChapters.Contains(chapter.ChapterId))
                {
                    visitedChapters.Add(chapter.ChapterId);
                }

                ChapterAdvance inChapter = ChapterTransition.Resolve(chapter, state);

                if (inChapter.Kind != ChapterAdvanceKind.ChapterEnded)
                {
                    EpisodeOption chosen = inChapter.Kind == ChapterAdvanceKind.AutoAdvance
                        ? inChapter.AutoOption
                        : inChapter.Options.First(option => option.IsSelectable).Option;

                    state = state.Commit(chapter, chosen);
                    continue;
                }

                ScenarioAdvance advance = ScenarioTransition.Resolve(scenario, state);

                if (advance.Kind == ScenarioAdvanceKind.ScenarioEnded)
                {
                    Assert.That(advance.EndingKey, Is.EqualTo("good_end"));
                    Assert.That(visitedChapters, Is.EqualTo(new[] { "ch01", "ch02_trusted" }));
                    Assert.That(state.EndingHistory.Count, Is.EqualTo(1));
                    return;
                }

                Assert.That(advance.Kind, Is.EqualTo(ScenarioAdvanceKind.NextChapter),
                    "막다른 노드에서 멈췄다");

                state = state.CommitChapterEnding(scenario, advance);
            }

            Assert.Fail("20걸음 안에 끝나지 않았다 — 순환이거나 픽스처가 바뀌었다.");
        }

        [Test]
        public void 엔딩키가_없는_노드에서_멈추면_DeadEnd다()
        {
            // 미완성 노드에서 멈춘 것과 작가가 의도한 종착이 화면에서 같아 보이면 안 된다.
            ScenarioProgressionDto dto = ReadFixture();
            dto.Chapters[2].Nodes[0].IsChapterEndingCandidate = false;
            dto.Chapters[2].Nodes[0].EndingKey = string.Empty;
            dto.Chapters[2].EndingRules.Clear();

            ScenarioLoadResult loaded = ProgressionLoader.Load(dto);
            Assert.That(loaded.HasErrors, Is.False,
                string.Join(" | ", loaded.Diagnostics.Select(d => d.ToString())));

            ProgressionState state = WalkChapterOne(loaded.Scenario, choiceIndex: 1);
            ScenarioAdvance toPlain = ScenarioTransition.Resolve(loaded.Scenario, state);
            state = state.CommitChapterEnding(loaded.Scenario, toPlain);

            Assert.That(ScenarioTransition.Resolve(loaded.Scenario, state).Kind,
                Is.EqualTo(ScenarioAdvanceKind.DeadEnd));
        }

        /// <summary>ch01의 ep_01에서 선택지 하나를 골라 ep_end까지 간 상태.</summary>
        private static ProgressionState WalkChapterOne(
            ScenarioProgression scenario, int choiceIndex)
        {
            ChapterProgression chapter = scenario.StartChapter;
            ProgressionState state = scenario.CreateInitialState();

            chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode start);

            return state.Commit(chapter, start.NextOptions[choiceIndex]);
        }
    }
}
