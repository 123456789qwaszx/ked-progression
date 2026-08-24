using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ked.Progression.Dto;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 스탯 <b>지정</b>(<see cref="StatChangeKind.Set"/>) — 깃발을 켜고 끄는 칸.
    ///
    /// 이 칸이 없어서 깃발을 쓰는 챕터는 툴이 내보내기를 거부했다. 저작은 8/19에
    /// 끝나 있었고 코어가 받을 자리만 비어 있었다.
    ///
    /// <b>여기서 가장 중요한 것은 "끄기"다.</b> 켜기만 있으면 증감과 다를 바 없어
    /// 단조로 남지만, 끄는 순간 "간선을 지날수록 한 방향으로만 간다"가 깨진다.
    /// 도달성 증명이 그 전제 위에 서 있었다면 여기서 틀린 답을 낸다.
    /// </summary>
    public sealed class BoolStatSetTests
    {
        private static readonly StatDefinition Trust =
            new StatDefinition("trust", "신뢰", StatType.Number, 0, 0, 5);

        private static readonly StatDefinition Flag =
            new StatDefinition("met_willow", "윌로를 만남", StatType.Bool, 0, 0, 1);

        private static EpisodeNode Node(string id, params EpisodeOption[] options) =>
            new EpisodeNode(id, id, "entry_" + id, options);

        private static ChapterProgression Chapter(params EpisodeNode[] nodes) =>
            new ChapterProgression(
                "ch_01", "첫 챕터", "ep_01", new[] { Trust, Flag }, nodes);

        // ── 불변식 — 로더가 앞당기고 타입이 최종적으로 막는다 ────────

        [Test]
        public void 숫자_스탯에_지정을_거부한다()
        {
            // 숫자를 '정하면' 도달성 증명의 스탯 폭이 뜻을 잃는다 — 이 간선을 지난
            // 순간 앞의 모든 경로가 하나로 접히기 때문이다. 깃발에만 연다.
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Set("trust", 1) })),
                Node("ep_02")));
        }

        [Test]
        public void bool_스탯에_정할_값은_0이나_1이다()
        {
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 2) })),
                Node("ep_02")));

            Assert.DoesNotThrow(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 1) })),
                Node("ep_02")));

            // 끄기도 정상이다 — 0은 "값 없음"이 아니라 "끈다"다.
            Assert.DoesNotThrow(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 0) })),
                Node("ep_02")));
        }

        [Test]
        public void 한_간선에서_같은_키를_두_번_정하면_거부한다()
        {
            // 어느 쪽이 사는지가 배열 순서에 달린다 — 시트에서 행을 옮기는 것만으로
            // 결과가 바뀌면 그건 저작자가 볼 수 없는 규칙이다.
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02",
                    statChanges: new[]
                    {
                        StatChange.Set("met_willow", 1),
                        StatChange.Set("met_willow", 0),
                    })),
                Node("ep_02")));
        }

        [Test]
        public void 더하기끼리는_같은_키가_두_번이어도_통과한다()
        {
            // 합쳐지므로 순서와 무관하다. 지정과 달리 뜻이 갈리지 않는다.
            Assert.DoesNotThrow(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02",
                    statChanges: new[]
                    {
                        StatChange.Add("trust", 1),
                        StatChange.Add("trust", 2),
                    })),
                Node("ep_02")));
        }

        [Test]
        public void bool_스탯에_증감은_여전히_거부한다()
        {
            // 지정이 생겼다고 증감이 열리는 것이 아니다. 켜고 끄는 것은 Set이 한다.
            Assert.Throws<ArgumentException>(() => Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Add("met_willow", 1) })),
                Node("ep_02")));
        }

        // ── 커밋 — 지정은 현재 값을 보지 않는다 ─────────────────────

        [Test]
        public void 지정은_현재_값을_보지_않는다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "켠다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 1) })),
                Node("ep_02", EpisodeOption.Choice(
                    "또 켠다", "ep_03", statChanges: new[] { StatChange.Set("met_willow", 1) })),
                Node("ep_03"));

            ProgressionState atEp02 = chapter
                .CreateEntryState()
                .Commit(chapter, chapter.StartNode.NextOptions[0]);

            Assert.That(atEp02.GetStat("met_willow"), Is.EqualTo(1));

            chapter.TryGetNode("ep_02", out EpisodeNode ep02);

            ProgressionState atEp03 = atEp02.Commit(chapter, ep02.NextOptions[0]);

            // 증감이었다면 1+1=2가 clamp되어 1이 된다 — 값은 같아도 뜻이 다르다.
            // 아래 끄기 테스트가 그 차이를 드러낸다.
            Assert.That(atEp03.GetStat("met_willow"), Is.EqualTo(1));
        }

        [Test]
        public void 켠_깃발을_끌_수_있다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "켠다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 1) })),
                Node("ep_02", EpisodeOption.Choice(
                    "끈다", "ep_03", statChanges: new[] { StatChange.Set("met_willow", 0) })),
                Node("ep_03"));

            ProgressionState atEp02 = chapter
                .CreateEntryState()
                .Commit(chapter, chapter.StartNode.NextOptions[0]);

            chapter.TryGetNode("ep_02", out EpisodeNode ep02);

            ProgressionState atEp03 = atEp02.Commit(chapter, ep02.NextOptions[0]);

            Assert.That(atEp02.GetStat("met_willow"), Is.EqualTo(1));
            Assert.That(atEp03.GetStat("met_willow"), Is.EqualTo(0));
        }

        [Test]
        public void 더하기는_그대로_현재_값에_더한다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "간다", "ep_02", statChanges: new[] { StatChange.Add("trust", 2) })),
                Node("ep_02", EpisodeOption.Choice(
                    "또 간다", "ep_03", statChanges: new[] { StatChange.Add("trust", 2) })),
                Node("ep_03"));

            ProgressionState atEp02 = chapter
                .CreateEntryState()
                .Commit(chapter, chapter.StartNode.NextOptions[0]);

            chapter.TryGetNode("ep_02", out EpisodeNode ep02);

            Assert.That(atEp02.Commit(chapter, ep02.NextOptions[0]).GetStat("trust"),
                Is.EqualTo(4));
        }

        // ── 도달성 — 깃발이 관문을 연다 ─────────────────────────────

        [Test]
        public void 깃발을_켜는_길이_있으면_관문_뒤가_도달_가능하다()
        {
            ReachabilityResult result = ChapterReachability.Prove(GateChapter(withFlagPath: true));

            Assert.That(result.ExplorationComplete, Is.True);
            Assert.That(result.IsReachable("ep_gated"), Is.True);
        }

        [Test]
        public void 깃발을_켜는_길이_없으면_관문_뒤가_도달_불가다()
        {
            ReachabilityResult result = ChapterReachability.Prove(GateChapter(withFlagPath: false));

            Assert.That(result.ExplorationComplete, Is.True);
            Assert.That(result.IsReachable("ep_gated"), Is.False);

            UnreachableEpisode blocked = FindUnreachable(result, "ep_gated");

            Assert.That(blocked.Cause, Is.EqualTo(UnreachableCause.BlockedByCondition));
        }

        /// <summary>
        /// <b>단조가 아님을 증명기가 실제로 본다.</b>
        ///
        /// 켰다가 끄고 나면 관문이 다시 닫힌다. "간선을 지날수록 값이 한 방향으로만
        /// 간다"를 전제로 스탯 폭만 들고 걷는 증명기는 여기서 "도달 가능"이라는
        /// 틀린 답을 낸다 — 최댓값 1을 한 번 본 뒤로는 계속 1로 남기 때문이다.
        /// </summary>
        [Test]
        public void 껐다면_관문이_다시_닫힌다()
        {
            ChapterProgression chapter = Chapter(
                Node("ep_01", EpisodeOption.Choice(
                    "켠다", "ep_02", statChanges: new[] { StatChange.Set("met_willow", 1) })),
                Node("ep_02", EpisodeOption.Choice(
                    "끈다", "ep_03", statChanges: new[] { StatChange.Set("met_willow", 0) })),
                Node("ep_03", EpisodeOption.Choice(
                    "문을 연다", "ep_gated",
                    conditions: new[]
                    {
                        ProgressionCondition.Stat("met_willow", ComparisonOp.Equal, 1),
                    })),
                Node("ep_gated"));

            ReachabilityResult result = ChapterReachability.Prove(chapter);

            Assert.That(result.ExplorationComplete, Is.True);
            Assert.That(result.IsReachable("ep_03"), Is.True);
            Assert.That(result.IsReachable("ep_gated"), Is.False,
                "끈 깃발로는 관문이 열리지 않는다 — 증명이 단조를 가정하면 여기서 틀린다.");
        }

        // ── 로더 — 이름 문자열을 종류로 옮긴다 ──────────────────────

        [Test]
        public void Op가_없으면_더하기다()
        {
            // 이 칸이 서기 전에 나간 챕터 JSON이 한 글자도 안 바뀌고 실려야 한다.
            ProgressionLoadResult result = LoadWithChange(new StatChangeDto
            {
                Key = "trust",
                Amount = 2,
            });

            Assert.That(result.HasErrors, Is.False);
            Assert.That(SoleChange(result).Kind, Is.EqualTo(StatChangeKind.Add));
            Assert.That(SoleChange(result).Amount, Is.EqualTo(2));
        }

        [Test]
        public void Op가_Add면_더하기다()
        {
            ProgressionLoadResult result = LoadWithChange(new StatChangeDto
            {
                Key = "trust",
                Amount = 2,
                Op = "Add",
            });

            Assert.That(result.HasErrors, Is.False);
            Assert.That(SoleChange(result).Kind, Is.EqualTo(StatChangeKind.Add));
        }

        [Test]
        public void Op가_Set이면_지정이다()
        {
            ProgressionLoadResult result = LoadWithChange(new StatChangeDto
            {
                Key = "met_willow",
                Amount = 1,
                Op = "Set",
            });

            Assert.That(result.HasErrors, Is.False);
            Assert.That(SoleChange(result).Kind, Is.EqualTo(StatChangeKind.Set));
            Assert.That(SoleChange(result).Amount, Is.EqualTo(1));
        }

        [Test]
        public void 알_수_없는_Op는_진단이다()
        {
            // 규율 1 — Add로 읽어 넘기면 깃발을 켜려던 간선이 아무것도 안 하는
            // 간선이 되고, 그 버그는 재생해 봐도 안 보인다.
            ProgressionLoadResult result = LoadWithChange(new StatChangeDto
            {
                Key = "met_willow",
                Amount = 1,
                Op = "set",   // 대소문자가 다르다
            });

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Diagnostics[0].Message, Does.Contain("set"));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("Add, Set"));
        }

        // ── 잔손 ────────────────────────────────────────────────────

        private static ChapterProgression GateChapter(bool withFlagPath)
        {
            var fromStart = new List<EpisodeOption>();

            if (withFlagPath)
            {
                fromStart.Add(EpisodeOption.Choice(
                    "윌로를 만난다", "ep_02",
                    statChanges: new[] { StatChange.Set("met_willow", 1) }));
            }

            fromStart.Add(EpisodeOption.Choice("그냥 간다", "ep_02"));

            return Chapter(
                new EpisodeNode("ep_01", "ep_01", "entry_ep_01", fromStart),
                Node("ep_02", EpisodeOption.Choice(
                    "문을 연다", "ep_gated",
                    conditions: new[]
                    {
                        ProgressionCondition.Stat("met_willow", ComparisonOp.Equal, 1),
                    })),
                Node("ep_gated"));
        }

        private static UnreachableEpisode FindUnreachable(ReachabilityResult result, string episodeId)
        {
            foreach (UnreachableEpisode entry in result.Unreachable)
            {
                if (string.Equals(entry.EpisodeId, episodeId, StringComparison.Ordinal))
                    return entry;
            }

            Assert.Fail($"'{episodeId}'가 도달 불가 목록에 없다.");
            return null;
        }

        private static ProgressionLoadResult LoadWithChange(StatChangeDto change) =>
            ProgressionLoader.Load(new ChapterProgressionDto
            {
                ChapterId = "ch_01",
                DisplayName = "첫 챕터",
                StartEpisodeId = "ep_01",
                Stats = new List<StatDto>
                {
                    new StatDto
                    {
                        Key = "trust", DisplayName = "신뢰", Type = "Number",
                        Initial = 0, Minimum = 0, Maximum = 5,
                    },
                    new StatDto
                    {
                        Key = "met_willow", DisplayName = "윌로를 만남", Type = "Bool",
                        Initial = 0, Minimum = 0, Maximum = 1,
                    },
                },
                Nodes = new List<EpisodeNodeDto>
                {
                    new EpisodeNodeDto
                    {
                        EpisodeId = "ep_01",
                        Title = "ep_01",
                        DialogueEntryId = "entry_ep_01",
                        NextOptions = new List<EpisodeOptionDto>
                        {
                            new EpisodeOptionDto
                            {
                                TargetEpisodeId = "ep_02",
                                ChoiceLabel = "간다",
                                StatChanges = new List<StatChangeDto> { change },
                            },
                        },
                    },
                    new EpisodeNodeDto
                    {
                        EpisodeId = "ep_02",
                        Title = "ep_02",
                        DialogueEntryId = "entry_ep_02",
                    },
                },
            });

        private static StatChange SoleChange(ProgressionLoadResult result)
        {
            Assert.That(result.IsValid, Is.True, "챕터가 실리지 않았다.");

            return result.Chapter.StartNode.NextOptions[0].StatChanges[0];
        }
    }
}
