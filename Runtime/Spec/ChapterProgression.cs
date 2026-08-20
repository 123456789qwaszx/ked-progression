using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 챕터 하나의 진행 규칙.
    //
    // 생성자 보장:
    // [1]에피소드 ID와 스탯 키에 중복이 없고,
    // [2]시작 에피소드가 실재하며,
    // [3]모든 간선이 실재하는 에피소드에 착지하고,
    // [4] 조건과 스탯변화가 정의된 스탯만 가리킴.
    //
    // 그 규칙의 구현 = ChapterInvariants
    // 
    // 어느 엔딩인지는 "EpisodeNode.EndingKey"가 정하고,
    // "EndingRules"는 그 키로 조회되는 표.
    public sealed class ChapterProgression
    {
        private readonly Dictionary<string, EpisodeNode> _nodesById;
        private readonly Dictionary<string, StatDefinition> _statsByKey;

        public string ChapterId { get; }
        public string DisplayName { get; }

        public string StartEpisodeId { get; }

        // 이 챕터에서 쓰는 스탯의 정의
        public IReadOnlyList<StatDefinition> Stats { get; }

        // 에피소드들. 순서 무관. 간선을 통해 연결되기 때문.
        public IReadOnlyList<EpisodeNode> Nodes { get; }

        public IReadOnlyList<EndingRule> EndingRules { get; }

        // 키로 찾는 스탯 정의. "ProgressionState.Commit"이 쓰는 시스템 경계.
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
                throw new ArgumentException(diagnostics[0].ToString());
        }

        // 에피소드를 ID로 찾는다. 생성자 보장.
        public bool TryGetNode(string episodeId, out EpisodeNode node)
        {
            if (episodeId == null)
            {
                node = null;
                return false;
            }

            return _nodesById.TryGetValue(episodeId, out node);
        }

        public EpisodeNode StartNode => _nodesById[StartEpisodeId];

        /// <summary>
        /// 이 챕터의 스탯 초기값으로 세운 시작 상태.
        /// 실제 플레이가 아닌 테스트용.
        /// </summary>
        public ProgressionState CreateInitialState() =>
            ProgressionState.CreateInitial(Stats, ChapterId, StartEpisodeId);

        public override string ToString() =>
            $"{ChapterId}(에피소드 {Nodes.Count}, 스탯 {Stats.Count})";
    }
}