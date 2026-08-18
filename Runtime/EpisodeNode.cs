using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 에피소드의 종류. 저작 쪽은 이름 문자열로 내보낸다(§G1).
    /// </summary>
    public enum EpisodeKind
    {
        /// <summary>본줄기. 들어오는 간선을 타고 도달한다.</summary>
        Main = 0,

        /// <summary>
        /// 부착. <b>들어오는 간선이 없다</b> — v8에서 관문이 간선으로 내려가면서
        /// 표시 조건이 갈 곳을 잃었다(§G9). v1 비범위이며, 쓸 때 노드 쪽 표시 조건을
        /// 부착에 한해 되살리는 것이 자연스럽다.
        /// </summary>
        Attachment = 1,
    }

    /// <summary>
    /// 에피소드 하나. 대본 한 덩어리와, 거기서 나가는 길들이다.
    ///
    /// <b>이 타입에 관문이 없다는 것이 핵심이다.</b> v8에서 표시조건·해금조건이
    /// <see cref="EpisodeOption"/>으로 내려갔고(§G5), 노드 쪽 두 필드는 스키마 1:1을 위해
    /// JSON에만 남아 언제나 빈 배열로 나간다. 모델에 그 빈칸을 옮겨 오면 "여기에 조건을 달 수
    /// 있다"는 잘못된 여지가 생기므로 <b>일부러 뺐다</b>. <c>IndexText</c>(v5 폐지)와
    /// <c>Position</c>(저작 레이아웃)도 같은 이유로 없다.
    /// </summary>
    public sealed class EpisodeNode
    {
        private readonly int _autoOptionIndex;

        public string EpisodeId { get; }
        public string Title { get; }
        public EpisodeKind Kind { get; }

        /// <summary>
        /// 호스트가 재생할 대본의 키. <b>이 패키지는 그 내용을 모른다</b> —
        /// Yarn 재생은 이 층의 일이 아니고, 여기서는 문자열일 뿐이다.
        /// 이 문자열 하나가 진행 층과 대사 층의 경계면 전부다.
        /// </summary>
        public string DialogueEntryId { get; }

        /// <summary>
        /// 나가는 길들. <b>배열 순서가 곧 화면에 뜨는 순서다</b>(§G6) —
        /// 저작 쪽 `간선` 시트의 행 순서가 그대로 온다. 정렬하지 않는다.
        /// </summary>
        public IReadOnlyList<EpisodeOption> NextOptions { get; }

        /// <summary>
        /// 이 노드로 챕터가 끝나면 어느 엔딩인가. <b>비어 있으면 엔딩 후보가 아니다.</b>
        ///
        /// ⚠ <c>bool IsChapterEndingCandidate</c>를 <b>없앴다.</b> 저작 스키마와 구 런타임에는
        /// bool과 키가 두 필드로 있는데, 4조합 중 둘(참인데 키가 빔 / 거짓인데 키가 있음)이
        /// 무효다. 아무도 그 둘을 막지 않았고 어느 쪽이 이기는지도 정해져 있지 않았다.
        /// 키 하나로 합치면 그 상태가 존재할 수 없다.
        ///
        /// DTO에는 스키마 1:1로 두 필드가 남고, <b>로더가 불일치를 진단한다.</b>
        /// </summary>
        public string EndingKey { get; }

        public string DesignerNote { get; }

        /// <summary>이 노드로 챕터가 끝날 수 있는가 — <see cref="EndingKey"/>가 있는가.</summary>
        public bool IsEndingCandidate => EndingKey.Length != 0;

        public EpisodeNode(
            string episodeId,
            string title,
            EpisodeKind kind,
            string dialogueEntryId,
            IReadOnlyList<EpisodeOption> nextOptions = null,
            string endingKey = null,
            string designerNote = null)
        {
            if (string.IsNullOrEmpty(episodeId))
            {
                throw new ArgumentException("에피소드 ID가 비어 있다.", nameof(episodeId));
            }

            EpisodeId = episodeId;
            Title = title ?? string.Empty;
            Kind = kind;
            DialogueEntryId = dialogueEntryId ?? string.Empty;
            NextOptions = nextOptions ?? Array.Empty<EpisodeOption>();
            EndingKey = endingKey ?? string.Empty;
            DesignerNote = designerNote ?? string.Empty;

            _autoOptionIndex = FindAutoOption(EpisodeId, NextOptions);
        }

        /// <summary>
        /// 문구 없는 자동 진행 간선(§G6-2). 고를 수 있는 것이 하나도 없을 때 타는 길이다.
        /// 없으면 <c>false</c> — 그때는 챕터 런이 거기서 끝난다(§G6-3).
        /// </summary>
        public bool TryGetAutoOption(out EpisodeOption option)
        {
            if (_autoOptionIndex < 0)
            {
                option = null;
                return false;
            }

            option = NextOptions[_autoOptionIndex];
            return true;
        }

        public override string ToString() => $"{EpisodeId}({Kind})";

        /// <summary>
        /// 자동 진행 간선은 <b>에피소드당 하나</b>다(§G6-2). 둘이면 "고를 수 있는 것이 없을 때
        /// 어디로 가는가"의 답이 배열 순서라는 우연에 달리게 된다 — 작가가 통제할 수 없는
        /// 자리이므로 조용히 첫 번째를 고르지 않는다.
        /// </summary>
        private static int FindAutoOption(
            string episodeId, IReadOnlyList<EpisodeOption> options)
        {
            int found = -1;

            for (int i = 0; i < options.Count; i++)
            {
                EpisodeOption option = options[i];

                if (option == null)
                {
                    throw new ArgumentException(
                        $"에피소드 '{episodeId}'의 {i}번째 선택지가 null이다.", nameof(options));
                }

                if (option.Kind != OptionKind.AutoAdvance)
                {
                    continue;
                }

                if (found >= 0)
                {
                    throw new ArgumentException(
                        $"에피소드 '{episodeId}'에 자동 진행 간선이 둘 이상이다: " +
                        $"[{found}] → {options[found].TargetEpisodeId}, " +
                        $"[{i}] → {option.TargetEpisodeId}. " +
                        "에피소드당 하나여야 한다(§G6-2).",
                        nameof(options));
                }

                found = i;
            }

            return found;
        }
    }
}
