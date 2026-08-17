using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 조건 평가의 계약. §G2 · §G3 · §G4의 문장이 여기서 코드가 된다.
    /// </summary>
    public sealed class ConditionEvaluatorTests
    {
        private static readonly StatDefinition Trust =
            new StatDefinition("trust", "신뢰", StatType.Number, 0, -5, 5);

        private static readonly StatDefinition Flag =
            new StatDefinition("met_willow", "윌로를 만남", StatType.Bool, 0, 0, 1);

        private static ProgressionState StateWith(params StatChange[] changes)
        {
            var defs = new Dictionary<string, StatDefinition>(StringComparer.Ordinal)
            {
                { Trust.Key, Trust },
                { Flag.Key, Flag },
            };

            return ProgressionState
                .CreateInitial(new[] { Trust, Flag }, "ep_01")
                .WithStatChanges(defs, changes);
        }

        private static bool Met(ProgressionCondition condition, ProgressionState state) =>
            ConditionEvaluator.IsMet(condition, state);

        // ── 비교 연산 5종 ────────────────────────────────────────────

        [TestCase(ComparisonOp.GreaterOrEqual, 3, true)]
        [TestCase(ComparisonOp.GreaterOrEqual, 4, false)]
        [TestCase(ComparisonOp.LessOrEqual, 3, true)]
        [TestCase(ComparisonOp.LessOrEqual, 2, false)]
        [TestCase(ComparisonOp.Equal, 3, true)]
        [TestCase(ComparisonOp.Equal, 2, false)]
        [TestCase(ComparisonOp.GreaterThan, 2, true)]
        [TestCase(ComparisonOp.GreaterThan, 3, false)]
        [TestCase(ComparisonOp.LessThan, 4, true)]
        [TestCase(ComparisonOp.LessThan, 3, false)]
        public void 스탯_비교(ComparisonOp op, int value, bool expected)
        {
            ProgressionState state = StateWith(new StatChange("trust", 3));

            var condition = new ProgressionCondition(ConditionKind.Stat, "trust", op, value);

            Assert.That(Met(condition, state), Is.EqualTo(expected));
        }

        // ── §G2 · §G4 ───────────────────────────────────────────────

        [Test]
        public void 값이_생략된_조건은_0과_비교한다()
        {
            // §G2 — 저작 쪽은 0을 키 생략으로 내보낸다.
            // 즉 flag == false 는 { Kind: Stat, Key: ..., Op: Equal } 로만 온다.
            ProgressionState notMet = StateWith();

            var isFalse = new ProgressionCondition(
                ConditionKind.Stat, "met_willow", ComparisonOp.Equal);

            Assert.That(isFalse.Value, Is.EqualTo(0), "생략된 값은 0이어야 한다");
            Assert.That(Met(isFalse, notMet), Is.True);

            ProgressionState met = StateWith(new StatChange("met_willow", 1));
            Assert.That(Met(isFalse, met), Is.False);
        }

        [Test]
        public void bool_스탯은_1을_넘지_않는다()
        {
            // §G4 — 값 공간이 0·1이다. 커밋에서 clamp된다.
            ProgressionState state = StateWith(new StatChange("met_willow", 5));

            Assert.That(state.GetStat("met_willow"), Is.EqualTo(1));
        }

        // ── EpisodeCleared ──────────────────────────────────────────

        [Test]
        public void 클리어한_에피소드를_판정한다()
        {
            ProgressionState state = StateWith().WithMovedTo("ep_02");

            var cleared = new ProgressionCondition(
                ConditionKind.EpisodeCleared, "ep_01", ComparisonOp.Exists);

            var notCleared = new ProgressionCondition(
                ConditionKind.EpisodeCleared, "ep_09", ComparisonOp.Exists);

            Assert.That(Met(cleared, state), Is.True);
            Assert.That(Met(notCleared, state), Is.False);
        }

        // ── 규율 1: 침묵 금지 ───────────────────────────────────────

        [Test]
        public void 정의되지_않은_스탯은_0이_아니라_예외다()
        {
            // 조용히 0을 주면 오타 낸 조건이 "언제나 통과하는 관문"으로 바뀐다.
            // 그런 버그는 재생해 봐도 안 보인다.
            ProgressionState state = StateWith();

            var typo = new ProgressionCondition(
                ConditionKind.Stat, "trsut", ComparisonOp.GreaterOrEqual, 1);

            Assert.Throws<KeyNotFoundException>(() => Met(typo, state));
        }

        [Test]
        public void EpisodeCleared에_다른_연산이_오면_예외다()
        {
            ProgressionState state = StateWith();

            var wrong = new ProgressionCondition(
                ConditionKind.EpisodeCleared, "ep_01", ComparisonOp.GreaterOrEqual, 1);

            Assert.Throws<NotSupportedException>(() => Met(wrong, state));
        }
    }
}
