// dotnet 전용 — 유니티에서는 통째로 빠진다.
//
// 픽스처를 System.Text.Json으로 읽는데 유니티에는 그 어셈블리가 없고(컴파일 자체가 안 된다),
// Tests/Fixtures/*.json도 EditMode로 복사되지 않는다. 규율 2대로 역직렬화는 호스트의 일이라
// 코어가 파서를 갖지 않기 때문이고, 그래서 이 파일들은 dotnet CI에서만 돈다.
#if !UNITY_2017_1_OR_NEWER

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// <b>시나리오가 서고, 챕터가 이어진다.</b>
    ///
    /// 시나리오는 껍데기다 — 챕터를 묶고 엔딩키로 다음 챕터를 고를 뿐, 상태를 하나도
    /// 들지 않는다. 진행 상태([2])의 수명은 챕터이고, 챕터를 넘어 사는 계층([1])은
    /// 아직 서지 않았다.
    /// </summary>
    public sealed class ScenarioProgressionTests
    {
        private static ScenarioProgression Load()
        {
            ScenarioLoadResult result =
                ScenarioFixture.Load(ScenarioFixture.Read(ScenarioFixture.TwoChapters));

            Assert.That(result.Diagnostics, Is.Empty, Joined(result));

            return result.Scenario;
        }

        [Test]
        public void 손으로_조립한_시나리오가_오류_0으로_실린다()
        {
            ScenarioProgression scenario = Load();

            Assert.That(scenario.ScenarioId, Is.EqualTo("demo"));
            Assert.That(scenario.Chapters.Count, Is.EqualTo(3));
            Assert.That(scenario.StartChapter.ChapterId, Is.EqualTo("ch01"));
        }

        [Test]
        public void 시나리오는_스탯을_들지_않는다()
        {
            // 껍데기다. 스탯의 주인은 챕터이고, 시나리오에는 그 자리가 아예 없다.
            ScenarioProgression scenario = Load();

            Assert.That(scenario.GetType().GetProperty("Stats"), Is.Null,
                "시나리오에 Stats가 다시 생겼다 — 껍데기 규약이 깨졌다.");

            Assert.That(scenario.StartChapter.Stats.Select(s => s.Key),
                Is.EquivalentTo(new[] { "trust" }));
        }

        // ── 스탯의 수명은 챕터다 ────────────────────────────────────

        [Test]
        public void 시작값은_챕터가_정한다()
        {
            ScenarioProgression scenario = Load();

            Assert.That(scenario.StartChapter.CreateEntryState().GetStat("trust"), Is.EqualTo(0));
        }

        [Test]
        public void 스탯은_챕터를_넘지_않는다()
        {
            // ch01에서 간선으로 2까지 올려 놓고 넘어가도, ch02는 자기 초기값에서 선다.
            // 챕터를 넘어 사는 기억이 필요하면 그것은 [1] 영구 계층의 일이다.
            ScenarioProgression scenario = Load();
            ChapterProgression ch01 = scenario.StartChapter;

            ProgressionState atEnd = WalkChapterOne(scenario, choiceIndex: 0);
            Assert.That(atEnd.GetStat("trust"), Is.EqualTo(2));

            ScenarioAdvance advance = ScenarioTransition.Resolve(ch01, atEnd);
            Assert.That(advance.NextChapterId, Is.EqualTo("ch02_trusted"));

            Assert.That(scenario.TryGetChapter(advance.NextChapterId, out ChapterProgression next),
                Is.True);

            ProgressionState entered = next.CreateEntryState();

            Assert.That(entered.CurrentEpisodeId, Is.EqualTo("ep_a"), "다음 챕터의 시작 에피소드");
            Assert.That(entered.GetStat("trust"), Is.EqualTo(0), "넘어오지 않는다");
        }

        [Test]
        public void 경계가_챕터마다_달라도_된다()
        {
            // 같은 이름이어도 챕터가 다르면 다른 스탯이다. 예전에는 시나리오가 정의의
            // 주인이라 경계가 갈리면 오류였는데, 주인이 챕터가 되면서 그 제약이 없어졌다.
            ScenarioFixtureFile file = ScenarioFixture.Read(ScenarioFixture.TwoChapters);
            file.Chapters[0].Stats[0].Maximum = 99;

            Assert.That(ScenarioFixture.Load(file).IsValid, Is.True);
        }

        // ── 챕터를 잇는 것은 엔딩키 하나다 ──────────────────────────

        [Test]
        public void 엔딩키로_다음_챕터가_갈린다()
        {
            ScenarioProgression scenario = Load();
            ChapterProgression ch01 = scenario.StartChapter;

            // "믿는다"(+2) → 같은 엔딩키인데 trust >= 2 규칙이 먼저 맞는다.
            ScenarioAdvance a =
                ScenarioTransition.Resolve(ch01, WalkChapterOne(scenario, choiceIndex: 0));

            Assert.That(a.Kind, Is.EqualTo(ScenarioAdvanceKind.NextChapter));
            Assert.That(a.EndingKey, Is.EqualTo("ch01_done"));
            Assert.That(a.NextChapterId, Is.EqualTo("ch02_trusted"));

            // "의심한다" → 조건이 미달이라 뒤의 무조건 규칙이 맞는다.
            ScenarioAdvance b =
                ScenarioTransition.Resolve(ch01, WalkChapterOne(scenario, choiceIndex: 1));

            Assert.That(b.NextChapterId, Is.EqualTo("ch02_plain"));

            // 엔딩키는 노드가 정한다 — 두 갈래 모두 같은 키다.
            Assert.That(ch01.TryGetNode("ep_end", out EpisodeNode ending), Is.True);
            Assert.That(ending.EndingKey, Is.EqualTo("ch01_done"));
        }

        [Test]
        public void 허공으로_가는_챕터_간선을_거부한다()
        {
            ScenarioFixtureFile file = ScenarioFixture.Read(ScenarioFixture.TwoChapters);
            file.Chapters[0].EndingRules[0].NextChapterId = "없는챕터";

            Assert.That(ScenarioFixture.Load(file).HasErrors, Is.True);
        }

        [Test]
        public void 시나리오가_끝까지_걸어진다()
        {
            ScenarioProgression scenario = Load();

            ChapterProgression chapter = scenario.StartChapter;
            var visited = new List<string>();
            int guard = 0;

            while (true)
            {
                visited.Add(chapter.ChapterId);

                // 챕터마다 상태를 새로 만든다 — 호스트 루프와 같은 순서다.
                ProgressionState state = chapter.CreateEntryState();

                while (true)
                {
                    Assert.That(++guard, Is.LessThan(20), "20걸음 안에 안 끝난다 — 순환이다.");

                    ChapterAdvance inChapter = ChapterTransition.Resolve(chapter, state);

                    if (inChapter.Kind == ChapterAdvanceKind.ChapterEnded)
                        break;

                    EpisodeOption chosen = inChapter.Kind == ChapterAdvanceKind.AutoAdvance
                        ? inChapter.AutoOption
                        : inChapter.Options.First(option => option.IsSelectable).Option;

                    state = state.Commit(chapter, chosen);
                }

                ScenarioAdvance advance = ScenarioTransition.Resolve(chapter, state);

                if (advance.Kind == ScenarioAdvanceKind.ScenarioEnded)
                {
                    Assert.That(advance.EndingKey, Is.EqualTo("good_end"));
                    Assert.That(visited, Is.EqualTo(new[] { "ch01", "ch02_trusted" }));

                    return;
                }

                Assert.That(advance.Kind, Is.EqualTo(ScenarioAdvanceKind.NextChapter),
                    "막다른 노드에서 멈췄다");

                Assert.That(scenario.TryGetChapter(advance.NextChapterId, out chapter), Is.True);
            }
        }

        [Test]
        public void 엔딩키가_없는_노드에서_멈추면_DeadEnd다()
        {
            // 미완성 노드에서 멈춘 것과 작가가 의도한 종착이 화면에서 같아 보이면 안 된다.
            ScenarioFixtureFile file = ScenarioFixture.Read(ScenarioFixture.TwoChapters);

            file.Chapters[2].Nodes[0].IsChapterEndingCandidate = false;
            file.Chapters[2].Nodes[0].EndingKey = string.Empty;
            file.Chapters[2].EndingRules.Clear();

            ScenarioLoadResult loaded = ScenarioFixture.Load(file);
            Assert.That(loaded.HasErrors, Is.False, Joined(loaded));

            ScenarioProgression scenario = loaded.Scenario;

            ScenarioAdvance toPlain = ScenarioTransition.Resolve(
                scenario.StartChapter, WalkChapterOne(scenario, choiceIndex: 1));

            Assert.That(scenario.TryGetChapter(toPlain.NextChapterId, out ChapterProgression plain),
                Is.True);

            Assert.That(
                ScenarioTransition.Resolve(plain, plain.CreateEntryState()).Kind,
                Is.EqualTo(ScenarioAdvanceKind.DeadEnd));
        }

        /// <summary>ch01의 ep_01에서 선택지 하나를 골라 ep_end까지 간 상태.</summary>
        private static ProgressionState WalkChapterOne(
            ScenarioProgression scenario, int choiceIndex)
        {
            ChapterProgression chapter = scenario.StartChapter;
            ProgressionState state = chapter.CreateEntryState();

            chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode start);

            return state.Commit(chapter, start.NextOptions[choiceIndex]);
        }

        private static string Joined(ScenarioLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(d => d.ToString()));
    }
}

#endif
