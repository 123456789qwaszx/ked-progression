using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 조건 평가의 계약. §G2 · §G3 · §G4의 문장이 여기서 코드가 된다.
    ///
    /// <b>상태를 손으로 만들지 않고 커밋을 거쳐 만든다.</b> 스탯이 변하는 자리가 간선 하나뿐이
    /// 되었으므로(§G6-1), 테스트도 그 길로만 값을 만든다 — 지름길이 있으면 지름길로만
    /// 검증된 코드가 생긴다.
    /// </summary>
    public sealed class ConditionEvaluatorTests
    {
        private static readonly StatDefinition Trust =
            new StatDefinition("trust", "신뢰", StatType.Number, 0, -5, 5);

        /// <summary>초기값 0인 bool 스탯 — §G4상 간선으로는 바꿀 수 없다.</summary>
        private static readonly StatDefinition FlagOff =
            new StatDefinition("met_willow", "윌로를 만남", StatType.Bool, 0, 0, 1);

        /// <summary>초기값 1인 bool 스탯. 참인 쪽을 검증할 유일한 길이다.</summary>
        private static readonly StatDefinition FlagOn =
            new StatDefinition("met_raru", "라루를 만남", StatType.Bool, 1, 0, 1);

        private static readonly StatDefinition[] Stats = { Trust, FlagOff, FlagOn };

        /// <summary>
        /// ep_01에서 <paramref name="changes"/>를 실은 자동 진행 간선을 타고 ep_02로 간 상태.
        /// 그래서 ep_01은 클리어되어 있다.
        /// </summary>
        private static ProgressionState StateWith(params StatChange[] changes)
        {
            EpisodeOption exit = EpisodeOption.Auto("ep_02", changes);

            var chapter = new ChapterProgression(
                "ch_01", "첫 챕터", "ep_01", Stats,
                new[]
                {
                    new EpisodeNode("ep_01", "첫", "e1", new[] { exit }),
                    new EpisodeNode("ep_02", "둘", "e2"),
                });

            return chapter.CreateEntryState().Commit(chapter, exit);
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
            ProgressionState state = StateWith(StatChange.Add("trust", 3));

            Assert.That(Met(ProgressionCondition.Stat("trust", op, value), state),
                Is.EqualTo(expected));
        }

        [Test]
        public void 커밋은_경계로_clamp한다()
        {
            Assert.That(StateWith(StatChange.Add("trust", 99)).GetStat("trust"), Is.EqualTo(5));
            Assert.That(StateWith(StatChange.Add("trust", -99)).GetStat("trust"), Is.EqualTo(-5));
        }

        // ── §G2 · §G4 ───────────────────────────────────────────────

        [Test]
        public void 값이_생략된_조건은_0과_비교한다()
        {
            // §G2 — 저작 쪽은 0을 키 생략으로 내보낸다.
            // 즉 flag == false 는 { Kind: Stat, Key: ..., Op: Equal } 로만 온다.
            ProgressionState state = StateWith();

            ProgressionCondition willowIsFalse =
                ProgressionCondition.Stat("met_willow", ComparisonOp.Equal);

            Assert.That(willowIsFalse.Value, Is.EqualTo(0), "생략된 값은 0이어야 한다");
            Assert.That(Met(willowIsFalse, state), Is.True, "초기값 0이므로 참");

            ProgressionCondition raruIsFalse =
                ProgressionCondition.Stat("met_raru", ComparisonOp.Equal);

            Assert.That(Met(raruIsFalse, state), Is.False, "초기값 1이므로 거짓");
        }

        // ── 조건은 스탯 하나뿐이다 ──────────────────────────────────

        [Test]
        public void 조건의_갈래는_스탯_하나다()
        {
            // 클리어 이력·본 라인 같은 것은 [1] 영구 계층의 것이라 걷어냈다. 그 계층이
            // 서면 갈래가 다시 늘고, 그때까지 조건이 볼 수 있는 것은 스탯뿐이다.
            Assert.That(Enum.GetNames(typeof(ConditionKind)), Is.EqualTo(new[] { "Stat" }));
        }

        // ── 규율 1: 침묵 금지 ───────────────────────────────────────

        [Test]
        public void 정의되지_않은_스탯은_0이_아니라_예외다()
        {
            // 조용히 0을 주면 오타 낸 조건이 "언제나 통과하는 관문"으로 바뀐다.
            // 그런 버그는 재생해 봐도 안 보인다.
            ProgressionState state = StateWith();

            Assert.Throws<KeyNotFoundException>(() =>
                Met(ProgressionCondition.Stat("trsut", ComparisonOp.GreaterOrEqual, 1), state));
        }

        // ── P1: 무효 조합이 만들어지지 않는다 ───────────────────────

        [Test]
        public void 조건은_팩토리로만_만들어진다()
        {
            // 생성자가 열려 있으면 무효 조합을 손으로 만들 수 있고, 그러면 평가기가
            // 그것을 다시 검사해야 한다. 팩토리만 두면 안쪽이 전체 함수로 남는다.
            Assert.That(typeof(ProgressionCondition).GetConstructors(), Is.Empty);
        }

        [Test]
        public void 대상_키가_비면_조건을_만들_수_없다()
        {
            Assert.Throws<ArgumentException>(() =>
                ProgressionCondition.Stat(string.Empty, ComparisonOp.Equal, 1));
            Assert.Throws<ArgumentException>(() =>
                ProgressionCondition.Stat(null, ComparisonOp.Equal, 1));
        }

        [Test]
        public void 만들어지지_않은_조건은_판별된다()
        {
            // default(ProgressionCondition)은 struct라 C#이 언제나 만든다. 막을 수 없으므로
            // 판별할 수 있게 해 두고, 배열에 섞이는 것은 소유 타입이 거부한다.
            Assert.That(default(ProgressionCondition).IsConstructed, Is.False);
            Assert.That(ProgressionCondition.Stat("trust", ComparisonOp.Equal, 1).IsConstructed,
                Is.True);
        }
    }
}
