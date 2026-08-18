using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 챕터가 끝난 뒤 어디로 가는가.
    ///
    /// <b>빈 <c>NextChapterId</c>를 "시나리오 종료"의 뜻으로 쓰지 않는다.</b> 그건
    /// <c>ChoiceLabel</c>이 비면 자동 진행이던 것과 같은 sentinel이고, 같은 사고를 만든다 —
    /// 저작에서 다음 챕터 칸을 실수로 비우면 <b>거기서 게임이 끝나 버리는데 아무도 모른다.</b>
    /// 구 런타임의 <c>bool UnlockNextChapter</c> + <c>NextChapterId</c>도 같은 이유로
    /// 안 가져왔다(4조합 중 둘이 무효였다).
    /// </summary>
    public enum EndingOutcome
    {
        /// <summary>다음 챕터로 이어진다.</summary>
        NextChapter = 0,

        /// <summary>여기서 시나리오가 끝난다. 의도한 종착점이다.</summary>
        ScenarioEnd = 1,
    }

    /// <summary>
    /// 챕터에서 나가는 길 하나 — <b>시나리오 층의 간선이다.</b>
    /// 에피소드 층의 <see cref="EpisodeOption"/>과 같은 자리에 같은 방식으로 놓인다:
    /// 출발 노드(챕터)가 자기 나가는 길을 소유한다.
    ///
    /// 원본은 런타임 <c>ChapterEndingRule</c>(test13)이다. <b>모양만 가져오고 코드는 안
    /// 가져왔다</b> — 그쪽 평가기는 조건을 조용히 틀리게 읽는다.
    ///
    /// <b>D2 — 어느 엔딩인지는 이 타입이 정하지 않는다.</b>
    /// <see cref="EpisodeNode.EndingKey"/>가 정하고, 이 규칙은 그 키로 <b>조회된다.</b>
    /// <see cref="Conditions"/>는 판정이 아니라 <b>같은 엔딩키에서 다음 챕터가 갈릴 때</b>
    /// 갈래를 고르는 용도다. 엔딩을 두 곳에서 정하면 반드시 어긋난다.
    /// </summary>
    public sealed class EndingRule
    {
        public EndingOutcome Outcome { get; }

        /// <summary><see cref="EpisodeNode.EndingKey"/>와 맞물린다.</summary>
        public string EndingKey { get; }

        public string DisplayName { get; }

        /// <summary>
        /// 같은 엔딩키의 규칙이 여럿일 때 갈래를 고른다(AND). <b>비어 있으면 무조건 성립</b>
        /// 하며, 같은 키의 <b>마지막</b> 규칙은 반드시 비어 있어야 한다 — 그래야 "엔딩에
        /// 도달했는데 갈 곳이 없다"가 생기지 않는다.
        /// </summary>
        public IReadOnlyList<ProgressionCondition> Conditions { get; }

        /// <summary><see cref="EndingOutcome.ScenarioEnd"/>면 빈 문자열이다.</summary>
        public string NextChapterId { get; }

        public string DesignerNote { get; }

        /// <summary>조건이 없어 무조건 성립하는 규칙 — 같은 키의 마지막 자리다.</summary>
        public bool IsCatchAll => Conditions.Count == 0;

        private EndingRule(
            EndingOutcome outcome,
            string endingKey,
            string nextChapterId,
            IReadOnlyList<ProgressionCondition> conditions,
            string displayName,
            string designerNote)
        {
            if (string.IsNullOrEmpty(endingKey))
            {
                throw new ArgumentException(
                    "엔딩 규칙의 엔딩키가 비어 있다. 어느 엔딩의 길인지 알 수 없다.",
                    nameof(endingKey));
            }

            Outcome = outcome;
            EndingKey = endingKey;
            NextChapterId = nextChapterId ?? string.Empty;
            Conditions = conditions ?? Array.Empty<ProgressionCondition>();
            DisplayName = displayName ?? string.Empty;
            DesignerNote = designerNote ?? string.Empty;

            ProgressionCondition.RequireAllConstructed(Conditions, nameof(conditions));
        }

        /// <summary>이 엔딩에서 다음 챕터로 이어진다.</summary>
        public static EndingRule To(
            string endingKey,
            string nextChapterId,
            IReadOnlyList<ProgressionCondition> conditions = null,
            string displayName = null,
            string designerNote = null)
        {
            if (string.IsNullOrEmpty(nextChapterId))
            {
                throw new ArgumentException(
                    $"엔딩 '{endingKey}'의 다음 챕터가 비어 있다. " +
                    "여기서 시나리오가 끝나는 것이라면 EndingRule.Ends()를 쓸 것.",
                    nameof(nextChapterId));
            }

            return new EndingRule(
                EndingOutcome.NextChapter, endingKey, nextChapterId,
                conditions, displayName, designerNote);
        }

        /// <summary>
        /// 이 엔딩에서 시나리오가 끝난다. <b>다음 챕터 인자를 받지 않는다</b> —
        /// "끝난다"와 "다음을 안 적었다"가 같은 모양이 되지 않게 한다.
        /// </summary>
        public static EndingRule Ends(
            string endingKey,
            IReadOnlyList<ProgressionCondition> conditions = null,
            string displayName = null,
            string designerNote = null)
        {
            return new EndingRule(
                EndingOutcome.ScenarioEnd, endingKey, string.Empty,
                conditions, displayName, designerNote);
        }

        public override string ToString() =>
            Outcome == EndingOutcome.ScenarioEnd
                ? $"{EndingKey} → (시나리오 종료)"
                : $"{EndingKey} → {NextChapterId}";
    }
}
