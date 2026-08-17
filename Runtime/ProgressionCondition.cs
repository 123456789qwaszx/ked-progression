namespace Ked.Progression
{
    /// <summary>
    /// 조건이 무엇을 묻는가. 저작 쪽은 이름 문자열로 내보낸다(§G1).
    /// </summary>
    public enum ConditionKind
    {
        /// <summary>스탯 값을 <see cref="ComparisonOp"/>로 비교한다.</summary>
        Stat = 0,

        /// <summary>에피소드를 클리어했는가. <see cref="ComparisonOp.Exists"/>만 쓴다.</summary>
        EpisodeCleared = 1,
    }

    /// <summary>
    /// 진행 조건 하나. 값 타입이며 불변이다.
    /// </summary>
    public readonly struct ProgressionCondition
    {
        public ConditionKind Kind { get; }
        public string Key { get; }
        public ComparisonOp Op { get; }

        /// <summary>
        /// 비교 대상 값.
        ///
        /// ⚠ §G2 — 저작 쪽은 <b>0을 키 자체를 생략해서</b> 내보낸다
        /// (<c>JsonIgnoreCondition.WhenWritingDefault</c>). 즉 <c>trust &gt;= 0</c>은
        /// <c>{ "Kind": "Stat", "Key": "trust", "Op": "GreaterOrEqual" }</c>로 나간다.
        ///
        /// 따라서 DTO의 대응 필드는 반드시 <c>int</c>여야 한다. <c>int?</c>로 두면
        /// "없음"과 "0"이 갈려서, 가장 흔한 조건인 <c>flag == false</c>(= Equal 0)가
        /// 통째로 어긋난다.
        /// </summary>
        public int Value { get; }

        public ProgressionCondition(ConditionKind kind, string key, ComparisonOp op, int value = 0)
        {
            Kind = kind;
            Key = key;
            Op = op;
            Value = value;
        }

        /// <summary>진단 메시지에 그대로 실린다. 사람이 원문에서 찾을 수 있는 모양으로 쓴다.</summary>
        public override string ToString()
        {
            return Kind == ConditionKind.EpisodeCleared
                ? $"{Kind}({Key})"
                : $"{Key} {Op} {Value}";
        }
    }
}
