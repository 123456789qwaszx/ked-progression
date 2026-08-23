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
    /// 툴은 챕터 JSON만 내는데 <see cref="EpisodeFlow"/>는 시나리오를 요구한다. 그런데
    /// 이 길은 시나리오 저작이 생긴 뒤에도 사라지지 않는다 — 작가가 ch03만 돌려 보고
    /// 싶은 때가 영원히 있다. 호스트 둘이 각자 감싸는 코드를 만들지 않게 코어에 둔다.
    ///
    /// <b>여기가 유니티 H1의 예행이다.</b> 아래 두 테스트가 하는 일이 곧
    /// <c>ProgressionDriver.RunAsync</c>가 할 일이고, 다른 점은 요청을 수행하는 데
    /// 시간이 걸리느냐뿐이다 — 코어에겐 구별이 없다.
    ///
    /// ⚠ 픽스처를 읽으므로 <b>dotnet 전용</b>이다(유니티 EditMode에는 Fixtures/가 없다).
    /// </summary>
    public sealed class SingleChapterScenarioTests
    {
        // ── 끝까지 걷는다 — 호스트 펌프 그대로 ──────────────────────

        [Test]
        public void 믿는_길로_걸으면_좋은끝에_닿는다()
        {
            WalkResult walk = Walk(chooseIndex: 0);

            Assert.That(walk.Phase, Is.EqualTo(EpisodePhase.ScenarioFinished));
            Assert.That(walk.Outcome.Kind, Is.EqualTo(ScenarioAdvanceKind.ScenarioEnded));
            Assert.That(walk.Outcome.EndingKey, Is.EqualTo("ch01_true"));

            Assert.That(walk.FinalState.GetStat("trust"), Is.EqualTo(2));
            Assert.That(walk.FinalState.GetStat("fatigue"), Is.EqualTo(0));

            // 대사는 에피소드마다 한 번. 마지막 엔딩 노드까지 포함해 넷이다.
            Assert.That(walk.DialogueNodes,
                Is.EqualTo(new[] { "시작", "믿는길", "좋은끝" }));
        }

        [Test]
        public void 혼자_가면_쓸쓸한끝에_닿는다()
        {
            WalkResult walk = Walk(chooseIndex: 1);

            Assert.That(walk.Phase, Is.EqualTo(EpisodePhase.ScenarioFinished));
            Assert.That(walk.Outcome.EndingKey, Is.EqualTo("ch01_alone"));

            Assert.That(walk.FinalState.GetStat("fatigue"), Is.EqualTo(1));
            Assert.That(walk.FinalState.GetStat("trust"), Is.EqualTo(0));

            Assert.That(walk.DialogueNodes,
                Is.EqualTo(new[] { "시작", "혼자길", "쓸쓸한끝" }));
        }

        [Test]
        public void 세이브_요청이_에피소드마다_온다()
        {
            WalkResult walk = Walk(chooseIndex: 0);

            // 커밋이 곧 저장 경계다 — 스탯만 오르고 안 옮겨 간 상태가 세이브에 실릴 수 없다.
            Assert.That(walk.SaveRequests, Has.Count.EqualTo(2));

            Assert.That(walk.SaveRequests[0].CurrentEpisodeId, Is.EqualTo("믿는길"));
            Assert.That(walk.SaveRequests[0].Stats["trust"], Is.EqualTo(2));

            Assert.That(walk.SaveRequests[1].CurrentEpisodeId, Is.EqualTo("좋은끝"));

            // 감싼 시나리오의 ID는 챕터 ID다 — 이어 하기가 이 값으로 짝을 맞춘다.
            Assert.That(walk.SaveRequests[0].ScenarioId, Is.EqualTo("ch01"));
            Assert.That(walk.SaveRequests[0].CurrentChapterId, Is.EqualTo("ch01"));
        }

        [Test]
        public void 감싼_시나리오의_이름은_챕터에서_온다()
        {
            ScenarioProgression scenario = LoadWrapped();

            Assert.That(scenario.ScenarioId, Is.EqualTo("ch01"));
            Assert.That(scenario.StartChapterId, Is.EqualTo("ch01"));
            Assert.That(scenario.Chapters, Has.Count.EqualTo(1));

            // 스탯은 챕터 것이 시나리오로 승격됐다(D1 — 주인은 시나리오다).
            Assert.That(scenario.Stats.Select(s => s.Key), Is.EquivalentTo(new[] { "trust", "fatigue" }));

            // 플레이 시작값은 시나리오가 세운다. 챕터 것은 증명 진입 가정이다.
            Assert.That(scenario.CreateInitialState().CurrentEpisodeId, Is.EqualTo("시작"));
        }

        // ── 거부 — 조용히 감싸지 않는다 ─────────────────────────────

        [Test]
        public void 스탯이_없는_챕터는_거부한다()
        {
            // 시나리오가 스탯 정의의 주인인데(D1) 줄 것이 없다. 빈 목록으로 감싸면
            // 조건이 가리키는 스탯이 전부 "정의되지 않음"이 되어, 진짜 원인이
            // 진단 수십 개 밑에 묻힌다.
            ChapterProgressionDto dto = ReadChapter();
            dto.Stats = null;

            ScenarioLoadResult result = ProgressionLoader.LoadAsSingleChapterScenario(dto);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Path, Is.EqualTo("Stats"));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("승격할 것이 없다"));
        }

        [Test]
        public void 빈_스탯_목록도_같은_이유로_거부한다()
        {
            ChapterProgressionDto dto = ReadChapter();
            dto.Stats = new List<StatDto>();

            Assert.That(ProgressionLoader.LoadAsSingleChapterScenario(dto).IsValid, Is.False);
        }

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
            public EpisodePhase Phase;
            public ScenarioAdvance Outcome;
            public ProgressionState FinalState;
            public List<string> DialogueNodes = new List<string>();
            public List<ProgressionSaveDto> SaveRequests = new List<ProgressionSaveDto>();
        }

        /// <summary>
        /// 호스트 펌프를 그대로 돈다 — 유니티 <c>ProgressionDriver</c>가 할 일과 같고,
        /// 다른 점은 요청 수행에 시간이 안 걸린다는 것뿐이다(즉시 완료 호스트).
        /// </summary>
        private static WalkResult Walk(int chooseIndex)
        {
            var walk = new WalkResult();
            EpisodeFlow flow = EpisodeFlow.Begin(LoadWrapped());

            int guard = 0;

            while (!flow.IsFinished)
            {
                Assert.That(++guard, Is.LessThan(50), "펌프가 안 끝난다 — 흐름이 도는 중이다.");

                FlowRequest request = flow.Pending;

                switch (request.Kind)
                {
                    case FlowRequestKind.PlayDialogue:
                        walk.DialogueNodes.Add(request.NodeName);
                        flow.DialogueCompleted();
                        break;

                    case FlowRequestKind.PresentOptions:
                        flow.Choose(chooseIndex);
                        break;

                    case FlowRequestKind.PlayVia:
                        flow.ViaCompleted();
                        break;

                    case FlowRequestKind.PersistSave:
                        walk.SaveRequests.Add(request.Save);
                        flow.SavePersisted();
                        break;

                    default:
                        Assert.Fail($"예상 못 한 요청: {request.Kind}");
                        break;
                }
            }

            walk.Phase = flow.Phase;
            walk.Outcome = flow.Pending.Outcome;
            walk.FinalState = flow.State;

            return walk;
        }

        private static ScenarioProgression LoadWrapped()
        {
            ScenarioLoadResult result =
                ProgressionLoader.LoadAsSingleChapterScenario(ReadChapter());

            Assert.That(result.HasErrors, Is.False, Joined(result));

            return result.Scenario;
        }

        private static ChapterProgressionDto ReadChapter()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "chapter-ch01-sample.json");

            Assert.That(File.Exists(path), Is.True, $"픽스처가 없다: {path}");

            return JsonSerializer.Deserialize<ChapterProgressionDto>(File.ReadAllText(path));
        }

        private static string Joined(ScenarioLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(d => d.ToString()));
    }
}
