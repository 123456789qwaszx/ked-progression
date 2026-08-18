using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// <b>게이트 G2 — 저작 도구가 실제로 낸 JSON이 오류 0으로 실린다.</b>
    ///
    /// 손으로 만든 DTO는 필드 이름이 틀려도 통과한다. 진짜 파일을 역직렬화해야
    /// <b>이름이 하나만 어긋나도 그 값이 조용히 기본값으로 들어오는 것</b>을 잡는다.
    /// 픽스처는 <c>ChapterProgressionExporter</c>가 <c>docs/chapter-graph-sample.xlsx</c>에서
    /// 낸 것을 그대로 옮긴 것이다(2026-08-18, `Stats` 신설 직후).
    ///
    /// ⚠ 여기서만 <c>System.Text.Json</c>을 쓴다 — <b>테스트 프로젝트에서만</b>이고
    /// 패키지의 의존 0은 그대로다(규율 2: 역직렬화는 호스트가 한다).
    /// 유니티 EditMode에서는 이 픽스처가 복사되지 않으므로 이 클래스는 dotnet 전용이다.
    /// </summary>
    public sealed class RealExportLoadTests
    {
        private static ChapterProgressionDto ReadFixture(
            string name = "chapter-sample-export.json")
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

            Assert.That(File.Exists(path), Is.True, $"픽스처가 없다: {path}");

            // 대소문자를 관대하게 두지 않는다 — 관대하면 이름 불일치를 못 잡는다.
            return JsonSerializer.Deserialize<ChapterProgressionDto>(File.ReadAllText(path));
        }

        [Test]
        public void 저작_쪽이_보낸_표본도_그대로_실린다()
        {
            // 두 번째 실데이터. 이쪽이 견본 워크북에서 뽑은 것과 달리 저작 쪽이 직접
            // 만들어 보낸 출력이다(java-start `docs/ch01.progression.sample.json`).
            // **독립적으로 만들어진 입력**이라 이름 규약이 우연히 맞은 것이 아님을 확인해 준다.
            //
            // ⚠ 2차 표본으로 갱신됐다 — 엔딩키가 실리기 시작해(v11) 엔딩 둘이 들어 있다.
            // 그쪽이 `ProgressionSampleGoldenTests`로 붙들고 있으므로, 규격이 바뀌면
            // 그쪽 테스트가 먼저 깨지고 이 픽스처를 다시 받아야 한다.
            ProgressionLoadResult result = ProgressionLoader.Load(ReadFixture("chapter-ch01-sample.json"));

            Assert.That(result.HasErrors, Is.False,
                string.Join(" | ", result.Diagnostics.Select(d => d.ToString())));

            ChapterProgression chapter = result.Chapter;

            // 에피소드 ID가 한글이다 — 규약이 ASCII를 전제하지 않는다.
            Assert.That(chapter.StartEpisodeId, Is.EqualTo("시작"));
            Assert.That(chapter.Nodes.Count, Is.EqualTo(5));
            Assert.That(chapter.StatsByKey.Keys, Is.EquivalentTo(new[] { "trust", "fatigue" }));

            // 두 갈래가 서로 다른 엔딩으로 간다 — 엔딩키가 실제로 실려 왔다는 증거.
            Assert.That(WalkToEnd(chapter, 0), Is.EqualTo("ch01_true"));
            Assert.That(WalkToEnd(chapter, 1), Is.EqualTo("ch01_alone"));
        }

        /// <summary>
        /// 시작에서 <paramref name="choiceIndex"/>를 고른 뒤 끝까지 걸어 엔딩키를 낸다.
        /// 전이기를 거치므로 관문·자동 진행 판정이 실제로 돈다.
        /// </summary>
        private static string WalkToEnd(ChapterProgression chapter, int choiceIndex)
        {
            ProgressionState state = chapter.CreateInitialState();
            ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);

            Assert.That(advance.Kind, Is.EqualTo(ChapterAdvanceKind.AwaitPlayerChoice));
            Assert.That(advance.Options.Count, Is.EqualTo(2));

            state = state.Commit(chapter, advance.Options[choiceIndex].Option);

            for (int guard = 0; guard < 10; guard++)
            {
                advance = ChapterTransition.Resolve(chapter, state);

                if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                {
                    return advance.EndingKey;
                }

                state = state.Commit(
                    chapter,
                    advance.Kind == ChapterAdvanceKind.AutoAdvance
                        ? advance.AutoOption
                        : advance.Options.First(option => option.IsSelectable).Option);
            }

            Assert.Fail("10걸음 안에 끝나지 않았다 — 표본이 바뀌었거나 순환이다.");
            return null;
        }

        [Test]
        public void 실제_내보내기가_오류_0으로_실린다()
        {
            ProgressionLoadResult result = ProgressionLoader.Load(ReadFixture());

            Assert.That(result.HasErrors, Is.False,
                string.Join(" | ", result.Diagnostics.Select(d => d.ToString())));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Chapter.ChapterId, Is.EqualTo("chapter-graph-sample"));
            Assert.That(result.Chapter.StartEpisodeId, Is.EqualTo("main05.01"));
            Assert.That(result.Chapter.Nodes.Count, Is.EqualTo(5));
        }

        [Test]
        public void 스탯_정의가_실려_온다()
        {
            // 이 칸이 비어 있어서 실데이터를 아예 못 싣고 있었다(§G7). 경계가 없으면
            // 툴의 도달성 증명만 clamp하며 걷고 런타임은 경계를 모른다.
            ChapterProgression chapter = ProgressionLoader.Load(ReadFixture()).Chapter;

            Assert.That(chapter.Stats.Count, Is.EqualTo(3));
            Assert.That(chapter.StatsByKey.Keys, Is.EquivalentTo(new[] { "trust", "anger", "fatigue" }));

            StatDefinition trust = chapter.StatsByKey["trust"];
            Assert.That(trust.DisplayName, Is.EqualTo("신뢰"));
            Assert.That(trust.Type, Is.EqualTo(StatType.Number), "저작 쪽 Int가 여기서는 Number다");
            Assert.That(trust.Minimum, Is.EqualTo(0));
            Assert.That(trust.Maximum, Is.EqualTo(10));
        }

        [Test]
        public void 부착도_엔딩키도_그대로_온다()
        {
            ChapterProgression chapter = ProgressionLoader.Load(ReadFixture()).Chapter;

            Assert.That(chapter.TryGetNode("attach05.02s", out EpisodeNode attachment), Is.True);
            Assert.That(attachment.Kind, Is.EqualTo(EpisodeKind.Attachment));

            Assert.That(chapter.TryGetNode("main05.end", out EpisodeNode ending), Is.True);
            Assert.That(ending.IsEndingCandidate, Is.True);
            Assert.That(ending.EndingKey, Is.EqualTo("ch05_normal"));
        }

        [Test]
        public void 문구가_빈_간선이_경고로_보고된다()
        {
            // D5 — 저작 데이터에 종류 열이 없어 문구의 유무로 판별한다.
            // 견본에는 자동 진행이 둘 있다(main05.01→02, main05.03→end).
            ProgressionLoadResult result = ProgressionLoader.Load(ReadFixture());

            ProgressionDiagnostic[] warnings = result.Diagnostics
                .Where(d => d.Severity == ProgressionDiagnosticSeverity.Warning).ToArray();

            Assert.That(warnings.Length, Is.EqualTo(1));
            Assert.That(warnings[0].Message, Does.Contain("2개"));
        }

        [Test]
        public void 실린_챕터가_끝까지_걸어진다()
        {
            // 로드되는 것과 도는 것은 다르다. 전이기를 거쳐 시작에서 엔딩까지 실제로 간다 —
            // 견본은 세 갈래를 모두 지난다(자동 진행 → 선택 → 자동 진행 → 종료).
            ChapterProgression chapter = ProgressionLoader.Load(ReadFixture()).Chapter;

            ProgressionState state = chapter.CreateInitialState();
            var seen = new System.Collections.Generic.List<ChapterAdvanceKind>();

            for (int step = 0; step < 10; step++)
            {
                ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);
                seen.Add(advance.Kind);

                if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                {
                    Assert.That(state.CurrentEpisodeId, Is.EqualTo("main05.end"));
                    Assert.That(advance.EndingKey, Is.EqualTo("ch05_normal"));
                    Assert.That(state.IsEpisodeCleared("main05.01"), Is.True);
                    Assert.That(state.IsEpisodeCleared("main05.02"), Is.True);

                    Assert.That(seen, Does.Contain(ChapterAdvanceKind.AutoAdvance));
                    Assert.That(seen, Does.Contain(ChapterAdvanceKind.AwaitPlayerChoice));
                    return;
                }

                EpisodeOption chosen = advance.Kind == ChapterAdvanceKind.AutoAdvance
                    ? advance.AutoOption
                    : advance.Options.First(option => option.IsSelectable).Option;

                state = state.Commit(chapter, chosen);
            }

            Assert.Fail("10걸음 안에 끝나지 않았다 — 견본이 바뀌었거나 순환이다.");
        }
    }
}
