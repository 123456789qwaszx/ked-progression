using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 진행 상태 — "지금 어디까지 왔나".
    ///
    /// 불변이다. 바꾸지 않고 새 상태를 돌려준다(규율 4) —
    /// <c>StageState</c>가 <c>Clone()</c> 후 새 것을 내는 것과 같은 규율이다.
    /// 그래서 "선택지를 눌렀을 때의 값"과 "누른 뒤의 값"을 동시에 들고 비교할 수 있다.
    ///
    /// 이 타입이 나중에 <b>세이브가 담을 내용</b>이 된다.
    /// </summary>
    public sealed class ProgressionState
    {
        private readonly Dictionary<string, int> _stats;
        private readonly HashSet<string> _cleared;

        public string CurrentEpisodeId { get; }

        public IReadOnlyDictionary<string, int> Stats => _stats;
        public IReadOnlyCollection<string> ClearedEpisodes => _cleared;

        private ProgressionState(
            string currentEpisodeId,
            Dictionary<string, int> stats,
            HashSet<string> cleared)
        {
            CurrentEpisodeId = currentEpisodeId;
            _stats = stats;
            _cleared = cleared;
        }

        /// <summary>
        /// 스탯 정의의 <see cref="StatDefinition.Initial"/>로 세운 시작 상태.
        /// </summary>
        public static ProgressionState CreateInitial(
            IEnumerable<StatDefinition> stats,
            string startEpisodeId)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            var values = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StatDefinition stat in stats)
            {
                if (values.ContainsKey(stat.Key))
                    throw new ArgumentException($"스탯 키 '{stat.Key}'가 중복 정의됐다.", nameof(stats));

                values[stat.Key] = stat.Initial;
            }

            return new ProgressionState(
                startEpisodeId,
                values,
                new HashSet<string>(StringComparer.Ordinal));
        }

        /// <summary>
        /// 스탯 값. <b>정의되지 않은 키는 0으로 떨어뜨리지 않고 던진다</b> — 규율 1.
        ///
        /// 조용히 0을 주면 <c>trust &gt;= 0</c>이 언제나 참이 되어, 작가가 오타 낸 조건이
        /// "언제나 통과하는 관문"으로 조용히 바뀐다. 그런 종류의 버그는 재생해 봐도 안 보인다.
        /// (코어의 <c>RectNodeTree.GetState("없는키")</c>와 같은 규율이다.)
        /// </summary>
        public int GetStat(string key)
        {
            if (_stats.TryGetValue(key, out int value))
                return value;

            throw new KeyNotFoundException(
                $"정의되지 않은 스탯 '{key}'. 정의된 것: {string.Join(", ", _stats.Keys)}");
        }

        public bool IsCleared(string episodeId) => _cleared.Contains(episodeId);

        /// <summary>현재 에피소드를 클리어로 표시하고 다음으로 옮긴 새 상태.</summary>
        public ProgressionState WithMovedTo(string nextEpisodeId)
        {
            var cleared = new HashSet<string>(_cleared, StringComparer.Ordinal);

            if (!string.IsNullOrEmpty(CurrentEpisodeId))
                cleared.Add(CurrentEpisodeId);

            return new ProgressionState(
                nextEpisodeId,
                new Dictionary<string, int>(_stats, StringComparer.Ordinal),
                cleared);
        }

        /// <summary>
        /// 스탯 변경을 <b>원자적으로 1회</b> 적용한 새 상태 (§G6-1).
        /// 각 값은 정의의 경계로 clamp된다.
        /// </summary>
        public ProgressionState WithStatChanges(
            IReadOnlyDictionary<string, StatDefinition> definitions,
            IEnumerable<StatChange> changes)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            if (changes == null)
                return this;

            var next = new Dictionary<string, int>(_stats, StringComparer.Ordinal);

            foreach (StatChange change in changes)
            {
                if (!definitions.TryGetValue(change.Key, out StatDefinition definition))
                {
                    throw new KeyNotFoundException(
                        $"정의되지 않은 스탯 '{change.Key}'을(를) 변경하려 한다. " +
                        $"정의된 것: {string.Join(", ", definitions.Keys)}");
                }

                if (!next.TryGetValue(change.Key, out int current))
                    current = definition.Initial;

                next[change.Key] = definition.Clamp(current + change.Amount);
            }

            return new ProgressionState(
                CurrentEpisodeId,
                next,
                new HashSet<string>(_cleared, StringComparer.Ordinal));
        }
    }

    /// <summary>선택지가 커밋될 때 적용되는 스탯 증감.</summary>
    public readonly struct StatChange
    {
        public string Key { get; }
        public int Amount { get; }

        public StatChange(string key, int amount)
        {
            Key = key;
            Amount = amount;
        }

        public override string ToString() => $"{Key} {(Amount >= 0 ? "+" : "")}{Amount}";
    }
}
