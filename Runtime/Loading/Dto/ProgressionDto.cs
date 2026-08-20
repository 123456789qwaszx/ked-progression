using System.Collections.Generic;

namespace Ked.Progression.Dto
{
    /// <summary>
    /// 저작 쪽 `ChapterProgressionExporter`가 내는 JSON과 <b>필드 1:1</b>이다.
    ///
    /// <b>이 패키지는 JSON을 파싱하지 않는다(규율 2).</b> 역직렬화는 호스트가 하고, 여기는
    /// 받을 그릇만 갖는다. 그래서 직렬화 어트리뷰트도 <c>[Serializable]</c>도 없다 —
    /// 어느 직렬화기를 쓸지는 호스트의 사정이다.
    ///
    /// <b>모델에 없는 칸도 그대로 남긴다.</b> <c>IndexText</c>(v5 폐지) · <c>Position</c>(저작
    /// 레이아웃) · 노드의 <c>VisibleConditions</c>/<c>UnlockConditions</c>(v8에서 간선으로
    /// 내려감) · <c>Attachments</c>(v1 비범위). 스키마가 1:1이어야 <b>"왔는데 안 쓴다"를
    /// 로더가 볼 수 있다</b> — 필드를 빼 버리면 그 데이터가 조용히 사라진다.
    /// </summary>
    public sealed class ChapterProgressionDto
    {
        public string ChapterId { get; set; }
        public string DisplayName { get; set; }
        public string StartEpisodeId { get; set; }

        /// <summary>2026-08-18 신설 — 이 칸이 비어 있어서 실데이터를 아예 못 싣고 있었다.</summary>
        public List<StatDto> Stats { get; set; }

        public List<EpisodeNodeDto> Nodes { get; set; }

        /// <summary>
        /// 시나리오 층의 간선. <b>툴은 아직 안 낸다</b>(언제나 빈 배열) — 그래서 이 모양은
        /// 손으로 쓰는 시나리오 JSON이 먼저 쓰고, 툴이 나중에 맞춘다(X2).
        /// </summary>
        public List<EndingRuleDto> EndingRules { get; set; }
    }

    /// <summary>
    /// 챕터에서 나가는 길.
    ///
    /// ⚠ <b><see cref="Outcome"/>을 명시로 둔 것이 <see cref="EpisodeOptionDto.ChoiceLabel"/>과
    /// 다른 점이다.</b> 저작 데이터의 간선 종류는 이미 sentinel로 굳어 있어 로더가 번역할 수밖에
    /// 없지만(D5), 이 모양은 아직 아무도 안 쓰므로 처음부터 명시할 수 있다.
    /// <c>NextChapterId</c>가 비었는지로 판별하면 "다음 챕터를 실수로 안 적음"과
    /// "여기서 끝남"이 같은 모양이 된다.
    /// </summary>
    public sealed class EndingRuleDto
    {
        /// <summary><c>"NextChapter"</c> 또는 <c>"ScenarioEnd"</c>.</summary>
        public string Outcome { get; set; }

        public string EndingKey { get; set; }
        public string DisplayName { get; set; }
        public List<ConditionDto> Conditions { get; set; }

        /// <summary><c>Outcome</c>이 <c>ScenarioEnd</c>면 비어 있어야 한다.</summary>
        public string NextChapterId { get; set; }

        public string DesignerNote { get; set; }
    }

    /// <summary>
    /// 시나리오 하나. <b>이 모양은 이 패키지가 정한다</b> — 툴에 시나리오 저작이 아직 없어
    /// 손으로 쓴 JSON이 먼저 온다(X4).
    /// </summary>
    public sealed class ScenarioProgressionDto
    {
        public string ScenarioId { get; set; }
        public string DisplayName { get; set; }
        public string StartChapterId { get; set; }

        /// <summary>D1 — 스탯 정의의 유일한 원천이다.</summary>
        public List<StatDto> Stats { get; set; }

        public List<ChapterProgressionDto> Chapters { get; set; }
    }

    /// <summary>
    /// ⚠ <see cref="Type"/>은 <c>"Number"</c> 또는 <c>"Bool"</c>이다.
    /// 저작 쪽 enum은 <c>Int</c>지만 내보내기가 이름을 번역해서 낸다.
    /// </summary>
    public sealed class StatDto
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public int Initial { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }

    public sealed class EpisodeNodeDto
    {
        public string EpisodeId { get; set; }
        public string Title { get; set; }

        /// <summary>v5에서 폐지됐다. 언제나 빈 문자열 — 통과값이다.</summary>
        public string IndexText { get; set; }

        public string Kind { get; set; }
        public string DialogueEntryId { get; set; }

        /// <summary>v8에서 간선으로 내려갔다. 언제나 빈 배열이어야 한다 — 로더가 확인한다.</summary>
        public List<ConditionDto> VisibleConditions { get; set; }

        /// <summary>v8에서 간선으로 내려갔다. 언제나 빈 배열이어야 한다 — 로더가 확인한다.</summary>
        public List<ConditionDto> UnlockConditions { get; set; }

        public List<EpisodeOptionDto> NextOptions { get; set; }

        /// <summary>v1 비범위(§G9). 비어 있지 않으면 로더가 오류를 낸다.</summary>
        public List<object> Attachments { get; set; }

        /// <summary>
        /// ⚠ 모델에는 없다. <see cref="EndingKey"/> 하나로 판별하고, 이 값과 어긋나면
        /// 로더가 오류를 낸다 — 어느 쪽이 이기는지 추측하지 않는다.
        /// </summary>
        public bool IsChapterEndingCandidate { get; set; }

        public string EndingKey { get; set; }
        public string DesignerNote { get; set; }

        /// <summary>저작 레이아웃(G-2 확장). 평가 입력이 아니다 — 통과값이다.</summary>
        public PositionDto Position { get; set; }
    }

    public sealed class EpisodeOptionDto
    {
        public string TargetEpisodeId { get; set; }

        /// <summary>
        /// ⚠ <b>비어 있으면 자동 진행으로 읽는다.</b> 저작 데이터에 종류 열이 없어서 생긴
        /// 유일한 sentinel이고, <b>그 해석이 일어나는 자리는 로더 한 곳뿐이다</b>(D5).
        /// </summary>
        public string ChoiceLabel { get; set; }

        public List<ConditionDto> VisibleConditions { get; set; }
        public List<ConditionDto> Conditions { get; set; }
        public bool HideWhenLocked { get; set; }
        public string LockedReasonText { get; set; }
        public List<StatChangeDto> StatChanges { get; set; }

        /// <summary>
        /// 연출을 매다는 자리(계약서 §H-3). 이 길을 지나며 먼저 거쳐 가는 <b>Yarn 노드</b>
        /// 이름이고, 비어 있으면 곧장 간다. 에피소드 사이 트랜지션과 엔딩 연출이 같은 칸을 쓴다.
        ///
        /// ⚠ 이름 하나만 온다. 지속시간·이징 같은 파라미터가 여기 붙기 시작하면
        /// 경계면이 넓어진다 — 그건 연출 쪽에서 산다.
        /// </summary>
        public string ViaNodeId { get; set; }
    }

    public sealed class ConditionDto
    {
        public string Kind { get; set; }
        public string Key { get; set; }
        public string Op { get; set; }

        /// <summary>
        /// ⚠ §G2 — 저작 쪽은 <b>0을 키 자체를 생략해서</b> 내보낸다. 그래서 이 필드는
        /// 반드시 <c>int</c>여야 한다. <c>int?</c>로 두면 "없음"과 "0"이 갈려서 가장 흔한
        /// 조건인 <c>flag == false</c>(= Equal 0)가 통째로 어긋난다.
        /// </summary>
        public int IntValue { get; set; }
    }

    public sealed class StatChangeDto
    {
        public string Key { get; set; }
        public int Amount { get; set; }
    }

    public sealed class PositionDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
