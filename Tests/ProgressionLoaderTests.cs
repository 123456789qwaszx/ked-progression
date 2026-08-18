using System.Collections.Generic;
using System.Linq;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 로더의 계약 — <b>침묵 금지가 실제로 사는 자리다.</b>
    ///
    /// 여기 걸리는 것들은 전부 "그냥 실으면 조용히 사라지거나 조용히 뜻이 바뀌는" 값들이다.
    /// </summary>
    public sealed class ProgressionLoaderTests
    {
        // ── 그릇 ────────────────────────────────────────────────────

        private static StatDto Stat(string key, string type = "Number", int max = 5) =>
            new StatDto
            {
                Key = key, DisplayName = key, Type = type,
                Initial = 0, Minimum = 0, Maximum = max,
            };

        private static EpisodeOptionDto Choice(string label, string target) =>
            new EpisodeOptionDto
            {
                TargetEpisodeId = target,
                ChoiceLabel = label,
                VisibleConditions = new List<ConditionDto>(),
                Conditions = new List<ConditionDto>(),
                LockedReasonText = string.Empty,
                StatChanges = new List<StatChangeDto>(),
            };

        private static EpisodeOptionDto Auto(string target) => Choice(string.Empty, target);

        private static EpisodeNodeDto Node(string id, params EpisodeOptionDto[] options) =>
            new EpisodeNodeDto
            {
                EpisodeId = id, Title = id, IndexText = string.Empty,
                Kind = "Main", DialogueEntryId = "entry_" + id,
                VisibleConditions = new List<ConditionDto>(),
                UnlockConditions = new List<ConditionDto>(),
                NextOptions = new List<EpisodeOptionDto>(options),
                Attachments = new List<object>(),
                IsChapterEndingCandidate = false,
                EndingKey = string.Empty,
                DesignerNote = string.Empty,
                Position = new PositionDto(),
            };

        private static ChapterProgressionDto Chapter(params EpisodeNodeDto[] nodes) =>
            new ChapterProgressionDto
            {
                ChapterId = "ch_01",
                DisplayName = "첫 챕터",
                StartEpisodeId = "ep_01",
                Stats = new List<StatDto> { Stat("trust") },
                Nodes = new List<EpisodeNodeDto>(nodes),
                EndingRules = new List<EndingRuleDto>(),
            };

        private static ChapterProgressionDto TwoStep() =>
            Chapter(Node("ep_01", Choice("간다", "ep_02")), Node("ep_02"));

        private static string Text(ProgressionLoadResult result) =>
            string.Join(" | ", result.Diagnostics.Select(d => d.ToString()));

        // ── 성립 ────────────────────────────────────────────────────

        [Test]
        public void 온전한_DTO는_챕터가_된다()
        {
            ProgressionLoadResult result = ProgressionLoader.Load(TwoStep());

            Assert.That(result.HasErrors, Is.False, Text(result));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Chapter.Nodes.Count, Is.EqualTo(2));
            Assert.That(result.Chapter.StatsByKey.ContainsKey("trust"), Is.True);
        }

        [Test]
        public void 오류가_있으면_챕터를_내지_않는다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].TargetEpisodeId = "ep_99";

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            // 부분 통과를 만들지 않는다. 반쯤 실린 챕터는 재생해 봐야 무엇이 빠졌는지 안다.
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Chapter, Is.Null);
        }

        [Test]
        public void 진단을_첫_오류에서_멈추지_않고_모은다()
        {
            // 첫 오류에서 멈추면 저작자가 고치고 다시 내보내고 또 걸리는 왕복을 여러 번 한다.
            ChapterProgressionDto dto = Chapter(
                Node("ep_01", Choice("가", "없음1"), Choice("나", "없음2")),
                Node("ep_02", Choice("다", "없음3")));

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.Diagnostics.Count, Is.GreaterThanOrEqualTo(3), Text(result));
        }

        [Test]
        public void 진단이_자리를_짚는다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].Conditions.Add(
                new ConditionDto { Kind = "Stat", Key = "trsut", Op = "GreaterOrEqual", IntValue = 3 });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            // "정의되지 않은 스탯"만으로는 워크북 전체를 뒤져야 한다.
            Assert.That(result.Diagnostics.Any(d =>
                d.Path == "Nodes[ep_01].NextOptions[0].Conditions[0]"), Is.True, Text(result));
        }

        // ── 알 수 없는 이름 — 로더만이 할 수 있는 검사 ──────────────

        [Test]
        public void 알_수_없는_스탯_타입을_거부한다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Stats[0].Type = "Int";   // 저작 쪽 이름. 내보내기가 Number로 번역해야 한다

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("Number"));
        }

        [Test]
        public void 알_수_없는_에피소드_종류를_거부한다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].Kind = "main";   // 대소문자가 다르면 다른 이름이다

            Assert.That(ProgressionLoader.Load(dto).HasErrors, Is.True);
        }

        [Test]
        public void 알_수_없는_조건_이름을_거부한다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].Conditions.Add(
                new ConditionDto { Kind = "Flag", Key = "x", Op = "Equal" });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("Stat, EpisodeCleared, ChapterCleared"));
        }

        [Test]
        public void NotEqual은_이름으로도_들어오지_못한다()
        {
            // 일부러 없는 연산이다 — 저작 파서가 닫아 두어 데이터로 나오지 않는다.
            // 넣으면 평가기에 영원히 안 타는 분기가 생긴다.
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].Conditions.Add(
                new ConditionDto { Kind = "Stat", Key = "trust", Op = "NotEqual", IntValue = 1 });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("NotEqual은 일부러 없다"));
        }

        [Test]
        public void Cleared에_다른_연산이_오면_조용히_바꾸지_않는다()
        {
            // 팩토리가 Exists로 고정하므로 그냥 만들면 통과해 버린다. 그건 뜻이 달라지는
            // 변환이므로 로더가 막는다.
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].Conditions.Add(
                new ConditionDto { Kind = "EpisodeCleared", Key = "ep_02", Op = "Equal" });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("Exists만 쓴다"));
        }

        // ── 왔는데 안 쓰는 값 — 조용히 사라지면 안 된다 ─────────────

        [Test]
        public void 노드에_실린_관문을_거부한다()
        {
            // v8에서 관문이 간선으로 내려갔다. 구판 데이터를 그대로 실으면
            // **에러 없이 관문이 전부 열린 채로 돈다** — 게임을 돌려 봐야 안다.
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].UnlockConditions.Add(
                new ConditionDto { Kind = "Stat", Key = "trust", Op = "GreaterOrEqual", IntValue = 3 });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("조용히 사라져"));
        }

        [Test]
        public void 부착이_오면_거부한다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[1].Attachments.Add(new object());

            Assert.That(ProgressionLoader.Load(dto).HasErrors, Is.True);
        }

        // ── 엔딩 규칙 (D2) ──────────────────────────────────────────

        private static ChapterProgressionDto WithEnding(params EndingRuleDto[] rules)
        {
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[1].IsChapterEndingCandidate = true;
            dto.Nodes[1].EndingKey = "ch01_end";
            dto.EndingRules = new List<EndingRuleDto>(rules);
            return dto;
        }

        private static EndingRuleDto Rule(
            string outcome, string next = null, params ConditionDto[] conditions) =>
            new EndingRuleDto
            {
                Outcome = outcome,
                EndingKey = "ch01_end",
                NextChapterId = next,
                Conditions = new List<ConditionDto>(conditions),
            };

        [Test]
        public void 엔딩_규칙의_결과를_문자열로_명시한다()
        {
            ProgressionLoadResult end = ProgressionLoader.Load(WithEnding(Rule("ScenarioEnd")));
            Assert.That(end.HasErrors, Is.False, Text(end));
            Assert.That(end.Chapter.EndingRules[0].Outcome, Is.EqualTo(EndingOutcome.ScenarioEnd));

            // 다음 챕터 칸이 비었는지로 판별하지 않는다 — "끝난다"와 "안 적었다"가
            // 같은 모양이 되면 실수로 게임이 끝나 버린다.
            ChapterProgressionDto missing = WithEnding(Rule("NextChapter"));
            ProgressionLoadResult result = ProgressionLoader.Load(missing);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("EndingRule.Ends()"));
        }

        [Test]
        public void 알_수_없는_엔딩_결과를_거부한다()
        {
            ProgressionLoadResult result = ProgressionLoader.Load(WithEnding(Rule("End")));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("NextChapter, ScenarioEnd"));
        }

        [Test]
        public void 종료인데_다음_챕터가_적혀_있으면_거부한다()
        {
            ProgressionLoadResult result =
                ProgressionLoader.Load(WithEnding(Rule("ScenarioEnd", "ch_02")));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("추측하지 않는다"));
        }

        [Test]
        public void 마지막_규칙에_조건이_달리면_거부한다()
        {
            // 조건이 전부 미달이면 엔딩에 도달하고도 갈 곳이 없다.
            ConditionDto gate =
                new ConditionDto { Kind = "Stat", Key = "trust", Op = "GreaterOrEqual", IntValue = 3 };

            ProgressionLoadResult result =
                ProgressionLoader.Load(WithEnding(Rule("ScenarioEnd", null, gate)));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("갈 곳이 없다"));
        }

        [Test]
        public void 조건_없는_규칙_뒤의_같은_키는_거부한다()
        {
            ConditionDto gate =
                new ConditionDto { Kind = "Stat", Key = "trust", Op = "GreaterOrEqual", IntValue = 3 };

            ProgressionLoadResult result = ProgressionLoader.Load(
                WithEnding(Rule("ScenarioEnd"), Rule("ScenarioEnd", null, gate)));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("영원히 타지 않는다"));
        }

        [Test]
        public void 엔딩키와_규칙이_서로를_덮어야_한다()
        {
            // D2 — 키는 노드가 내고 규칙은 그 키로 조회된다. 한쪽만 있으면 둘 다 문제다.
            ChapterProgressionDto noRule = TwoStep();
            noRule.Nodes[1].IsChapterEndingCandidate = true;
            noRule.Nodes[1].EndingKey = "ch01_end";
            noRule.EndingRules.Add(new EndingRuleDto
            {
                Outcome = "ScenarioEnd", EndingKey = "다른키",
                Conditions = new List<ConditionDto>(),
            });

            ProgressionLoadResult result = ProgressionLoader.Load(noRule);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("맞는 규칙이 없다"));
            Assert.That(Text(result), Does.Contain("영원히 타지 않는다"));
        }

        [Test]
        public void 엔딩_표시와_키가_어긋나면_거부한다()
        {
            // 4조합 중 둘이 무효인데 어느 쪽이 이기는지 정해져 있지 않았다. 추측하지 않는다.
            ChapterProgressionDto onlyFlag = TwoStep();
            onlyFlag.Nodes[1].IsChapterEndingCandidate = true;

            Assert.That(ProgressionLoader.Load(onlyFlag).HasErrors, Is.True);

            ChapterProgressionDto onlyKey = TwoStep();
            onlyKey.Nodes[1].EndingKey = "ch01_good";

            Assert.That(ProgressionLoader.Load(onlyKey).HasErrors, Is.True);

            ChapterProgressionDto both = TwoStep();
            both.Nodes[1].IsChapterEndingCandidate = true;
            both.Nodes[1].EndingKey = "ch01_good";

            ProgressionLoadResult ok = ProgressionLoader.Load(both);
            Assert.That(ok.HasErrors, Is.False, Text(ok));
            Assert.That(ok.Chapter.Nodes[1].EndingKey, Is.EqualTo("ch01_good"));
        }

        // ── 자동 진행 (D5) ──────────────────────────────────────────

        [Test]
        public void 문구가_비면_자동_진행으로_읽고_한_줄로_알린다()
        {
            ChapterProgressionDto dto = Chapter(
                Node("ep_01", Auto("ep_02")),
                Node("ep_02", Auto("ep_03")),
                Node("ep_03"));

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.False, Text(result));
            Assert.That(result.Chapter.Nodes[0].NextOptions[0].Kind,
                Is.EqualTo(OptionKind.AutoAdvance));

            // 간선마다 경고를 내면 흔한 경우라 소음이 되고, 소음은 읽히지 않는다.
            List<ProgressionDiagnostic> warnings = result.Diagnostics
                .Where(d => d.Severity == ProgressionDiagnosticSeverity.Warning).ToList();

            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(warnings[0].Message, Does.Contain("2개"));
        }

        [Test]
        public void 자동_진행에_관문이_달리면_거부한다()
        {
            ChapterProgressionDto dto = Chapter(Node("ep_01", Auto("ep_02")), Node("ep_02"));
            dto.Nodes[0].NextOptions[0].Conditions.Add(
                new ConditionDto { Kind = "Stat", Key = "trust", Op = "GreaterOrEqual", IntValue = 3 });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("조용히 끝난다"));
        }

        [Test]
        public void 자동_진행에_잠금이_달리면_거부한다()
        {
            // Auto()가 그 인자를 안 받으므로 그냥 실으면 조용히 사라진다.
            ChapterProgressionDto dto = Chapter(Node("ep_01", Auto("ep_02")), Node("ep_02"));
            dto.Nodes[0].NextOptions[0].LockedReasonText = "아직 이르다";

            Assert.That(ProgressionLoader.Load(dto).HasErrors, Is.True);
        }

        // ── 챕터 불변식은 생성자와 같은 규칙이다 ────────────────────

        [Test]
        public void 챕터_불변식을_생성자와_같은_규칙으로_본다()
        {
            // ChapterInvariants 한 곳에 있고, 생성자는 던지고 로더는 모은다.
            // 사본이 있으면 한쪽만 고쳐지는 날이 오고 그날 둘의 답이 갈린다.
            ChapterProgressionDto dto = TwoStep();
            dto.Nodes[0].NextOptions[0].StatChanges.Add(
                new StatChangeDto { Key = "없는키", Amount = 1 });

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(Text(result), Does.Contain("정의되지 않은 스탯"));
        }

        [Test]
        public void 시작_에피소드가_없으면_거부한다()
        {
            ChapterProgressionDto dto = TwoStep();
            dto.StartEpisodeId = "ep_00";

            Assert.That(ProgressionLoader.Load(dto).HasErrors, Is.True);
        }

        [Test]
        public void 연출_참조를_그대로_실어_온다()
        {
            // 이 패키지는 그 이름이 무엇을 가리키는지 모른다 — DialogueEntryId와 같은
            // 종류의 통과값이다. 실재를 검사할 수단도 없다(연출 카탈로그가 없다).
            ChapterProgressionDto dto = Chapter(
                Node("ep_01", Choice("믿는다", "ep_02"), Auto("ep_02")),
                Node("ep_02"));

            dto.Nodes[0].NextOptions[0].ViaNodeId = "fade_trust";
            dto.Nodes[0].NextOptions[1].ViaNodeId = "fade_cold";

            ProgressionLoadResult result = ProgressionLoader.Load(dto);

            Assert.That(result.HasErrors, Is.False, Text(result));
            Assert.That(result.Chapter.Nodes[0].NextOptions[0].ViaNodeId, Is.EqualTo("fade_trust"));
            Assert.That(result.Chapter.Nodes[0].NextOptions[1].ViaNodeId, Is.EqualTo("fade_cold"),
                "자동 진행에도 붙는다 — 연출은 간선의 종류와 직교한다");
        }

        [Test]
        public void null_DTO도_진단으로_돌려준다()
        {
            ProgressionLoadResult result = ProgressionLoader.Load((ChapterProgressionDto)null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        }
    }
}
