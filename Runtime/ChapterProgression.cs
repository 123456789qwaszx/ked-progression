using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 챕터 하나의 진행 규칙 전부 — 이 패키지가 다루는 가장 큰 단위다.
    ///
    /// <b>생성자를 통과했다는 것은 다음이 전부 참이라는 뜻이다:</b>
    /// 에피소드 ID와 스탯 키에 중복이 없고, 시작 에피소드가 실재하며, <b>모든 간선이 실재하는
    /// 에피소드에 착지하고</b>, 조건과 스탯변화가 <b>정의된 스탯만</b> 가리키고, bool 스탯의
    /// 어휘가 §G4를 지킨다. 그래서 이 타입을 손에 쥔 쪽 — 전이기와 도달성 증명 — 은
    /// "허공으로 가는 간선"이나 "없는 키를 읽는 조건"을 다시 걱정하지 않아도 된다.
    ///
    /// 그 규칙의 구현은 <see cref="ChapterInvariants"/> 한 곳에 있다. 여기서는 <b>첫 진단에서
    /// 던지고</b>, <see cref="ProgressionLoader"/>는 <b>전부 모아서</b> 낸다 — 여기서 나는
    /// 예외는 로더를 거치지 않은 프로그래머 실수에 대한 마지막 방어선이므로 즉시 멈추는 것이
    /// 맞고, 데이터의 잘못은 한 번에 다 보여 주는 것이 맞다.
    ///
    /// <b>D2 확정 (2026-08-18)</b> — 어느 엔딩인지는 <see cref="EpisodeNode.EndingKey"/>가
    /// 정하고, <see cref="EndingRules"/>는 그 키로 <b>조회되는 표</b>다. 규칙의 조건은
    /// 엔딩을 판정하지 않고, 같은 키에서 다음 챕터가 갈릴 때 갈래를 고른다.
    /// 엔딩을 두 곳에서 정하면 반드시 어긋난다.
    /// </summary>
    public sealed class ChapterProgression
    {
        private readonly Dictionary<string, EpisodeNode> _nodesById;
        private readonly Dictionary<string, StatDefinition> _statsByKey;

        public string ChapterId { get; }
        public string DisplayName { get; }

        /// <summary>챕터 런이 시작하는 에피소드. 반드시 <see cref="Nodes"/>에 있다.</summary>
        public string StartEpisodeId { get; }

        /// <summary>
        /// 이 챕터에서 쓰는 스탯의 정의 — <b>§G7의 빈칸을 채우는 자리다.</b>
        ///
        /// 값(초기·최소·최대·타입)의 주인은 챕터 워크북의 `스탯` 시트이고, 2026-08-18부터
        /// 내보내기가 이것을 챕터 JSON 최상위에 싣는다.
        ///
        /// ⚠ 시나리오 층이 서면 <b>소유가 시나리오로 올라간다</b>(D1) — 챕터가 이어지는데
        /// 초기값이 챕터마다 다르면 "스탯이 챕터를 넘나든다"가 성립하지 않는다. 그때 여기
        /// 실린 초기값은 도달성 증명의 <b>진입 가정</b>으로 역할이 바뀐다.
        /// </summary>
        public IReadOnlyList<StatDefinition> Stats { get; }

        /// <summary>에피소드들. 순서에 의미는 없다 — 화면 순서를 정하는 것은 간선이다.</summary>
        public IReadOnlyList<EpisodeNode> Nodes { get; }

        /// <summary>
        /// 이 챕터에서 나가는 길들 — <b>시나리오 층의 간선이다.</b>
        ///
        /// <b>배열 순서에 뜻이 있다</b>: 같은 엔딩키의 규칙은 적힌 순서대로 보고 처음 맞는
        /// 것을 쓴다. 그리고 같은 키의 <b>마지막 규칙은 조건이 없어야 한다</b> — 그래야
        /// "엔딩에 도달했는데 갈 곳이 없다"가 생기지 않는다(생성자가 강제한다).
        /// </summary>
        public IReadOnlyList<EndingRule> EndingRules { get; }

        /// <summary>
        /// 키로 찾는 스탯 정의. <see cref="ProgressionState.Commit"/>이 경계를 여기서 읽는다.
        /// </summary>
        public IReadOnlyDictionary<string, StatDefinition> StatsByKey => _statsByKey;

        public ChapterProgression(
            string chapterId,
            string displayName,
            string startEpisodeId,
            IReadOnlyList<StatDefinition> stats,
            IReadOnlyList<EpisodeNode> nodes,
            IReadOnlyList<EndingRule> endingRules = null)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("챕터 ID가 비어 있다.", nameof(chapterId));
            }

            ChapterId = chapterId;
            DisplayName = displayName ?? string.Empty;
            StartEpisodeId = startEpisodeId ?? string.Empty;
            Stats = stats ?? Array.Empty<StatDefinition>();
            Nodes = nodes ?? Array.Empty<EpisodeNode>();
            EndingRules = endingRules ?? Array.Empty<EndingRule>();

            var diagnostics = new List<ProgressionDiagnostic>();

            ChapterInvariants.Collect(
                Stats, Nodes, EndingRules, StartEpisodeId,
                diagnostics, out _statsByKey, out _nodesById);

            if (diagnostics.Count > 0)
            {
                // 첫 진단만 싣는다 — 여기까지 온 것은 프로그래머 실수이고, 그때는 스택이
                // 붙은 예외 하나가 목록보다 낫다. 데이터의 잘못은 로더가 전부 모아서 낸다.
                throw new ArgumentException(diagnostics[0].ToString());
            }
        }

        /// <summary>
        /// 에피소드를 ID로 찾는다. 없으면 <c>false</c> — 다만 생성자가 모든 간선의 도착을
        /// 이미 확인했으므로, 전이 중에 여기서 <c>false</c>가 나올 일은 없다.
        /// </summary>
        public bool TryGetNode(string episodeId, out EpisodeNode node)
        {
            if (episodeId == null)
            {
                node = null;
                return false;
            }

            return _nodesById.TryGetValue(episodeId, out node);
        }

        /// <summary>시작 에피소드. 생성자가 실재를 보장한다.</summary>
        public EpisodeNode StartNode => _nodesById[StartEpisodeId];

        /// <summary>
        /// 이 챕터의 스탯 초기값으로 세운 시작 상태.
        ///
        /// ⚠ <b>실제 플레이는 이 길로 시작하지 않는다(D1).</b> 시나리오가
        /// <see cref="ScenarioProgression.CreateInitialState"/>로 한 번만 세운다.
        /// 여기 실린 초기값은 챕터를 <b>단독으로</b> 검증할 때의 진입 가정이다 —
        /// 테스트와 도달성 증명이 그 뜻으로 쓴다.
        /// </summary>
        public ProgressionState CreateInitialState() =>
            ProgressionState.CreateInitial(Stats, ChapterId, StartEpisodeId);

        public override string ToString() =>
            $"{ChapterId}(에피소드 {Nodes.Count}, 스탯 {Stats.Count})";
    }
}
