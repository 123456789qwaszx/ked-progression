using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 시나리오 하나 — 챕터들과 그 사이의 길. 이 패키지가 다루는 가장 큰 단위다.
    ///
    /// <b>D1 확정 (2026-08-18) — 스탯 정의의 주인은 여기다.</b>
    /// 챕터 워크북이 값을 적지만, 챕터가 이어지는 순간 그 초기값이 두 번 의미를 갖는다:
    /// ch01을 끝내고 ch02에 들어갈 때 <c>trust</c>가 ch02의 초기값으로 되돌아가면
    /// "스탯이 챕터를 넘나든다"가 거짓이 된다. 그래서 <b>실제 플레이의 시작값은 시나리오가
    /// 한 번만 세우고</b>, 챕터에 실린 초기값은 <b>도달성 증명의 진입 가정</b>으로 역할이
    /// 바뀐다.
    ///
    /// <b>경계와 타입은 갈리면 안 된다.</b> 초기값과 달리 이 둘이 챕터마다 다르면 증명이
    /// 걷는 상태공간과 실제 플레이가 갈린다 — 계약서 §G7이 경고한 바로 그 상황이다.
    /// 생성자가 그것을 막는다.
    /// </summary>
    public sealed class ScenarioProgression
    {
        private readonly Dictionary<string, ChapterProgression> _chaptersById;
        private readonly Dictionary<string, StatDefinition> _statsByKey;

        public string ScenarioId { get; }
        public string DisplayName { get; }

        /// <summary>새 게임이 시작하는 챕터. 반드시 <see cref="Chapters"/>에 있다.</summary>
        public string StartChapterId { get; }

        /// <summary>스탯 정의의 <b>유일한 원천</b>이다 (D1).</summary>
        public IReadOnlyList<StatDefinition> Stats { get; }

        public IReadOnlyList<ChapterProgression> Chapters { get; }

        public IReadOnlyDictionary<string, StatDefinition> StatsByKey => _statsByKey;

        public ScenarioProgression(
            string scenarioId,
            string displayName,
            string startChapterId,
            IReadOnlyList<StatDefinition> stats,
            IReadOnlyList<ChapterProgression> chapters)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                throw new ArgumentException("시나리오 ID가 비어 있다.", nameof(scenarioId));
            }

            ScenarioId = scenarioId;
            DisplayName = displayName ?? string.Empty;
            StartChapterId = startChapterId ?? string.Empty;
            Stats = stats ?? Array.Empty<StatDefinition>();
            Chapters = chapters ?? Array.Empty<ChapterProgression>();

            var diagnostics = new List<ProgressionDiagnostic>();

            ScenarioInvariants.Collect(
                Stats, Chapters, StartChapterId,
                diagnostics, out _statsByKey, out _chaptersById);

            if (diagnostics.Count > 0)
            {
                throw new ArgumentException(diagnostics[0].ToString());
            }
        }

        public bool TryGetChapter(string chapterId, out ChapterProgression chapter)
        {
            if (chapterId == null)
            {
                chapter = null;
                return false;
            }

            return _chaptersById.TryGetValue(chapterId, out chapter);
        }

        /// <summary>시작 챕터. 생성자가 실재를 보장한다.</summary>
        public ChapterProgression StartChapter => _chaptersById[StartChapterId];

        /// <summary>
        /// 새 게임의 상태. <b>시나리오의 초기값으로 한 번만 세운다</b> — 챕터를 넘어가도
        /// 다시 세우지 않는다(D1).
        /// </summary>
        public ProgressionState CreateInitialState()
        {
            ChapterProgression start = StartChapter;

            return ProgressionState.CreateInitial(
                Stats, start.ChapterId, start.StartEpisodeId);
        }

        public override string ToString() =>
            $"{ScenarioId}(챕터 {Chapters.Count}, 스탯 {Stats.Count})";
    }
}
