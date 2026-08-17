using System;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 계약 테스트 — "내 동작이 맞나"가 아니라 <b>"내 약속이 유지되나"</b>를 묻는다.
    /// runtime-contract §G3의 문장이 여기서 코드가 된다.
    /// </summary>
    public sealed class ComparisonOpTests
    {
        [Test]
        public void 비교_연산은_여섯_종이다()
        {
            // §G3 — 기존 넷(GreaterOrEqual · LessOrEqual · Equal · Exists)에
            // 2026-08-16 개방으로 GreaterThan · LessThan이 더해졌다.
            Assert.That(Enum.GetValues(typeof(ComparisonOp)).Length, Is.EqualTo(6));
        }

        [Test]
        public void NotEqual은_없다()
        {
            // 저작 쪽 파서가 닫아 두어 데이터로 나오지 않는다.
            // 미리 넣으면 평가기에 영원히 안 타는 분기가 생긴다.
            // 파서가 열려서 이 테스트가 걸리면, 그때가 더할 때다.
            Assert.That(Enum.IsDefined(typeof(ComparisonOp), "NotEqual"), Is.False);
        }

        [TestCase("GreaterOrEqual", ComparisonOp.GreaterOrEqual)]
        [TestCase("LessOrEqual", ComparisonOp.LessOrEqual)]
        [TestCase("Equal", ComparisonOp.Equal)]
        [TestCase("Exists", ComparisonOp.Exists)]
        [TestCase("GreaterThan", ComparisonOp.GreaterThan)]
        [TestCase("LessThan", ComparisonOp.LessThan)]
        public void 저작이_내보내는_이름으로_되읽을_수_있다(string exported, ComparisonOp expected)
        {
            // §G1 — enum은 이름 문자열로 나간다. 순서를 재배열해도 깨지지 않아야 한다.
            Assert.That(Enum.Parse(typeof(ComparisonOp), exported), Is.EqualTo(expected));
        }
    }
}
