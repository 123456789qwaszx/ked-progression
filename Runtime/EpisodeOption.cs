using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 이 길이 무엇인가.
    ///
    /// <b>전에는 이것을 빈 문자열로 표현했다</b>(<c>IsDefault =&gt; ChoiceLabel.Length == 0</c>).
    /// sentinel이므로 사고가 하나 열려 있었다 — 작가가 엑셀에서 선택지 문구를 실수로 지우면
    /// 그 간선이 <b>플레이어 선택지에서 보이지 않는 자동 진행으로 조용히 변신한다.</b>
    /// 분기가 사라지고, 검증은 통과하고, 게임을 돌려 봐야 안다.
    ///
    /// 연출 층의 <c>GateTokenType.Immediately</c>가 "기다림 없음"을 명시적 토큰으로 만든 것과
    /// 같은 판단이다 — <b>없음은 값이어야지 빈 문자열이면 안 된다.</b>
    ///
    /// ⚠ 저작 데이터에는 아직 이 구분이 없다(간선 시트의 `선택지` 열이 비었는지로 읽는다).
    /// 로더가 그 변환의 유일한 지점이 되고, 열을 둘지는 소유자 결정(D5)이다.
    /// </summary>
    public enum OptionKind
    {
        /// <summary>플레이어가 고른다. 문구가 반드시 있다.</summary>
        PlayerChoice = 0,

        /// <summary>
        /// 고를 수 있는 것이 하나도 없을 때 자동으로 타는 길(§G6-2).
        /// 에피소드당 하나. <b>문구도 관문도 없다.</b>
        /// </summary>
        AutoAdvance = 1,
    }

    /// <summary>
    /// 에피소드에서 나가는 길 하나 — 저작 쪽 `간선` 시트의 한 행이다.
    ///
    /// <b>관문이 사는 자리가 여기다(§G5, v8).</b> 전에는 에피소드 노드가 표시조건·해금조건을
    /// 들고 있었는데, v8에서 길 단위로 내려왔다. 그래서 "같은 곳으로 가되 조건이 다른 길"을
    /// 여럿 둘 수 있다 — 흔한 패턴이다.
    ///
    /// ⚠ 구 런타임(test13)은 아직 노드 쪽 조건만 읽는다. 같은 JSON을 그쪽에 먹이면
    /// <b>에러 없이 관문이 전부 열린다</b> — 노드의 두 필드가 언제나 빈 배열이기 때문이다.
    ///
    /// <b>생성자가 private이다.</b> <see cref="Auto"/>는 문구·조건·잠금 인자를 <b>아예 받지
    /// 않는다</b> — "자동 진행에 관문이 달림"이 실행 시점 예외가 아니라 컴파일 오류가 된다.
    /// 전에는 생성자가 그 조합을 받아 놓고 뒤에서 던졌다.
    /// </summary>
    public sealed class EpisodeOption
    {
        public OptionKind Kind { get; }

        /// <summary>
        /// 화면에 뜨는 문구. <see cref="OptionKind.AutoAdvance"/>면 빈 문자열이다.
        /// </summary>
        public string ChoiceLabel { get; }

        public string TargetEpisodeId { get; }

        /// <summary>
        /// <b>표시조건</b> — 미달이면 목록에 <b>만들지 않는다</b>(§G5).
        /// 플레이어는 그런 선택지가 있었다는 사실 자체를 모른다.
        /// </summary>
        public IReadOnlyList<ProgressionCondition> VisibleConditions { get; }

        /// <summary>
        /// <b>해금조건</b> — 미달이면 <b>잠긴 채 보인다</b>. <see cref="LockedReasonText"/>가
        /// 왜 잠겼는지 알려 준다. 단 <see cref="HideWhenLocked"/>가 참이면 숨긴다(§G5).
        /// </summary>
        public IReadOnlyList<ProgressionCondition> Conditions { get; }

        public bool HideWhenLocked { get; }
        public string LockedReasonText { get; }

        /// <summary>
        /// 이 길을 타는 순간 <b>원자적으로 1회</b> 커밋되는 증감(§G6-1).
        /// 스탯이 변하는 유일한 자리다 — 에피소드 재생 중에는 변하지 않는다.
        /// </summary>
        public IReadOnlyList<StatChange> StatChanges { get; }

        private EpisodeOption(
            OptionKind kind,
            string choiceLabel,
            string targetEpisodeId,
            IReadOnlyList<ProgressionCondition> visibleConditions,
            IReadOnlyList<ProgressionCondition> conditions,
            bool hideWhenLocked,
            string lockedReasonText,
            IReadOnlyList<StatChange> statChanges)
        {
            if (string.IsNullOrEmpty(targetEpisodeId))
            {
                throw new ArgumentException(
                    "선택지의 도착 에피소드가 비어 있다. 아무 데도 가지 않는 길은 둘 수 없다.",
                    nameof(targetEpisodeId));
            }

            // null을 빈 값으로 받는 것은 조용한 기본값이 아니라 §G2와 같은 규약의 번역이다 —
            // 저작 쪽은 "없음"과 "빈 것"을 구분하지 않고(조건 없는 간선은 빈 배열로 나간다),
            // 역직렬화기는 없는 키를 null로 준다.
            Kind = kind;
            ChoiceLabel = choiceLabel ?? string.Empty;
            LockedReasonText = lockedReasonText ?? string.Empty;
            VisibleConditions = visibleConditions ?? Array.Empty<ProgressionCondition>();
            Conditions = conditions ?? Array.Empty<ProgressionCondition>();
            StatChanges = statChanges ?? Array.Empty<StatChange>();

            TargetEpisodeId = targetEpisodeId;
            HideWhenLocked = hideWhenLocked;

            ProgressionCondition.RequireAllConstructed(
                VisibleConditions, nameof(visibleConditions));
            ProgressionCondition.RequireAllConstructed(Conditions, nameof(conditions));
        }

        /// <summary>
        /// 플레이어가 고르는 길. <paramref name="choiceLabel"/>이 비면 예외다 —
        /// 문구 없는 길은 <see cref="Auto"/>이지 문구가 빈 선택지가 아니다.
        /// </summary>
        public static EpisodeOption Choice(
            string choiceLabel,
            string targetEpisodeId,
            IReadOnlyList<ProgressionCondition> visibleConditions = null,
            IReadOnlyList<ProgressionCondition> conditions = null,
            bool hideWhenLocked = false,
            string lockedReasonText = null,
            IReadOnlyList<StatChange> statChanges = null)
        {
            if (string.IsNullOrEmpty(choiceLabel))
            {
                throw new ArgumentException(
                    $"'{targetEpisodeId}'(으)로 가는 선택지의 문구가 비어 있다. " +
                    "문구 없이 자동으로 타는 길이라면 EpisodeOption.Auto()를 쓸 것.",
                    nameof(choiceLabel));
            }

            return new EpisodeOption(
                OptionKind.PlayerChoice,
                choiceLabel,
                targetEpisodeId,
                visibleConditions,
                conditions,
                hideWhenLocked,
                lockedReasonText,
                statChanges);
        }

        /// <summary>
        /// 고를 수 있는 것이 하나도 없을 때 타는 길(§G6-2). 에피소드당 하나.
        ///
        /// <b>조건 인자가 없다는 것이 이 팩토리의 내용이다.</b> 기본 선택지는 "마지막 길"이라
        /// 여기 관문이 달리면 그 관문마저 막혔을 때 챕터가 조용히 끝나 버린다. 작가는 자기가
        /// 이어 둔 길이 있으므로 끝날 리 없다고 믿는다 — v9가 이것을 오류로 못 박은 이유다.
        /// 인자를 없애면 그 오류가 애초에 표현되지 않는다.
        /// </summary>
        public static EpisodeOption Auto(
            string targetEpisodeId,
            IReadOnlyList<StatChange> statChanges = null)
        {
            return new EpisodeOption(
                OptionKind.AutoAdvance,
                string.Empty,
                targetEpisodeId,
                null,
                null,
                false,
                null,
                statChanges);
        }

        /// <summary>진단 메시지에 그대로 실린다.</summary>
        public override string ToString() =>
            Kind == OptionKind.AutoAdvance
                ? $"(자동) → {TargetEpisodeId}"
                : $"\"{ChoiceLabel}\" → {TargetEpisodeId}";

    }
}
