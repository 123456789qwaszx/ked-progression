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
    /// <b>호스트가 쓰는 모습 그대로.</b>
    ///
    /// <see cref="ArchitectureWalkthroughTests"/>가 타입들이 어떻게 맞물리는지를 보여 준다면,
    /// 여기는 <b>그 맞물림을 흐름 하나로 감쌌을 때의 모습</b>을 고정한다. 아래 <c>switch</c>
    /// 다섯 줄이 호스트가 쓰는 코드 전부다 — 조건도, 스탯도, 챕터 경계도 나오지 않는다.
    /// </summary>
    public sealed class EpisodeFlowTests
    {
        [Test]
        public void 호스트_루프는_다섯_갈래뿐이다()
        {
            var trace = new List<string>();
            EpisodeFlow flow = EpisodeFlow.Begin(LoadScenario());

            for (int guard = 0; guard < 40; guard++)
            {
                trace.Add($"{flow.Phase}  {flow.Pending}");

                if (flow.IsFinished)
                    break;

                switch (flow.Pending.Kind)
                {
                    case FlowRequestKind.PlayDialogue:
                        // 호스트: EpisodePlayer.StartGameAsync(flow.Pending.NodeName)
                        flow.DialogueCompleted();
                        break;

                    case FlowRequestKind.PresentOptions:
                        // 호스트: 목록을 그리고 플레이어가 고른다
                        flow.Choose(IndexOf(flow.Pending.Options, "믿는다"));
                        break;

                    case FlowRequestKind.PlayVia:
                        // 호스트: 연출을 재생한다
                        flow.ViaCompleted();
                        break;

                    case FlowRequestKind.PersistSave:
                        // 호스트: File.WriteAllText(slot, ...)
                        flow.SavePersisted();
                        break;
                }
            }

            Assert.That(trace, Is.EqualTo(new[]
            {
                "EpisodeEntered  대사 재생 \"ch01_01\"",
                "AwaitingChoice  선택지 2개 (숨김 0)",
                "Committed  세이브 기록",
                "EpisodeEntered  대사 재생 \"ch01_end\"",
                "ChapterBoundaryCommitted  세이브 기록",
                "EpisodeEntered  대사 재생 \"ch02t_01\"",
                "Committed  세이브 기록",
                "EpisodeEntered  대사 재생 \"ch02t_02\"",
                "ScenarioFinished  종료 — ScenarioEnded",
            }), string.Join("\n", trace));

            // 스탯은 챕터를 넘어왔고, 해석은 아무것도 안 남았다(P3).
            Assert.That(flow.State.GetStat("trust"), Is.EqualTo(2));
            Assert.That(flow.State.ClearedChapterIds, Is.EquivalentTo(new[] { "ch01" }));
        }

        [Test]
        public void 엔딩키_하나가_다음_챕터를_가른다()
        {
            // 같은 엔딩키 ch01_done으로 끝나는데, 스탯에 따라 규칙이 갈린다.
            Assert.That(WalkTo("믿는다"), Is.EqualTo("ch02_trusted"));
            Assert.That(WalkTo("의심한다"), Is.EqualTo("ch02_plain"));
        }

        [Test]
        public void 자리가_아니면_던지고_자리는_그대로다()
        {
            EpisodeFlow flow = EpisodeFlow.Begin(LoadScenario());

            // 아직 대사도 안 끝났는데 고르려 한다.
            Assert.Throws<InvalidOperationException>(() => flow.Choose(0));

            // 던진 뒤에도 흐름은 그 자리에 있다 — 반쯤 진행된 자리를 만들지 않는다.
            Assert.That(flow.Phase, Is.EqualTo(EpisodePhase.EpisodeEntered));

            flow.DialogueCompleted();
            Assert.Throws<InvalidOperationException>(() => flow.DialogueCompleted());
        }

        [Test]
        public void 잠긴_것을_고르면_원인_조건을_지목한다()
        {
            ScenarioProgressionDto dto = ReadScenario();
            EpisodeOptionDto believe = dto.Chapters[0].Nodes[0].NextOptions[0];

            believe.Conditions = new List<ConditionDto>
            {
                new ConditionDto
                {
                    Kind = "Stat", Key = "trust", Op = "GreaterOrEqual", IntValue = 3,
                },
            };

            EpisodeFlow flow = EpisodeFlow.Begin(ProgressionLoader.Load(dto).Scenario);
            flow.DialogueCompleted();

            var thrown = Assert.Throws<InvalidOperationException>(() => flow.Choose(0));

            Assert.That(thrown.Message, Does.Contain("trust"));
            Assert.That(thrown.Message, Does.Contain("3"));
        }

        // ── 그릇 ────────────────────────────────────────────────────

        private static string WalkTo(string label)
        {
            EpisodeFlow flow = EpisodeFlow.Begin(LoadScenario());

            for (int guard = 0; guard < 40 && !flow.IsFinished; guard++)
            {
                switch (flow.Pending.Kind)
                {
                    case FlowRequestKind.PlayDialogue: flow.DialogueCompleted(); break;
                    case FlowRequestKind.PresentOptions:
                        flow.Choose(IndexOf(flow.Pending.Options, label));
                        break;
                    case FlowRequestKind.PlayVia: flow.ViaCompleted(); break;
                    case FlowRequestKind.PersistSave:
                        if (flow.Phase == EpisodePhase.ChapterBoundaryCommitted)
                            return flow.State.CurrentChapterId;

                        flow.SavePersisted();
                        break;
                }
            }

            return flow.State.CurrentChapterId;
        }

        private static int IndexOf(IReadOnlyList<ResolvedOption> options, string label)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Option.ChoiceLabel == label)
                    return i;
            }

            throw new InvalidOperationException($"선택지 [{label}]이 목록에 없다.");
        }

        private static ScenarioProgression LoadScenario() =>
            ProgressionLoader.Load(ReadScenario()).Scenario;

        private static ScenarioProgressionDto ReadScenario()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "scenario-two-chapters.json");

            return JsonSerializer.Deserialize<ScenarioProgressionDto>(File.ReadAllText(path));
        }
    }
}
#endif
