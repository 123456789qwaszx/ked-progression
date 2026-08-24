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
    /// <b>이 패키지의 전부를 한 화면에.</b>
    ///
    /// 다른 테스트들이 규칙 하나씩을 고정한다면, 여기는 <b>흐름을 고정한다</b> —
    /// 무엇이 무엇을 부르고 어떤 순서로 도는지. 읽는 사람이 아키텍처를 잡는 자리이자,
    /// 흐름이 바뀌면 가장 먼저 깨지는 자리다.
    ///
    /// <code>
    ///   DTO ──[Loader]──► 스펙 ──┐
    ///                            ├──[Resolve]──► 해석 ──[고른다]──┐
    ///                     진행 ──┘                                │
    ///                       ▲                                     │
    ///                       └──────────[Commit]───────────────────┘
    ///
    ///   루프가 둘이다 — 바깥이 챕터, 안쪽이 에피소드.
    ///   진행([2])의 수명이 챕터라 챕터마다 새로 만든다.
    /// </code>
    ///
    /// <b>세 종류가 절대 안 섞인다</b> — 스펙은 콘텐츠와 함께 오고, 진행은 챕터를 사는
    /// 값이고, 해석은 이 순간뿐이라 저장되지 않는다.
    /// </summary>
    public sealed class ArchitectureWalkthroughTests
    {
        [Test]
        public void 시나리오_하나가_처음부터_끝까지_도는_모습()
        {
            var trace = new List<string>();

            // ── ① 스펙 — 데이터가 모델이 된다 ──────────────────────
            //
            // 호스트가 역직렬화하고(규율 2: JSON 파서를 안 갖는다), 로더가 검증한다.
            // 시나리오는 저작물이 아니라 호스트가 챕터들을 묶은 것이다.
            // 오류가 하나라도 있으면 아무것도 나오지 않는다 — 부분 통과는 없다.

            ScenarioLoadResult loaded =
                ScenarioFixture.Load(ScenarioFixture.Read(ScenarioFixture.TwoChapters));

            Assert.That(loaded.HasErrors, Is.False, Diagnostics(loaded));

            ScenarioProgression scenario = loaded.Scenario;

            trace.Add($"스펙   {scenario.ScenarioId} — 챕터 {scenario.Chapters.Count}");

            // ── ② 루프 ────────────────────────────────────────────

            ChapterProgression chapter = scenario.StartChapter;
            ProgressionState state = null;
            int guard = 0;

            while (true)
            {
                // 챕터 진입 — 진행 상태는 **챕터가** 만든다. 이전 챕터의 값은 안 넘어온다.
                state = chapter.CreateEntryState();

                trace.Add($"진행   {chapter.ChapterId} 진입 → {Where(chapter, state)}  {Stats(state)}");

                while (true)
                {
                    Assert.That(++guard, Is.LessThan(20), "20걸음 안에 안 끝난다.");

                    // 에피소드 층 — 판단이 먼저, 실행이 나중 (P4).
                    // 조건 판정은 **커밋 전 값**으로 한다.
                    ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);

                    if (advance.Kind == ChapterAdvanceKind.AwaitPlayerChoice)
                    {
                        int locked = advance.Options.Count(option => !option.IsSelectable);

                        trace.Add($"해석   {Where(chapter, state)}  선택 {advance.Options.Count} " +
                                  $"(잠김 {locked} · 숨김 {advance.HiddenCount})");

                        // 호스트가 고른다. 이 패키지는 목록을 만들 뿐 고르지 않는다.
                        EpisodeOption chosen =
                            advance.Options.First(option => option.IsSelectable).Option;

                        // 스탯 반영과 이동이 **한 연산**이다(트랜잭션 경계).
                        state = state.Commit(chapter, chosen);

                        trace.Add($"커밋   [{chosen.ChoiceLabel}] → {state.CurrentEpisodeId}  {Stats(state)}");
                        continue;
                    }

                    if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
                    {
                        trace.Add($"해석   {Where(chapter, state)}  자동 진행");

                        state = state.Commit(chapter, advance.AutoOption);

                        trace.Add($"커밋   (자동) → {state.CurrentEpisodeId}  {Stats(state)}");
                        continue;
                    }

                    break;
                }

                // ── 챕터 경계 — 시나리오 층이 다음을 정한다 ─────────
                //
                // 엔딩키는 **노드가 정한다**. Resolve가 그 키를 인자로 받지 않고 지금
                // 노드에서 읽으므로, 호출자가 엉뚱한 키를 넘길 자리가 없다.

                chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode last);

                trace.Add($"해석   {Where(chapter, state)}  챕터 끝 ({last.EndingKey})");

                ScenarioAdvance next = ScenarioTransition.Resolve(chapter, state);

                if (next.Kind != ScenarioAdvanceKind.NextChapter)
                {
                    trace.Add($"해석   {next.EndingKey} → 시나리오 종료");
                    break;
                }

                trace.Add($"해석   {next.EndingKey} → {next.NextChapterId}");

                Assert.That(scenario.TryGetChapter(next.NextChapterId, out chapter), Is.True);
            }

            // ── 흐름 전체가 여기 한 눈에 ───────────────────────────
            //
            // ch01에서 2까지 올린 trust가 ch02 진입에서 0으로 다시 서는 것이 보인다.
            // 그것이 "[2]의 수명은 챕터"의 전부다.

            Assert.That(trace, Is.EqualTo(new[]
            {
                "스펙   demo — 챕터 3",
                "진행   ch01 진입 → ch01/ep_01  trust=0",
                "해석   ch01/ep_01  선택 2 (잠김 0 · 숨김 0)",
                "커밋   [믿는다] → ep_end  trust=2",
                "해석   ch01/ep_end  챕터 끝 (ch01_done)",
                "해석   ch01_done → ch02_trusted",
                "진행   ch02_trusted 진입 → ch02_trusted/ep_a  trust=0",
                "해석   ch02_trusted/ep_a  자동 진행",
                "커밋   (자동) → ep_b  trust=0",
                "해석   ch02_trusted/ep_b  챕터 끝 (good_end)",
                "해석   good_end → 시나리오 종료",
            }), string.Join("\n", trace));

            // ── ③ 무엇이 상태이고 무엇이 아닌가 ────────────────────
            //
            // 진행은 챕터를 사는 값이고, 해석은 이 순간뿐이라 상태가 아니다(P3).

            Assert.That(state.GetStat("trust"), Is.EqualTo(0), "챕터를 넘으며 다시 섰다");

            // 챕터 수명 객체라 "지금 어느 챕터인가"를 들지 않는다 — 그건 굴리는 쪽이 안다.
            Assert.That(typeof(ProgressionState).GetProperty("CurrentChapterId"), Is.Null);

            Assert.That(typeof(ProgressionState).GetProperty("Options"), Is.Null);
            Assert.That(typeof(ProgressionState).GetProperty("LockedEpisodeIds"), Is.Null);
        }

        [Test]
        public void 층_사이를_지나는_것은_이름_문자열뿐이다()
        {
            // 경계면의 크기가 이 설계의 이식성 전부다. 넓히는 변경은 반려한다.
            //
            //   진행 → 대사   : EpisodeNode.DialogueEntryId   (재생할 대본)
            //   진행 → 연출   : EpisodeOption.ViaNodeId       (지나며 거쳐 갈 것)
            //   에피소드 → 챕터: EpisodeNode.EndingKey         (어느 엔딩으로 끝났나)
            //
            // 셋 다 **호스트가 푸는 이름 하나**다. 이 패키지는 셋 다 내용을 모른다.
            ScenarioProgression scenario =
                ScenarioFixture.Load(ScenarioFixture.Read(ScenarioFixture.TwoChapters)).Scenario;

            ChapterProgression chapter = scenario.StartChapter;

            Assert.That(chapter.StartNode.DialogueEntryId, Is.EqualTo("ch01_01"));

            Assert.That(chapter.Nodes.Where(node => node.IsEndingCandidate)
                    .Select(node => node.EndingKey),
                Is.EqualTo(new[] { "ch01_done" }));

            // 대사 층의 어휘가 이 패키지에 하나도 없다는 것이 위의 다른 표현이다.
            Assert.That(typeof(EpisodeNode).GetProperty("Lines"), Is.Null);
            Assert.That(typeof(EpisodeNode).GetProperty("Speaker"), Is.Null);

            // 연출 층도 마찬가지 — 이름만 오고 파라미터는 안 온다.
            Assert.That(typeof(EpisodeOption).GetProperty("ViaNodeId").PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(typeof(EpisodeOption).GetProperty("TransitionDuration"), Is.Null);
        }

        // ── 그릇 ────────────────────────────────────────────────────

        private static string Where(ChapterProgression chapter, ProgressionState state) =>
            $"{chapter.ChapterId}/{state.CurrentEpisodeId}";

        private static string Stats(ProgressionState state) =>
            string.Join(" ", state.Stats.Select(pair => $"{pair.Key}={pair.Value}"));

        private static string Diagnostics(ScenarioLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(item => item.ToString()));
    }
}

#endif
