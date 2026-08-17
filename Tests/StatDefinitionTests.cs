using System;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// §G4(bool 스탯)와 §G7(경계의 집)을 코드로 고정한다.
    /// </summary>
    public sealed class StatDefinitionTests
    {
        private static StatDefinition Number(int initial, int min, int max) =>
            new StatDefinition("trust", "신뢰", StatType.Number, initial, min, max);

        [Test]
        public void 경계로_자른다()
        {
            StatDefinition trust = Number(0, -5, 5);

            Assert.That(trust.Clamp(99), Is.EqualTo(5));
            Assert.That(trust.Clamp(-99), Is.EqualTo(-5));
            Assert.That(trust.Clamp(3), Is.EqualTo(3));
        }

        [Test]
        public void bool_스탯의_경계는_0과_1이다()
        {
            // §G4 — 값 공간이 0·1이고 조건은 Equal뿐이다.
            Assert.DoesNotThrow(() =>
                new StatDefinition("flag", "깃발", StatType.Bool, 0, 0, 1));

            Assert.Throws<ArgumentException>(() =>
                new StatDefinition("flag", "깃발", StatType.Bool, 0, 0, 5));
        }

        [Test]
        public void 초기값이_경계_밖이면_거부한다()
        {
            // 조용히 clamp하면 작가가 쓴 값과 다른 값으로 시작하게 된다 — 규율 1.
            Assert.Throws<ArgumentException>(() => Number(10, -5, 5));
        }

        [Test]
        public void 뒤집힌_경계를_거부한다()
        {
            Assert.Throws<ArgumentException>(() => Number(0, 5, -5));
        }
    }
}
