using System;

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

        /// <summary>
        /// 챕터를 클리어했는가. <see cref="ComparisonOp.Exists"/>만 쓴다.
        ///
        /// 시나리오 층이 생기면서 들어왔다 — 챕터를 넘나드는 조건은 이것 하나뿐이다.
        /// 대상 챕터의 실재는 시나리오가 검사한다(에피소드와 다른 판단: 챕터는 개수가 적고
        /// 오타가 곧 시나리오가 끊기는 것이라 fail-closed로 두지 않는다).
        /// </summary>
        ChapterCleared = 2,
    }

    /// <summary>
    /// 진행 조건 하나. 값 타입이며 불변이다.
    ///
    /// <b>생성자가 private이다.</b> <see cref="Kind"/>와 <see cref="Op"/>는 자유 조합이
    /// 아니다 — Cleared 계열은 <see cref="ComparisonOp.Exists"/>만 뜻이 있고 나머지 조합은
    /// 존재하지 않는다. 공개 생성자를 두면 그 조합이 <b>타이핑된 뒤</b> 어딘가에서 예외로
    /// 걸리지만, 팩토리만 열면 애초에 타이핑되지 않는다.
    ///
    /// 구 런타임 <c>EpisodeCondition</c>과의 결정적 차이가 여기다 — 그쪽은
    /// <c>IntValue</c>·<c>BoolValue</c>·<c>StringValue</c>를 한 타입에 다 두고 Kind에 따라
    /// 뜻이 달라졌다. 5종 × 6연산 = 30조합 중 대부분이 무의미한데 아무것도 막지 않았고,
    /// 그래서 평가기가 조용히 틀릴 자리가 생겼다.
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

        private ProgressionCondition(ConditionKind kind, string key, ComparisonOp op, int value)
        {
            Kind = kind;
            Key = key;
            Op = op;
            Value = value;
        }

        /// <summary>스탯 비교. 연산 6종 전부 쓸 수 있다.</summary>
        public static ProgressionCondition Stat(string key, ComparisonOp op, int value = 0) =>
            new ProgressionCondition(ConditionKind.Stat, Require(key, nameof(key)), op, value);

        /// <summary>에피소드를 클리어했는가. 연산을 고를 여지가 없다.</summary>
        public static ProgressionCondition EpisodeCleared(string episodeId) =>
            new ProgressionCondition(
                ConditionKind.EpisodeCleared,
                Require(episodeId, nameof(episodeId)),
                ComparisonOp.Exists,
                0);

        /// <summary>챕터를 클리어했는가. 연산을 고를 여지가 없다.</summary>
        public static ProgressionCondition ChapterCleared(string chapterId) =>
            new ProgressionCondition(
                ConditionKind.ChapterCleared,
                Require(chapterId, nameof(chapterId)),
                ComparisonOp.Exists,
                0);

        /// <summary>
        /// 팩토리를 거쳐 만들어진 값인가.
        ///
        /// ⚠ <c>default(ProgressionCondition)</c>은 C#이 언제나 만들 수 있다 — struct의
        /// 한계이고 막을 방법이 없다. 그 값은 <see cref="Key"/>가 <c>null</c>이므로
        /// <b>여기서 판별된다.</b> 배열에 그런 값이 섞이는 것은 소유 타입의 생성자가
        /// 거부하고, 평가기는 다시 확인하지 않는다 — 경계에서 좁혔으므로 안쪽은 전체 함수다.
        /// </summary>
        public bool IsConstructed => Key != null;

        /// <summary>
        /// 조건 목록에 <c>default</c>가 섞이지 않았는지 확인한다.
        ///
        /// <b>조건을 품는 타입이 여럿이므로(간선 · 엔딩 규칙) 검사를 여기 둔다.</b>
        /// 각자 쓰면 사본이 되고, 사본은 한쪽만 고쳐지는 날이 온다.
        /// </summary>
        internal static void RequireAllConstructed(
            System.Collections.Generic.IReadOnlyList<ProgressionCondition> conditions,
            string paramName)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].IsConstructed)
                {
                    continue;
                }

                throw new ArgumentException(
                    $"{paramName}[{i}]가 만들어지지 않은 조건이다(default 값). " +
                    "ProgressionCondition.Stat/EpisodeCleared/ChapterCleared로 만들 것.",
                    paramName);
            }
        }

        /// <summary>진단 메시지에 그대로 실린다. 사람이 원문에서 찾을 수 있는 모양으로 쓴다.</summary>
        public override string ToString()
        {
            return Kind == ConditionKind.Stat
                ? $"{Key} {Op} {Value}"
                : $"{Kind}({Key})";
        }

        private static string Require(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("조건의 대상 키가 비어 있다.", paramName);
            }

            return value;
        }
    }
}
