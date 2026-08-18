using System;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 에피소드 노드의 계약. §G5(노드에 관문이 없다)와 §G6(배열 순서 = 화면 순서)가 코드가 된다.
    /// </summary>
    public sealed class EpisodeNodeTests
    {
        private static EpisodeNode NodeWith(params EpisodeOption[] options) =>
            new EpisodeNode("ep_01", "첫 에피소드", EpisodeKind.Main, "Chapter1_Ep01", options);

        [Test]
        public void 자동_진행_간선을_찾는다()
        {
            EpisodeNode node = NodeWith(
                EpisodeOption.Choice("설득한다", "ep_02"),
                EpisodeOption.Auto("ep_99"));

            Assert.That(node.TryGetAutoOption(out EpisodeOption option), Is.True);
            Assert.That(option.TargetEpisodeId, Is.EqualTo("ep_99"));
        }

        [Test]
        public void 자동_진행_간선이_없을_수도_있다()
        {
            // 그때는 고를 것이 없으면 챕터 런이 거기서 끝난다(§G6-3).
            EpisodeNode node = NodeWith(EpisodeOption.Choice("설득한다", "ep_02"));

            Assert.That(node.TryGetAutoOption(out EpisodeOption option), Is.False);
            Assert.That(option, Is.Null);
        }

        [Test]
        public void 자동_진행_간선이_둘이면_거부한다()
        {
            // §G6-2 — "고를 것이 없을 때 어디로 가는가"의 답이 배열 순서라는 우연에
            // 달리면 안 된다. 조용히 첫 번째를 고르지 않는다.
            Assert.Throws<ArgumentException>(() => NodeWith(
                EpisodeOption.Auto("ep_02"),
                EpisodeOption.Auto("ep_03")));
        }

        [Test]
        public void 선택지_순서를_바꾸지_않는다()
        {
            // §G6 — `간선` 시트의 행 순서가 곧 화면에 뜨는 순서다. 정렬하지 않는다.
            EpisodeNode node = NodeWith(
                EpisodeOption.Choice("셋째", "ep_04"),
                EpisodeOption.Choice("첫째", "ep_02"),
                EpisodeOption.Choice("둘째", "ep_03"));

            Assert.That(node.NextOptions[0].ChoiceLabel, Is.EqualTo("셋째"));
            Assert.That(node.NextOptions[1].ChoiceLabel, Is.EqualTo("첫째"));
            Assert.That(node.NextOptions[2].ChoiceLabel, Is.EqualTo("둘째"));
        }

        [Test]
        public void 엔딩_후보는_키_하나로_판별된다()
        {
            // bool IsChapterEndingCandidate + string EndingKey는 4조합 중 둘이 무효였다
            // (참인데 키가 빔 / 거짓인데 키가 있음). 아무도 그 둘을 막지 않았고 어느 쪽이
            // 이기는지도 정해져 있지 않았다. 키 하나로 합치면 그 상태가 존재할 수 없다.
            Assert.That(NodeWith().IsEndingCandidate, Is.False);

            var ending = new EpisodeNode(
                "ep_09", "마지막", EpisodeKind.Main, "Chapter1_Ep09",
                endingKey: "ch01_good_end");

            Assert.That(ending.IsEndingCandidate, Is.True);
            Assert.That(ending.EndingKey, Is.EqualTo("ch01_good_end"));

            Assert.That(typeof(EpisodeNode).GetProperty("IsChapterEndingCandidate"), Is.Null);
        }

        [Test]
        public void 에피소드_ID가_비면_거부한다()
        {
            Assert.Throws<ArgumentException>(() =>
                new EpisodeNode(string.Empty, "제목", EpisodeKind.Main, "entry"));
        }

        [Test]
        public void 노드에는_관문이_없다()
        {
            // §G5 — v8에서 표시조건·해금조건이 간선으로 내려갔고, 노드 쪽 두 필드는
            // JSON에만 남아 언제나 빈 배열로 나간다. 모델에 그 빈칸을 옮겨 오면
            // "여기에 조건을 달 수 있다"는 잘못된 여지가 생긴다.
            //
            // 부착(§G9)을 실제로 쓸 때 이 둘을 되살리는 것이 자연스럽고,
            // 그때 이 테스트가 걸린다 — 그때가 결정할 때다.
            Assert.That(typeof(EpisodeNode).GetProperty("VisibleConditions"), Is.Null);
            Assert.That(typeof(EpisodeNode).GetProperty("UnlockConditions"), Is.Null);

            // 같은 이유로 뺀 둘: IndexText(v5 폐지) · Position(저작 레이아웃)
            Assert.That(typeof(EpisodeNode).GetProperty("IndexText"), Is.Null);
            Assert.That(typeof(EpisodeNode).GetProperty("Position"), Is.Null);
        }
    }
}
