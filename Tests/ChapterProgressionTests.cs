using System;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 챕터의 계약 — <b>"이 타입을 쥐었다면 무엇이 참인가"</b>를 고정한다.
    ///
    /// 여기 걸리는 것들이 전부 규율 1(침묵 금지)이다. 조용히 통과시키면 전부
    /// "재생해 봐도 안 보이는" 종류의 버그가 된다.
    /// </summary>
    public sealed class ChapterProgressionTests
    {
        private static readonly StatDefinition Trust =
            new StatDefinition("trust", "신뢰", StatType.Number, 0, -5, 5);

        private static readonly StatDefinition Flag =
            new StatDefinition("met_willow", "윌로를 만남", StatType.Bool, 0, 0, 1);

        private static EpisodeNode Node(string id, params EpisodeOption[] options) =>
            new EpisodeNode(id, id, EpisodeKind.Main, "entry_" + id, options);

        private static ChapterProgression Chapter(params EpisodeNode[] nodes) =>
            new ChapterProgression(
                "ch_01", "첫 챕터", "ep_01", new[] { Trust, Flag }, nodes);

        private static ChapterProgression TwoStep(params ProgressionCondition[] gate) =>
            Chapter(
                Node("ep_01", EpisodeOption.Choice("간다", "ep_02", conditions: gate)),
                Node("ep_02"));

        // ── 뼈대 ────────────────────────────────────────────────────

        [Test]
        public void 에피소드를_ID로_찾는다()
        {
            ChapterProgression chapter = TwoStep();

            Assert.That(chapter.TryGetNode("ep_02", out EpisodeNode found), Is.True);
            Assert.That(found.EpisodeId, Is.EqualTo("ep_02"));

            Assert.That(chapter.TryGetNode("ep_99", out _), Is.False);
            Assert.That(chapter.TryGetNode(null, out _), Is.False);

            Assert.That(chapter.StartNode.EpisodeId, Is.EqualTo("ep_01"));
        }

        [Test]
        public void 스탯_초기값으로_시작_상태를_만든다()
        {
            ProgressionState state = TwoStep().CreateProofEntryState();

            Assert.That(state.CurrentEpisodeId, Is.EqualTo("ep_01"));
            Assert.That(state.GetStat("trust"), Is.EqualTo(0));
            Assert.That(state.ClearedEpisodeIds, Is.Empty);
        }

        // ── 커밋 = 트랜잭션 경계 ────────────────────────────────────

        [Test]
        public void 커밋은_스탯과_이동을_함께_한다()
        {
            // 셋이 한 연산이다 — 스탯 반영 · 클리어 표시 · 이동. 따로 부를 수 있으면
            // 언젠가 따로 불리고, 그 순간 "스탯만 바뀌고 안 옮겨 간" 상태가 생긴다.
            EpisodeOption exit = EpisodeOption.Choice(
                "간다", "ep_02", statChanges: new[] { new StatChange("trust", 99) });

            ChapterProgression chapter = Chapter(Node("ep_01", exit), Node("ep_02"));

            ProgressionState after = chapter.CreateProofEntryState().Commit(chapter, exit);

            Assert.That(after.GetStat("trust"), Is.EqualTo(5), "경계로 clamp된다");
            Assert.That(after.CurrentEpisodeId, Is.EqualTo("ep_02"));
            Assert.That(after.IsEpisodeCleared("ep_01"), Is.True);
        }

        [Test]
        public void 스탯만_바꾸거나_이동만_할_수_없다()
        {
            // 나뉘어 있으면 반쪽 상태가 만들어진다 — §3.3이 막으려던 중복 가산이다.
            // 이 테스트가 깨진다면 둘을 다시 갈라 놓은 것이다.
            Assert.That(typeof(ProgressionState).GetMethod("WithStatChanges"), Is.Null);
            Assert.That(typeof(ProgressionState).GetMethod("WithMovedTo"), Is.Null);
        }

        [Test]
        public void 지금_에피소드의_길이_아니면_커밋을_거부한다()
        {
            // 챕터 생성자는 "모든 간선이 실재하는 노드에 착지한다"까지만 보장한다.
            // 엉뚱한 노드의 선택지를 넘기면 그래프에 없는 경로로 이동한 상태가 만들어지고,
            // 도달성 증명이 보증한 것과 실제 플레이가 갈린다.
            EpisodeOption fromEp01 = EpisodeOption.Choice("간다", "ep_02");
            EpisodeOption fromEp02 = EpisodeOption.Choice("돌아간다", "ep_01");

            ChapterProgression chapter = Chapter(
                Node("ep_01", fromEp01),
                Node("ep_02", fromEp02));

            ProgressionState atEp01 = chapter.CreateProofEntryState();

            Assert.DoesNotThrow(() => atEp01.Commit(chapter, fromEp01));
            Assert.Throws<ArgumentException>(() => atEp01.Commit(chapter, fromEp02));
        }

        // ── 뻗은 참조는 만들어지지 않는다 ────────────────────────────

        [Test]
        public void 시작_에피소드가_없으면_거부한다()
        {
            Assert.Throws<ArgumentException>(() => new ChapterProgression(
                "ch_01", "첫 챕터", "ep_00", new[] { Trust }, new[] { Node("ep_01") }));
        }

        [Test]
        public void 허공으로_가는_간선을_거부한다()
        {
            // 전이가 갈 곳 없이 떨어진다. 이 검사가 있으면 전이기와 도달성 증명이
            // "없는 도착"을 다시 걱정하지 않아도 된다.
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice("간다", "ep_99"))));
        }

        [Test]
        public void 중복된_에피소드_ID를_거부한다()
        {
            Assert.Throws<ArgumentException>(() => Chapter(Node("ep_01"), Node("ep_01")));
        }

        [Test]
        public void 중복된_스탯_키를_거부한다()
        {
            Assert.Throws<ArgumentException>(() => new ChapterProgression(
                "ch_01", "첫 챕터", "ep_01",
                new[] { Trust, Trust }, new[] { Node("ep_01") }));
        }

        // ── 정의되지 않은 스탯 ──────────────────────────────────────

        [Test]
        public void 조건이_없는_스탯을_가리키면_거부한다()
        {
            // 오타 하나가 "언제나 통과하는 관문"이 된다 — 재생해 봐도 안 보이는 버그다.
            Assert.Throws<ArgumentException>(() => TwoStep(
                ProgressionCondition.Stat("trsut", ComparisonOp.GreaterOrEqual, 3)));
        }

        [Test]
        public void 스탯변화가_없는_스탯을_가리키면_거부한다()
        {
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { new StatChange("trsut", 1) })),
                Node("ep_02")));
        }

        // ── §G4 bool 스탯의 어휘 ────────────────────────────────────

        [Test]
        public void bool_스탯에_크기_비교를_거부한다()
        {
            // 값 공간이 0·1 하나뿐이라 크기 비교가 의미를 갖지 않는다.
            // 저작 쪽도 같은 자리에서 막는다(ChapterWorkbookReader.VerifyBoolStatUsage).
            Assert.Throws<ArgumentException>(() => TwoStep(
                ProgressionCondition.Stat("met_willow", ComparisonOp.GreaterOrEqual, 1)));

            Assert.DoesNotThrow(() => TwoStep(
                ProgressionCondition.Stat("met_willow", ComparisonOp.Equal, 1)));
        }

        [Test]
        public void bool_스탯의_비교값은_0이나_1이다()
        {
            Assert.Throws<ArgumentException>(() => TwoStep(
                ProgressionCondition.Stat("met_willow", ComparisonOp.Equal, 2)));

            // §G2 — 값이 생략되면 0이다. flag == false가 이 모양으로 온다.
            Assert.DoesNotThrow(() => TwoStep(
                ProgressionCondition.Stat("met_willow", ComparisonOp.Equal)));
        }

        [Test]
        public void bool_스탯에_증감을_거부한다()
        {
            // 0/1 사이를 +1로 오가면 clamp에 걸려 한 방향으로만 간다.
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { new StatChange("met_willow", 1) })),
                Node("ep_02")));
        }

        // ── Cleared 계열 ────────────────────────────────────────────

        [Test]
        public void Cleared_조건은_챕터가_대상의_실재를_보지_않는다()
        {
            // 에피소드 오타는 "영원히 안 열리는 관문"이 되는데(fail-closed) 그건 도달성
            // 증명이 잡는 자리다. 챕터 오타는 시나리오가 잡는다 — 챕터는 개수가 적고
            // 오타가 곧 시나리오가 끊기는 것이라 fail-closed로 두지 않는다.
            Assert.DoesNotThrow(() => TwoStep(ProgressionCondition.EpisodeCleared("ep_99")));
            Assert.DoesNotThrow(() => TwoStep(ProgressionCondition.ChapterCleared("ch_99")));
        }

        // ── EndingRules ─────────────────────────────────────────────

        // ── EndingRules (D2) ────────────────────────────────────────

        private static EpisodeNode Ending(string id, string endingKey) =>
            new EpisodeNode(id, id, EpisodeKind.Main, "entry_" + id, null, endingKey);

        private static ChapterProgression WithEnding(params EndingRule[] rules) =>
            new ChapterProgression(
                "ch_01", "첫 챕터", "ep_01", new[] { Trust, Flag },
                new[] { Node("ep_01", EpisodeOption.Choice("간다", "ep_02")), Ending("ep_02", "ch01_end") },
                rules);

        [Test]
        public void 엔딩키를_내는_노드에_규칙이_없으면_거부한다()
        {
            // D2 — 키는 노드가 정하고 규칙은 그 키로 조회된다. 규칙이 없으면 엔딩에
            // 도달하고도 갈 곳이 없다.
            Assert.Throws<ArgumentException>(() =>
                WithEnding(EndingRule.Ends("다른키")));

            Assert.DoesNotThrow(() => WithEnding(EndingRule.Ends("ch01_end")));
        }

        [Test]
        public void 아무도_안_내는_엔딩키의_규칙을_거부한다()
        {
            // 반대 방향. 영원히 안 타는 분기를 데이터로도 만들지 않는다.
            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                WithEnding(EndingRule.Ends("ch01_end"), EndingRule.Ends("유령키")));

            Assert.That(error.Message, Does.Contain("영원히 타지 않는다"));
        }

        [Test]
        public void 같은_키의_마지막_규칙은_조건이_없어야_한다()
        {
            ProgressionCondition[] gate =
            {
                ProgressionCondition.Stat("trust", ComparisonOp.GreaterOrEqual, 3),
            };

            // 조건이 전부 미달이면 갈 곳이 없어진다 — 그때 무슨 일이 일어나야 하는지
            // 아무도 안 적었으므로 런타임이 추측하게 된다.
            Assert.Throws<ArgumentException>(() =>
                WithEnding(EndingRule.Ends("ch01_end", gate)));

            // 조건 있는 것 뒤에 무조건 성립하는 것이 오면 정상이다.
            Assert.DoesNotThrow(() => WithEnding(
                EndingRule.Ends("ch01_end", gate),
                EndingRule.Ends("ch01_end")));
        }

        [Test]
        public void 엔딩_규칙이_없는_챕터도_성립한다()
        {
            // 단일 챕터 시나리오 — 엔딩이 곧 종착이다. 규칙이 하나라도 있을 때만
            // 노드의 키가 그 안에 있어야 한다.
            Assert.DoesNotThrow(() => WithEnding());
        }
    }
}
