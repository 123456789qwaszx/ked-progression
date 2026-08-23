using System;
using System.Linq;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// §G5(관문) · §G6(전이 3갈래)가 코드가 되는 자리. <b>관문이 실제로 잠그는지를 여기서만
    /// 증명할 수 있다</b> — 모델과 로더는 조건을 싣기만 하고 보지 않는다.
    /// </summary>
    public sealed class ChapterTransitionTests
    {
        private static readonly StatDefinition Trust =
            new StatDefinition("trust", "신뢰", StatType.Number, 0, 0, 5);

        private static readonly ProgressionCondition[] NeedsThree =
        {
            ProgressionCondition.Stat("trust", ComparisonOp.GreaterOrEqual, 3),
        };

        private static readonly ProgressionCondition[] NeedsFive =
        {
            ProgressionCondition.Stat("trust", ComparisonOp.GreaterOrEqual, 5),
        };

        private static EpisodeNode Node(string id, params EpisodeOption[] options) =>
            new EpisodeNode(id, id, EpisodeKind.Main, "entry_" + id, options);

        private static ChapterProgression Chapter(params EpisodeNode[] nodes) =>
            new ChapterProgression("ch_01", "첫 챕터", "ep_01", new[] { Trust }, nodes);

        // ── 세 갈래 ─────────────────────────────────────────────────

        [Test]
        public void 고를_수_있는_것이_있으면_입력을_기다린다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("설득한다", "ep_02", conditions: NeedsThree,
                        lockedReasonText: "아직 그를 믿지 못한다"),
                    EpisodeOption.Choice("떠난다", "ep_03"),
                    EpisodeOption.Choice("비밀 통로", "ep_03", visibleConditions: NeedsFive),
                    EpisodeOption.Auto("ep_03")),
                Node("ep_02"),
                Node("ep_03"));

            ChapterAdvance advance =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState());

            Assert.That(advance.Kind, Is.EqualTo(ChapterAdvanceKind.AwaitPlayerChoice));

            // 잠긴 것은 보이고, 표시조건 미달과 자동 진행은 목록에 없다.
            Assert.That(advance.Options.Count, Is.EqualTo(2));
            Assert.That(advance.HiddenCount, Is.EqualTo(1));

            // §G6 — 배열 순서가 곧 화면 순서다. 정렬하지 않는다.
            Assert.That(advance.Options[0].Option.ChoiceLabel, Is.EqualTo("설득한다"));
            Assert.That(advance.Options[1].Option.ChoiceLabel, Is.EqualTo("떠난다"));
        }

        [Test]
        public void 고를_수_있는_것이_없으면_자동으로_간다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("설득한다", "ep_02", conditions: NeedsThree),
                    EpisodeOption.Auto("ep_03")),
                Node("ep_02"),
                Node("ep_03"));

            ChapterAdvance advance =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState());

            Assert.That(advance.Kind, Is.EqualTo(ChapterAdvanceKind.AutoAdvance));
            Assert.That(advance.AutoOption.TargetEpisodeId, Is.EqualTo("ep_03"));

            // 누를 수 없는 목록을 띄워 놓고 아무 일도 안 일어나는 것이 더 나쁘다.
            Assert.That(advance.Options, Is.Empty);
        }

        [Test]
        public void 자동_진행도_없으면_챕터가_끝난다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01", EpisodeOption.Choice("간다", "ep_02")),
                new EpisodeNode("ep_02", "끝", EpisodeKind.Main, "e2", null, "ch01_end"));

            ProgressionState atEnd = chapter
                .CreateProofEntryState()
                .Commit(chapter, chapter.StartNode.NextOptions[0]);

            ChapterAdvance advance = ChapterTransition.Resolve(chapter, atEnd);

            Assert.That(advance.Kind, Is.EqualTo(ChapterAdvanceKind.ChapterEnded));
            Assert.That(advance.EndingKey, Is.EqualTo("ch01_end"));
        }

        // ── 관문 ────────────────────────────────────────────────────

        [Test]
        public void 잠긴_선택지가_원인_조건을_지목한다()
        {
            // 툴의 도달성 증명이 이미 원인 조건을 지목한다. 런타임도 같은 것을 낼 수
            // 있어야 "왜 잠겼는지"를 두 곳에서 따로 계산하지 않는다.
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("설득한다", "ep_02", conditions: NeedsThree,
                        lockedReasonText: "아직 그를 믿지 못한다"),
                    EpisodeOption.Choice("떠난다", "ep_02")),
                Node("ep_02"));

            ResolvedOption locked =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState()).Options[0];

            Assert.That(locked.Visibility, Is.EqualTo(OptionVisibility.Locked));
            Assert.That(locked.IsSelectable, Is.False);
            Assert.That(locked.LockedReason, Is.EqualTo("아직 그를 믿지 못한다"));

            Assert.That(locked.BlockingCondition.IsConstructed, Is.True);
            Assert.That(locked.BlockingCondition.Key, Is.EqualTo("trust"));
            Assert.That(locked.BlockingCondition.Value, Is.EqualTo(3));
        }

        [Test]
        public void 안내문이_없으면_지어내지_않는다()
        {
            // 무해석성 — 이 패키지는 플레이어에게 보일 문장을 만들지 않는다.
            // 무엇 때문인지는 BlockingCondition이 기계가 읽을 모양으로 진다.
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("설득한다", "ep_02", conditions: NeedsThree),
                    EpisodeOption.Choice("떠난다", "ep_02")),
                Node("ep_02"));

            ResolvedOption locked =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState()).Options[0];

            Assert.That(locked.LockedReason, Is.EqualTo(string.Empty));
            Assert.That(locked.BlockingCondition.IsConstructed, Is.True);
        }

        [Test]
        public void 숨김_설정된_선택지는_목록에서_빠진다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("설득한다", "ep_02",
                        conditions: NeedsThree, hideWhenLocked: true),
                    EpisodeOption.Choice("떠난다", "ep_02")),
                Node("ep_02"));

            ChapterAdvance advance =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState());

            Assert.That(advance.Options.Count, Is.EqualTo(1));
            Assert.That(advance.HiddenCount, Is.EqualTo(1),
                "숨겼다는 사실 자체는 남는다 — 몇 개인지조차 모르면 추적할 수 없다");
        }

        [Test]
        public void 스탯이_오르면_관문이_열린다()
        {
            // ★ 관문이 실제로 잠그고 실제로 열리는지. 이것이 안 되면 나머지가 다 무의미하다.
            var loop = EpisodeOption.Choice(
                "수련한다", "ep_01", statChanges: new[] { StatChange.Add("trust", 3) });

            var gate = EpisodeOption.Choice(
                "문을 연다", "ep_02", conditions: NeedsThree, lockedReasonText: "신뢰가 모자라다");

            ChapterProgression chapter = Chapter(Node("ep_01", loop, gate), Node("ep_02"));

            ProgressionState before = chapter.CreateProofEntryState();
            Assert.That(ChapterTransition.Resolve(chapter, before).Options[1].IsSelectable,
                Is.False);

            // ⚠ 판정은 커밋 전 값으로 한다(§G6) — 위 판정은 trust=0일 때의 것이다.
            ProgressionState after = before.Commit(chapter, loop);
            Assert.That(after.GetStat("trust"), Is.EqualTo(3));

            Assert.That(ChapterTransition.Resolve(chapter, after).Options[1].IsSelectable,
                Is.True);
        }

        // ── 구조 ────────────────────────────────────────────────────

        [Test]
        public void 자동_진행_간선은_선택지가_아니다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01",
                    EpisodeOption.Choice("떠난다", "ep_02"),
                    EpisodeOption.Auto("ep_02")),
                Node("ep_02"));

            ChapterAdvance advance =
                ChapterTransition.Resolve(chapter, chapter.CreateProofEntryState());

            Assert.That(advance.Options.Count, Is.EqualTo(1));
            Assert.That(advance.HiddenCount, Is.Zero, "자동 진행은 숨긴 선택지가 아니다");
        }

        [Test]
        public void 숨김에는_이름이_없다()
        {
            // §G5의 "목록에 만들지 않는다"를 그대로 옮기면 숨긴 것은 목록에 **없는 것**이다.
            // Hidden 값을 두면 호스트가 전부 그리다 숨겨야 할 것을 보여 주는 사고가 열리고,
            // 그 값은 어차피 아무도 그리면 안 되므로 영원히 안 타는 분기가 된다.
            Assert.That(Enum.GetNames(typeof(OptionVisibility)),
                Is.EqualTo(new[] { "Shown", "Locked" }));
        }

        [Test]
        public void 해석_결과는_저장_대상이_아니다()
        {
            // P3 — 구 런타임은 LockedEpisodeIds·VisibleEpisodeIds를 상태에 넣었고,
            // 그래서 세이브에 옛 판정이 섞여 들어갈 길이 열려 있었다.
            Assert.That(typeof(ChapterAdvance).IsDefined(typeof(SerializableAttribute), false),
                Is.False);
            Assert.That(typeof(ResolvedOption).IsDefined(typeof(SerializableAttribute), false),
                Is.False);

            Assert.That(typeof(ProgressionState).GetProperty("LockedEpisodeIds"), Is.Null);
            Assert.That(typeof(ProgressionState).GetProperty("VisibleEpisodeIds"), Is.Null);
        }

        [Test]
        public void 다른_챕터의_상태를_넘기면_거부한다()
        {
            ChapterProgression chapter = Chapter(Node("ep_01"), Node("ep_02"));

            ProgressionState elsewhere = ProgressionState
                .CreateInitial(new[] { Trust }, "ch_99", "없는에피소드");

            Assert.Throws<ArgumentException>(() =>
                ChapterTransition.Resolve(chapter, elsewhere));
        }
    }
}
