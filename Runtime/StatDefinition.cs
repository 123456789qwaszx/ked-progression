using System;

namespace Ked.Progression
{
    public enum StatType
    {
        /// <summary>정수 스탯. 경계는 저작이 정한다.</summary>
        Number = 0,

        /// <summary>
        /// 참/거짓 스탯. 값 공간은 0·1이고 조건은 <see cref="ComparisonOp.Equal"/>뿐이다(§G4).
        /// 런타임에 bool이라는 별도 종류를 만들지 않는다 — 그게 저작 쪽 출력 모양이다.
        /// </summary>
        Bool = 1,
    }

    /// <summary>
    /// 스탯의 정의.
    ///
    /// <b>이 타입이 이 패키지가 새로 소유하는 유일한 개념이다.</b>
    /// 계약서 §G7이 적어 둔 빈칸이 여기다 — 초기값·최소·최대가 지금 어느 런타임 입력에도
    /// 실려 있지 않고, 저작 도구의 도달성 증명 안에만 있다. 그래서 증명은
    /// <c>Clamp(값, 최소, 최대)</c>로 걷는데 런타임은 경계를 몰라, 증명과 실제 플레이가
    /// 갈릴 수 있었다.
    ///
    /// 이 타입을 양쪽이 공유하면 그 불일치가 원천적으로 불가능해진다.
    /// </summary>
    public sealed class StatDefinition
    {
        public string Key { get; }
        public string DisplayName { get; }
        public StatType Type { get; }

        public int Initial { get; }
        public int Minimum { get; }
        public int Maximum { get; }

        /// <summary>
        /// 불변식을 만족하지 않으면 만들어지지 않는다 — 규율 1(침묵 금지).
        /// 잘못된 정의를 조용히 보정하면, 작가는 자기가 쓴 값이 아닌 것으로 플레이하게 된다.
        ///
        /// 데이터에서 오는 오류는 로더가 <b>전부 모아서</b> 진단으로 내는 것이 낫다
        /// (첫 오류에서 멈추면 작가가 왕복을 여러 번 해야 한다). 여기서 던지는 것은
        /// 로더를 거치지 않은 프로그래머 실수에 대한 마지막 방어선이다.
        /// </summary>
        public StatDefinition(
            string key,
            string displayName,
            StatType type,
            int initial,
            int minimum,
            int maximum)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("스탯 키가 비어 있다.", nameof(key));

            if (minimum > maximum)
                throw new ArgumentException(
                    $"스탯 '{key}'의 경계가 뒤집혀 있다: 최소 {minimum} > 최대 {maximum}.",
                    nameof(minimum));

            if (type == StatType.Bool && (minimum != 0 || maximum != 1))
                throw new ArgumentException(
                    $"bool 스탯 '{key}'의 경계는 0..1이어야 한다 (§G4). 받은 값: {minimum}..{maximum}.",
                    nameof(type));

            if (initial < minimum || initial > maximum)
                throw new ArgumentException(
                    $"스탯 '{key}'의 초기값 {initial}이 경계 {minimum}..{maximum} 밖이다. " +
                    "조용히 clamp하면 작가가 쓴 값과 다른 값으로 시작하게 된다.",
                    nameof(initial));

            Key = key;
            DisplayName = displayName;
            Type = type;
            Initial = initial;
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// 경계 안으로 자른다. <b>커밋 시점에만</b> 부른다 —
        /// 조건 판정은 커밋 전 값으로 하기 때문이다(§G6).
        /// </summary>
        public int Clamp(int value)
        {
            if (value < Minimum)
                return Minimum;

            return value > Maximum ? Maximum : value;
        }
    }
}
