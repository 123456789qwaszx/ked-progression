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
    /// </code>
    ///
    /// <b>세 종류가 절대 안 섞인다</b> — 스펙은 콘텐츠와 함께 오고, 진행은 세이브에 남고,
    /// 해석은 이 순간뿐이라 저장되지 않는다.
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
            // 오류가 하나라도 있으면 아무것도 나오지 않는다 — 부분 통과는 없다.

            ScenarioLoadResult loaded = ProgressionLoader.Load(ReadScenario());

            Assert.That(loaded.HasErrors, Is.False, Diagnostics(loaded));

            ScenarioProgression scenario = loaded.Scenario;

            trace.Add($"스펙   {scenario.ScenarioId} — 챕터 {scenario.Chapters.Count}, " +
                      $"스탯 {scenario.Stats.Count}");

            // ── ② 진행 — 새 게임 ──────────────────────────────────
            //
            // 시작값은 **시나리오가 한 번만** 세운다(D1). 챕터에 적힌 초기값은
            // 도달성 증명의 진입 가정이지 실행값이 아니다.

            ProgressionState state = scenario.CreateInitialState();

            trace.Add($"진행   새 게임 → {Where(state)}  {Stats(state)}");

            // ── ③ 루프 ────────────────────────────────────────────

            for (int guard = 0; guard < 20; guard++)
            {
                scenario.TryGetChapter(state.CurrentChapterId, out ChapterProgression chapter);

                // 에피소드 층 — 판단이 먼저, 실행이 나중 (P4).
                // 조건 판정은 **커밋 전 값**으로 한다(§G6).
                ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);

                if (advance.Kind == ChapterAdvanceKind.AwaitPlayerChoice)
                {
                    int locked = advance.Options.Count(option => !option.IsSelectable);

                    trace.Add($"해석   {Where(state)}  선택 {advance.Options.Count} " +
                              $"(잠김 {locked} · 숨김 {advance.HiddenCount})");

                    // 호스트가 고른다. 이 패키지는 목록을 만들 뿐 고르지 않는다.
                    EpisodeOption chosen =
                        advance.Options.First(option => option.IsSelectable).Option;

                    // 스탯 반영 · 클리어 표시 · 이동이 **한 연산**이다(트랜잭션 경계).
                    state = state.Commit(chapter, chosen);

                    trace.Add($"커밋   [{chosen.ChoiceLabel}] → {state.CurrentEpisodeId}  {Stats(state)}");
                    continue;
                }

                if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
                {
                    trace.Add($"해석   {Where(state)}  자동 진행");

                    state = state.Commit(chapter, advance.AutoOption);

                    trace.Add($"커밋   (자동) → {state.CurrentEpisodeId}  {Stats(state)}");
                    continue;
                }

                trace.Add($"해석   {Where(state)}  챕터 끝 ({advance.EndingKey})");

                // ── 챕터 경계 — 시나리오 층이 다음을 정한다 ─────────
                //
                // 엔딩키는 **노드가 정한다**(D2). Resolve가 그 키를 인자로 받지 않고
                // 지금 노드에서 읽으므로, 호출자가 엉뚱한 키를 넘길 자리가 없다.

                ScenarioAdvance next = ScenarioTransition.Resolve(scenario, state);

                if (next.Kind != ScenarioAdvanceKind.NextChapter)
                {
                    trace.Add($"해석   {next.EndingKey} → 시나리오 종료");
                    break;
                }

                trace.Add($"해석   {next.EndingKey} → {next.NextChapterId}");

                state = state.CommitChapterEnding(scenario, next);

                trace.Add($"커밋   챕터 경계 → {Where(state)}  {Stats(state)}");
            }

            // ── 흐름 전체가 여기 한 눈에 ───────────────────────────

            Assert.That(trace, Is.EqualTo(new[]
            {
                "스펙   demo — 챕터 3, 스탯 1",
                "진행   새 게임 → ch01/ep_01  trust=0",
                "해석   ch01/ep_01  선택 2 (잠김 0 · 숨김 0)",
                "커밋   [믿는다] → ep_end  trust=2",
                "해석   ch01/ep_end  챕터 끝 (ch01_done)",
                "해석   ch01_done → ch02_trusted",
                "커밋   챕터 경계 → ch02_trusted/ep_a  trust=2",
                "해석   ch02_trusted/ep_a  자동 진행",
                "커밋   (자동) → ep_b  trust=2",
                "해석   ch02_trusted/ep_b  챕터 끝 (good_end)",
                "해석   good_end → 시나리오 종료",
            }), string.Join("\n", trace));

            // ── ④ 남는 것 / 안 남는 것 ────────────────────────────
            //
            // 진행은 세이브가 담을 내용이고, 해석은 담지 않는다(P3).
            // 구 런타임은 이 둘을 한 자루에 넣어 세이브에 옛 판정이 섞일 길을 열어 두었다.

            Assert.That(state.GetStat("trust"), Is.EqualTo(2), "스탯이 챕터를 넘어왔다");
            Assert.That(state.ClearedChapterIds, Is.EquivalentTo(new[] { "ch01" }));
            Assert.That(state.EndingHistory.Select(ending => ending.ToString()),
                Is.EqualTo(new[] { "ch01:ch01_done" }));

            Assert.That(typeof(ProgressionState).GetProperty("Options"), Is.Null);
            Assert.That(typeof(ProgressionState).GetProperty("LockedEpisodeIds"), Is.Null);
        }

        [Test]
        public void 층_사이를_지나는_것은_문자열_둘뿐이다()
        {
            // 경계면의 크기가 이 설계의 이식성 전부다. 넓히는 변경은 반려한다.
            //
            //   진행 → 대사   : EpisodeNode.DialogueEntryId   (Yarn 노드 이름 등)
            //   에피소드 → 챕터: EpisodeNode.EndingKey         (어느 엔딩으로 끝났나)
            //
            // 이 패키지는 둘 다 **내용을 모른다.** 그래서 대사 층이 무엇이든 붙는다.
            ScenarioProgression scenario = ProgressionLoader.Load(ReadScenario()).Scenario;
            ChapterProgression chapter = scenario.StartChapter;

            Assert.That(chapter.StartNode.DialogueEntryId, Is.EqualTo("ch01_01"));

            Assert.That(chapter.Nodes.Where(node => node.IsEndingCandidate)
                    .Select(node => node.EndingKey),
                Is.EqualTo(new[] { "ch01_done" }));

            // 대사 층의 어휘가 이 패키지에 하나도 없다는 것이 위 둘의 다른 표현이다.
            Assert.That(typeof(EpisodeNode).GetProperty("Lines"), Is.Null);
            Assert.That(typeof(EpisodeNode).GetProperty("Speaker"), Is.Null);
        }

        // ── 그릇 ────────────────────────────────────────────────────

        private static ScenarioProgressionDto ReadScenario()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "scenario-two-chapters.json");

            return JsonSerializer.Deserialize<ScenarioProgressionDto>(File.ReadAllText(path));
        }

        private static string Where(ProgressionState state) =>
            $"{state.CurrentChapterId}/{state.CurrentEpisodeId}";

        private static string Stats(ProgressionState state) =>
            string.Join(" ", state.Stats.Select(pair => $"{pair.Key}={pair.Value}"));

        private static string Diagnostics(ScenarioLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(item => item.ToString()));
    }
}
