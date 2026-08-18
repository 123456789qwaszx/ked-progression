using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// 간선의 계약. §G5(관문이 여기 산다)와 §G6-2(문구 없는 자동 진행)가 코드가 된다.
    /// </summary>
    public sealed class EpisodeOptionTests
    {
        private static readonly ProgressionCondition[] Gate =
        {
            ProgressionCondition.Stat("trust", ComparisonOp.GreaterOrEqual, 3),
        };

        [Test]
        public void 종류가_문구가_아니라_타입에_있다()
        {
            Assert.That(EpisodeOption.Auto("ep_02").Kind, Is.EqualTo(OptionKind.AutoAdvance));
            Assert.That(EpisodeOption.Choice("떠난다", "ep_02").Kind,
                Is.EqualTo(OptionKind.PlayerChoice));

            // 자동 진행의 문구는 빈 문자열이지만, 그것이 종류를 정하는 것은 아니다.
            Assert.That(EpisodeOption.Auto("ep_02").ChoiceLabel, Is.EqualTo(string.Empty));
        }

        [Test]
        public void 문구가_빈_선택지를_만들_수_없다()
        {
            // 전에는 이것이 조용히 "기본 선택지"가 됐다. 작가가 엑셀에서 문구 칸을 실수로
            // 지우면 분기가 보이지 않는 자동 진행으로 변신하고, 검증은 통과하고,
            // 게임을 돌려 봐야 알았다. 이제는 만들 수가 없다.
            Assert.Throws<ArgumentException>(() => EpisodeOption.Choice(string.Empty, "ep_02"));
            Assert.Throws<ArgumentException>(() => EpisodeOption.Choice(null, "ep_02"));
        }

        [Test]
        public void 자동_진행에는_관문을_달_자리가_없다()
        {
            // 예외로 막는 것이 아니라 **인자가 없다.** 그 관문마저 막히면 챕터가 조용히
            // 끝나는데(작가는 길을 이어 뒀으므로 끝날 리 없다고 믿는다), 표현할 수 없으면
            // 그 사고가 일어날 수 없다.
            //
            // 이 테스트가 깨진다면 Auto()에 조건 인자를 되살린 것이다 — 그때가 결정할 때다.
            MethodInfo auto = typeof(EpisodeOption)
                .GetMethod(nameof(EpisodeOption.Auto), BindingFlags.Public | BindingFlags.Static);

            string[] parameters = auto.GetParameters().Select(p => p.Name).ToArray();

            // 목록을 통째로 고정하지 않는다 — 연출(viaNodeId)처럼 종류와 직교하는 인자는
            // 앞으로도 늘 수 있고, 그때마다 이 테스트가 깨지면 무엇을 지키려던 건지 흐려진다.
            // 지키려는 것은 **관문이 달릴 자리가 없다**는 것 하나다.
            Assert.That(parameters, Does.Not.Contain("conditions"));
            Assert.That(parameters, Does.Not.Contain("visibleConditions"));
            Assert.That(parameters, Does.Not.Contain("hideWhenLocked"));
            Assert.That(parameters, Does.Not.Contain("lockedReasonText"));
            Assert.That(parameters, Does.Not.Contain("choiceLabel"));

            // 문구가 있으면 같은 관문이 정상이다 — 관문 자체가 금지된 것이 아니다.
            Assert.DoesNotThrow(() =>
                EpisodeOption.Choice("설득한다", "ep_02", conditions: Gate));
        }

        [Test]
        public void 자동_진행도_스탯변화는_커밋한다()
        {
            // 관문만 없다. 자동 진행도 간선이므로 §G6-1의 1회 커밋을 그대로 한다.
            EpisodeOption auto = EpisodeOption.Auto(
                "ep_02", new[] { new StatChange("trust", 1) });

            Assert.That(auto.StatChanges.Count, Is.EqualTo(1));
        }

        [Test]
        public void 도착이_비면_거부한다()
        {
            Assert.Throws<ArgumentException>(() => EpisodeOption.Choice("떠난다", string.Empty));
            Assert.Throws<ArgumentException>(() => EpisodeOption.Choice("떠난다", null));
            Assert.Throws<ArgumentException>(() => EpisodeOption.Auto(null));
        }

        [Test]
        public void 만들어지지_않은_조건이_섞이면_거부한다()
        {
            // default(ProgressionCondition)은 struct라 C#이 언제나 만들 수 있고, 그 값은
            // Key가 null이라 평가기가 무엇을 물어야 할지 알 수 없다. 경계에서 좁혀 두면
            // 평가기가 방어 코드 없이 전체 함수가 된다.
            var leaked = new ProgressionCondition[1];   // 전부 default

            Assert.Throws<ArgumentException>(() =>
                EpisodeOption.Choice("떠난다", "ep_02", conditions: leaked));

            Assert.Throws<ArgumentException>(() =>
                EpisodeOption.Choice("떠난다", "ep_02", visibleConditions: leaked));
        }

        [Test]
        public void 없는_목록은_빈_목록이_된다()
        {
            // 역직렬화기는 없는 키를 null로 준다. "없음"과 "빈 것"을 구분하지 않는 것이
            // 저작 쪽 모양이므로(§G2와 같은 규약), null 검사를 소비자에게 떠넘기지 않는다.
            EpisodeOption option = EpisodeOption.Choice("떠난다", "ep_02");

            Assert.That(option.VisibleConditions, Is.Empty);
            Assert.That(option.Conditions, Is.Empty);
            Assert.That(option.StatChanges, Is.Empty);
            Assert.That(option.LockedReasonText, Is.EqualTo(string.Empty));
        }

        // ── 연출을 매다는 자리 (§H-3) ────────────────────────────────

        [Test]
        public void 연출은_간선의_종류와_직교한다()
        {
            // 관문은 자동 진행에 뜻이 없어 인자를 뺐지만, 연출은 다르다 — 말없이 넘어가는
            // 자리가 오히려 트랜지션 연출의 주 무대다. 팀장이 요구한 "에피소드 사이
            // 트랜지션"이 여기서 산다.
            EpisodeOption choice =
                EpisodeOption.Choice("믿는다", "ep_02", viaNodeId: "fade_trust");

            EpisodeOption auto = EpisodeOption.Auto("ep_02", viaNodeId: "fade_cold");

            Assert.That(choice.ViaNodeId, Is.EqualTo("fade_trust"));
            Assert.That(auto.ViaNodeId, Is.EqualTo("fade_cold"));
            Assert.That(choice.HasVia, Is.True);
            Assert.That(auto.HasVia, Is.True);
        }

        [Test]
        public void 연출은_없어도_된다()
        {
            EpisodeOption plain = EpisodeOption.Choice("떠난다", "ep_02");

            Assert.That(plain.HasVia, Is.False);
            Assert.That(plain.ViaNodeId, Is.EqualTo(string.Empty), "null이 새어 나오지 않는다");
        }

        [Test]
        public void 연출에는_이름_하나만_들어간다()
        {
            // ⚠ 지속시간·이징·색 같은 파라미터가 여기 붙기 시작하면 그때가 경계면이 진짜로
            // 넓어지는 순간이다. 연출의 파라미터는 연출 쪽에서 산다.
            //
            // 이 테스트가 깨진다면 칸이 문자열에서 다른 무엇으로 바뀐 것이다 — 그때가
            // 결정할 때다.
            Assert.That(typeof(EpisodeOption).GetProperty("ViaNodeId").PropertyType,
                Is.EqualTo(typeof(string)));
        }

        [Test]
        public void 공개_생성자가_없다()
        {
            // 팩토리만 열어야 "문구 빈 선택지"와 "관문 달린 자동 진행"이 타이핑되지 않는다.
            Assert.That(typeof(EpisodeOption).GetConstructors(), Is.Empty);
        }
    }
}
