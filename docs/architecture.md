# 아키텍처 — 원칙과 타입

기준: 2026-08-18 · **이 문서가 타입의 정본이다.** 코드와 갈리면 코드가 이기지만,
갈렸다는 것은 둘 중 하나가 틀렸다는 뜻이므로 반드시 맞춘다.

[`model-draft.md`](model-draft.md)는 v0 기록으로 남는다 — 이 문서가 그것을 대체한다.

---

## 0. 왜 다시 짜는가

같은 도메인을 **세 번** 구현한 결과가 저장소 셋에 흩어져 있다. 형태는 셋 다 비슷한데
품질이 갈렸고, **갈린 이유가 문법이 아니라 규율이었다.**

| 구현 | 형태 | 무너진 지점 |
|---|---|---|
| 런타임 `EpisodeSelection` (`test13`) | 노드·간선·조건·엔딩규칙 — **맞다** | 평가기가 조용히 틀린다. 조건이 필드 자루다 |
| 런타임 `PresentationCore/Flow` (`test13`·`phase2-*`) | 토큰·계획·커서·인플라이트 — **훌륭하다** | 실패 처리(`default: return false` = 데드락) |
| VnTool `Vn.Authoring.Chapters` (`master`, 살아 있음) | 간선에 관문, 스탯 경계, 검증기 — **맞다** | 엔진/엑셀에 묶여 이식 불가 |

셋이 독립적으로 거의 같은 골격에 도달했다. **형태는 이미 검증됐다.** 이 저장소가 하는 일은
새 형태를 발명하는 게 아니라 **같은 형태를 규율 있게 한 번 더 쓰는 것**이다.

> **⚠ 앞의 둘은 멈춘 브랜치의 것이다 (2026-08-18 실측).** 런타임 mainline `dev`(08-17)에는
> `EpisodeSelection`도 `PresentationCore/Flow`도 **0파일**이다 — 진행·세이브·변수 저장소가
> 통째로 걷혔다.
>
> **여기서 뽑은 것은 코드가 아니라 원칙이므로 그대로 남는다.** `GateToken.Immediately`가
> 가르쳐 준 것("없음은 값이어야 한다")은 그 파일이 어느 브랜치에 있든 참이다. 다만
> *"런타임에 이미 있으니 붙이면 된다"*는 종류의 논증은 이 문서 어디에도 없어야 하고,
> 실제로 없다 — §2의 타입들은 전부 이 저장소가 세운 것이다.

---

## 1. 원칙 다섯

### P1 — 불가능한 상태를 만들 수 없게 한다

`GateToken`에서 배운다:

> `Immediately` — *"No wait. **An explicit token used to represent 'none'.**"*

없음이 null도 빈 문자열도 아닌 **타입 안의 한 항목**이다. 이 규칙의 실제 형태는 셋이다.

- **생성자를 private으로 하고 팩토리만 연다.** 무효 조합이 *타이핑되지 않는다*
- **sentinel 쌍을 없앤다.** `bool IsX` + `string XKey`는 4조합 중 2개가 무효다. 키 하나로 합친다
- **함께여야 하는 연산은 함께 노출한다.** 따로 부를 수 있으면 언젠가 따로 불린다

우선순위: **타입이 막는 것 > 생성자가 막는 것 > 로더가 모으는 것.** 위로 올릴 수 있으면 올린다.

### P2 — 검증은 경계에, 안쪽은 전체 함수

평가기에 `default: return false`가 없다. **모르는 것은 로드 시점에 죽는다.**

test13에서 이게 무너진 값이 무엇이었는지가 근거다 — `EpisodeConditionEvaluator.cs:100`은
null 조건을 통과시키고, `:193`은 `Equal`에서 `BoolValue`를 안 보고 `exists`를 돌려준다.
`StepGateAdvancer.cs:79`의 `default: return false`는 **모르는 토큰에서 영원히 블록된다.**

경계에서 좁히면 안쪽 함수는 전체 함수가 되고, **방어 코드를 쓸 이유가 사라진다.**
안쪽에 방어 코드가 보이면 그것이 곧 경계가 새고 있다는 신호다.

### P3 — 스펙 / 진행 / 해석을 섞지 않는다

`GateInFlight`에서 배운다 — *"meaningful only while the token is in-flight"*.
**저장할 가치가 없는 상태를 타입으로 구분해 두었다.**

| | 예 | 수명 | 저장 |
|---|---|---|---|
| **스펙** | `ScenarioProgression` · `ChapterProgression` · `EpisodeNode` | 콘텐츠와 같다 | 콘텐츠로 |
| **진행** | `ProgressionState` | 플레이어의 것 | **세이브** |
| **해석** | `ChapterAdvance` · `ResolvedOption` | 이 순간뿐 | **안 한다** |

test13이 무너진 자리가 여기다 — `EpisodeSelectionStateData`가 `Stats`(진행)와
`LockedEpisodeIds`·`VisibleEpisodeIds`(해석)를 한 자루에 넣고 평가기가 그걸 직접 뒤집는다.
그래서 세이브에 옛 판정이 섞여 들어갈 길이 열린다.

**구조적 표현**: 해석 타입에는 `[Serializable]`을 붙이지 않는다.

### P4 — 계획을 먼저 확정하고, 그 뒤엔 커서만 움직인다

`StepGatePlanBuilder.BuildForCurrentNode`가 노드 진입 시 토큰 목록을 통째로 만든다.
그 뒤 `StepGateAdvancer`는 커서만 민다. **판단과 실행이 시간적으로 분리돼 있다.**

진행 층의 §G6 *"조건 판정은 커밋 전 값으로 한다"*가 같은 원칙이다.
`ChapterTransition.Resolve`가 에피소드 진입 시점에 선택지를 확정하고, 그 뒤로는 고르기만 한다.

### P5 — 조용히 버리지 않는다

진단은 **자리를 짚는다**: `Chapters[ch01].Nodes[ep03].NextOptions[1].Conditions[0]`.
진단은 **전부 모아서 한 번에** 낸다 — 첫 오류에서 멈추면 작가가 왕복을 여러 번 한다.
잠긴 이유는 **원인 조건을 지목**한다. 툴 증명기가 이미 그렇게 하므로 여기도 같아야 한다.

---

## 2. 타입

`netstandard2.1` · C# 9 · `Nullable disable` · 엔진 의존 0 · JSON 파서 없음.

### 2.1 어휘 — 세 층이 공유하는 것

```csharp
namespace Ked.Progression
{
    /// <summary>
    /// 비교 연산 6종. NotEqual은 **일부러 없다** — 저작 파서가 닫아 두어 데이터로
    /// 나오지 않는다. 넣으면 평가기에 영원히 안 타는 분기가 생긴다.
    /// </summary>
    public enum ComparisonOp
    {
        GreaterOrEqual, LessOrEqual, Equal, Exists, GreaterThan, LessThan
    }

    public enum ConditionKind
    {
        Stat = 0,
        EpisodeCleared = 1,
        ChapterCleared = 2,     // ← 신설. 시나리오 층이 생기는 지금이 그 자리
    }
```

```csharp
    /// <summary>
    /// 진행 조건 하나.
    ///
    /// <b>생성자가 private이다 (P1).</b> Kind와 Op는 자유 조합이 아니다 —
    /// Cleared 계열은 <see cref="ComparisonOp.Exists"/>만 뜻이 있고, 나머지 조합은
    /// 존재하지 않는다. 공개 생성자를 두면 그 조합이 <b>타이핑된 뒤</b> 어딘가에서
    /// 예외로 걸린다. 팩토리만 열면 애초에 타이핑되지 않는다.
    ///
    /// 이것이 test13 <c>EpisodeCondition</c>과의 결정적 차이다 — 그쪽은
    /// IntValue·BoolValue·StringValue를 한 타입에 다 두고 Kind에 따라 뜻이 달라졌다.
    /// 5종 × 6연산 = 30조합 중 대부분이 무의미한데 아무것도 막지 않았다.
    /// </summary>
    public readonly struct ProgressionCondition
    {
        public ConditionKind Kind { get; }
        public string Key { get; }
        public ComparisonOp Op { get; }

        /// <summary>
        /// ⚠ §G2 — 저작 쪽은 <b>0을 키 자체를 생략해서</b> 내보낸다.
        /// DTO의 대응 필드는 반드시 <c>int</c>여야 한다. <c>int?</c>로 두면
        /// "없음"과 "0"이 갈려 <c>flag == false</c>(= Equal 0)가 통째로 어긋난다.
        /// </summary>
        public int Value { get; }

        private ProgressionCondition(ConditionKind kind, string key, ComparisonOp op, int value)
        {
            Kind = kind; Key = key; Op = op; Value = value;
        }

        public static ProgressionCondition Stat(string key, ComparisonOp op, int value) =>
            new ProgressionCondition(ConditionKind.Stat, Require(key, nameof(key)), op, value);

        public static ProgressionCondition EpisodeCleared(string episodeId) =>
            new ProgressionCondition(
                ConditionKind.EpisodeCleared, Require(episodeId, nameof(episodeId)),
                ComparisonOp.Exists, 0);

        public static ProgressionCondition ChapterCleared(string chapterId) =>
            new ProgressionCondition(
                ConditionKind.ChapterCleared, Require(chapterId, nameof(chapterId)),
                ComparisonOp.Exists, 0);

        /// <summary>
        /// ⚠ <c>default(ProgressionCondition)</c>은 C#이 언제나 만들 수 있다 — struct의
        /// 한계다. 그 값은 <c>Key == null</c>이므로 <b>여기서 판별된다.</b>
        /// 소유 타입의 생성자가 이걸 거부하고, 평가기는 다시 확인하지 않는다(P2).
        /// </summary>
        public bool IsConstructed => Key != null;

        public override string ToString() =>
            Kind == ConditionKind.Stat ? $"{Key} {Op} {Value}" : $"{Kind}({Key})";
    }
```

```csharp
    public enum StatType { Number, Bool }

    /// <summary>
    /// 스탯의 정의 — 초기값·경계의 유일한 집 (§G7의 빈칸).
    ///
    /// <b>소유는 시나리오다 (D1).</b> 같은 <c>trust</c>가 챕터마다 다른 경계를 가지면
    /// 챕터를 넘나든다는 말이 성립하지 않는다. 챕터 워크북의 `초기값` 열은 실행값이
    /// 아니라 <b>도달성 증명의 진입 가정</b>으로 역할이 바뀐다.
    /// </summary>
    public sealed class StatDefinition
    {
        public string Key { get; }
        public string DisplayName { get; }
        public StatType Type { get; }
        public int Initial { get; }
        public int Minimum { get; }
        public int Maximum { get; }

        public int Clamp(int value);
    }

    /// <summary>간선을 탈 때 원자적으로 1회 적용되는 증감.</summary>
    public readonly struct StatChange
    {
        public string Key { get; }
        public int Amount { get; }
    }
```

### 2.2 에피소드 층

```csharp
    /// <summary>
    /// 이 길이 무엇인가.
    ///
    /// <b>지금까지 이걸 빈 문자열로 표현하고 있었다</b>(<c>IsDefault =&gt;
    /// ChoiceLabel.Length == 0</c>). sentinel이므로 사고가 하나 열려 있었다 —
    /// 작가가 엑셀에서 선택지 문구를 실수로 지우면 그 간선이 <b>플레이어 선택지에서
    /// 보이지 않는 자동 진행으로 조용히 변신한다.</b> 분기가 사라지고 검증은 통과하고
    /// 게임을 돌려 봐야 안다.
    ///
    /// <c>GateTokenType.Immediately</c>가 "없음"을 명시적 토큰으로 만든 것과 같은 판단이다.
    /// </summary>
    public enum OptionKind
    {
        /// <summary>플레이어가 고른다. 문구가 반드시 있다.</summary>
        PlayerChoice = 0,

        /// <summary>
        /// 고를 수 있는 것이 하나도 없을 때 자동으로 타는 길 (§G6-2).
        /// 에피소드당 하나. <b>문구도 관문도 없다.</b>
        /// </summary>
        AutoAdvance = 1,
    }
```

```csharp
    /// <summary>
    /// 에피소드에서 나가는 길 하나 — 저작 `간선` 시트의 한 행.
    ///
    /// <b>관문이 사는 자리가 여기다 (§G5, v8).</b> 전에는 노드가 표시·해금 조건을
    /// 들고 있었고 test13은 아직 그렇다. 그래서 같은 JSON을 그쪽에 먹이면
    /// <b>에러 없이 관문이 전부 열린다.</b>
    ///
    /// <b>생성자가 private이다 (P1).</b> <see cref="Auto"/>는 문구·조건·잠금 인자를
    /// 아예 받지 않는다 — "자동 진행에 관문이 달림"이 예외가 아니라 <b>컴파일 오류</b>가 된다.
    /// </summary>
    public sealed class EpisodeOption
    {
        public OptionKind Kind { get; }

        /// <summary><see cref="OptionKind.AutoAdvance"/>면 빈 문자열이다.</summary>
        public string ChoiceLabel { get; }

        public string TargetEpisodeId { get; }

        /// <summary>미달이면 목록에 <b>만들지 않는다</b> — 있었다는 사실 자체를 모른다.</summary>
        public IReadOnlyList<ProgressionCondition> VisibleConditions { get; }

        /// <summary>미달이면 <b>잠긴 채 보인다</b>. <see cref="HideWhenLocked"/>면 숨긴다.</summary>
        public IReadOnlyList<ProgressionCondition> Conditions { get; }

        public bool HideWhenLocked { get; }
        public string LockedReasonText { get; }

        /// <summary>이 길을 타는 순간 원자적으로 1회 커밋된다. 스탯이 변하는 유일한 자리.</summary>
        public IReadOnlyList<StatChange> StatChanges { get; }

        public static EpisodeOption Choice(
            string choiceLabel,                     // 비면 예외 — AutoAdvance와 구별된다
            string targetEpisodeId,
            IReadOnlyList<ProgressionCondition> visibleConditions = null,
            IReadOnlyList<ProgressionCondition> conditions = null,
            bool hideWhenLocked = false,
            string lockedReasonText = null,
            IReadOnlyList<StatChange> statChanges = null);

        public static EpisodeOption Auto(
            string targetEpisodeId,
            IReadOnlyList<StatChange> statChanges = null);
    }
```

```csharp
    public enum EpisodeKind { Main, Attachment }

    public sealed class EpisodeNode
    {
        public string EpisodeId { get; }
        public string Title { get; }
        public EpisodeKind Kind { get; }

        /// <summary>호스트가 재생할 대본의 키. <b>이 패키지는 내용을 모른다</b> — 경계면이 여기다.</summary>
        public string DialogueEntryId { get; }

        /// <summary>배열 순서가 곧 화면에 뜨는 순서다 (§G6).</summary>
        public IReadOnlyList<EpisodeOption> NextOptions { get; }

        /// <summary>
        /// 이 노드로 챕터가 끝나면 어느 엔딩인가. <b>비어 있으면 엔딩 후보가 아니다.</b>
        ///
        /// ⚠ <c>bool IsChapterEndingCandidate</c>를 <b>없앴다</b>. 저작 스키마와
        /// test13에는 bool + 키 두 필드가 있는데, 4조합 중 둘(참인데 키가 빔 /
        /// 거짓인데 키가 있음)이 무효다. 키 하나로 합치면 그 둘이 존재할 수 없다 (P1).
        /// DTO에는 스키마 1:1로 남기고 <b>로더가 불일치를 진단한다.</b>
        /// </summary>
        public string EndingKey { get; }

        public string DesignerNote { get; }

        public bool IsEndingCandidate => EndingKey.Length != 0;

        /// <summary>에피소드당 하나뿐인 자동 진행 간선.</summary>
        public bool TryGetAutoOption(out EpisodeOption option);
    }
```

> **모델에서 뺀 넷** — `Node.VisibleConditions`·`UnlockConditions`(v8에서 간선으로 내려감) ·
> `IndexText`(v5 폐지) · `Position`(저작 레이아웃). 언제나 빈 값으로 나오는 칸을 모델에
> 옮기면 "여기에 조건을 달 수 있다"는 잘못된 여지가 생긴다. DTO에는 1:1로 남는다.

### 2.3 챕터 층

```csharp
    /// <summary>
    /// 챕터에서 나가는 길 — <b>시나리오 층의 간선이다.</b>
    /// 원본은 런타임 <c>ChapterEndingRule</c>(test13). 모양만 가져오고 코드는 안 가져온다.
    ///
    /// ⚠ <c>bool UnlockNextChapter</c>를 <b>없앴다</b> — <see cref="NextChapterId"/>가
    /// 비면 여기서 시나리오가 끝난다. <see cref="EpisodeNode.EndingKey"/>와 같은 판단이다.
    /// </summary>
    public sealed class EndingRule
    {
        public string EndingKey { get; }
        public string DisplayName { get; }

        /// <summary>
        /// 같은 엔딩키인데 스탯에 따라 다음 챕터가 갈릴 때만 쓴다 (AND).
        /// <b>비어 있으면 무조건 성립</b> — 대부분의 규칙이 그렇다.
        ///
        /// ⚠ D2 — <b>어느 엔딩인지는 노드의 <c>EndingKey</c>가 정한다.</b> 이 조건은
        /// 판정하지 않고 <i>갈래를 고른다</i>. 두 곳에서 엔딩을 정하면 어긋난다.
        /// </summary>
        public IReadOnlyList<ProgressionCondition> Conditions { get; }

        /// <summary>비면 시나리오 종료.</summary>
        public string NextChapterId { get; }

        public string DesignerNote { get; }
    }
```

```csharp
    public sealed class ChapterProgression
    {
        public string ChapterId { get; }
        public string DisplayName { get; }
        public string StartEpisodeId { get; }

        public IReadOnlyList<EpisodeNode> Nodes { get; }

        /// <summary>이 챕터에서 나가는 길들. 같은 <c>EndingKey</c>가 여럿이면 순서대로 본다.</summary>
        public IReadOnlyList<EndingRule> EndingRules { get; }

        /// <summary>
        /// ⚠ <b>소유하지 않는다.</b> 시나리오가 준 것을 참조로 든다 (D1).
        /// 챕터를 단독으로 세울 때(테스트·증명)는 명시적으로 넘긴다.
        /// </summary>
        public IReadOnlyDictionary<string, StatDefinition> Stats { get; }

        public ChapterProgression(
            string chapterId, string displayName, string startEpisodeId,
            IReadOnlyList<EpisodeNode> nodes,
            IReadOnlyList<EndingRule> endingRules,
            IReadOnlyDictionary<string, StatDefinition> stats);

        public bool TryGetNode(string episodeId, out EpisodeNode node);
        public EpisodeNode StartNode { get; }
    }
```

**생성자가 강제하는 챕터 불변식** — 통과했다는 것은 아래가 전부 참이라는 뜻이다.
전이기와 증명기는 이것들을 **다시 걱정하지 않는다**.

1. 에피소드 ID에 중복이 없다 · 시작 에피소드가 실재한다
2. 모든 간선이 **실재하는 에피소드에 착지한다**
3. 모든 조건·스탯변화가 **정의된 스탯만** 가리킨다
4. `Bool` 스탯에 크기 비교·증감이 없다 (§G4)
5. 모든 조건이 `IsConstructed`다 (`default` 값이 배열에 안 샜다)
6. 자동 진행 간선이 에피소드당 **최대 하나**다
7. **노드의 `EndingKey`가 전부 `EndingRules`에 있다** ← 엔딩인데 갈 곳이 없는 상태를 막는다
8. `EndingRules`의 `EndingKey`에 중복이 없다

### 2.4 시나리오 층

```csharp
    public sealed class ScenarioProgression
    {
        public string ScenarioId { get; }
        public string DisplayName { get; }
        public string StartChapterId { get; }

        /// <summary>스탯 정의의 <b>유일한 원천</b> (D1).</summary>
        public IReadOnlyList<StatDefinition> Stats { get; }
        public IReadOnlyDictionary<string, StatDefinition> StatsByKey { get; }

        public IReadOnlyList<ChapterProgression> Chapters { get; }

        public bool TryGetChapter(string chapterId, out ChapterProgression chapter);
        public ChapterProgression StartChapter { get; }

        /// <summary><see cref="StatDefinition.Initial"/>로 세운 새 게임 상태.</summary>
        public ProgressionState CreateInitialState();
    }
```

**생성자가 강제하는 시나리오 불변식**

1. 챕터 ID에 중복이 없다 · 시작 챕터가 실재한다
2. 모든 `EndingRule.NextChapterId`가 **실재하는 챕터에 착지한다** (비어 있지 않다면)
3. 스탯 키에 중복이 없다 · 모든 챕터가 **같은 스탯 인스턴스**를 든다
4. `ChapterCleared` 조건의 대상 챕터가 실재한다

> 4번은 `EpisodeCleared`와 다른 판단이다. 에피소드는 개수가 많고 저작 중 자주 바뀌어
> "없는 대상"이 fail-closed(영원히 안 열림)로 남는 편이 낫지만(증명기가 잡는다),
> 챕터는 개수가 적고 오타가 곧 **시나리오가 끊기는 것**이라 여기서 잡는다.

### 2.5 진행 — 저장되는 것

```csharp
    public readonly struct ChapterEnding
    {
        public string ChapterId { get; }
        public string EndingKey { get; }
    }

    public sealed class ProgressionState
    {
        public string CurrentChapterId { get; }
        public string CurrentEpisodeId { get; }

        public IReadOnlyDictionary<string, int> Stats { get; }
        public IReadOnlyCollection<string> ClearedEpisodeIds { get; }
        public IReadOnlyCollection<string> ClearedChapterIds { get; }
        public IReadOnlyList<ChapterEnding> EndingHistory { get; }

        public int GetStat(string key);          // 정의되지 않은 키는 예외 (규율 1)
        public bool IsEpisodeCleared(string episodeId);
        public bool IsChapterCleared(string chapterId);

        /// <summary>
        /// <b>스탯 커밋과 이동이 한 연산이다 (P1).</b>
        ///
        /// 전에는 <c>WithStatChanges(...)</c>와 <c>WithMovedTo(...)</c>가 각각 public이라
        /// 따로 부를 수 있었다. 따로 부를 수 있으면 언젠가 따로 불리고, 그 순간
        /// <b>스탯만 바뀌고 안 옮겨 간 상태</b>가 생긴다 — 그게 정확히 §3.3이 막으려는
        /// 중복 가산이다. 합치면 그 상태가 존재할 수 없다.
        ///
        /// 이것이 "에피소드 = 트랜잭션 경계"의 <b>타입 수준 표현</b>이다.
        /// </summary>
        public ProgressionState Commit(ChapterProgression chapter, EpisodeOption chosen);

        /// <summary>챕터 경계. 엔딩을 기록하고 다음 챕터의 시작 에피소드로 옮긴다.</summary>
        public ProgressionState CommitChapterEnding(
            ScenarioProgression scenario, string endingKey, string nextChapterId);
    }
```

### 2.6 해석 — 저장되지 않는 것

⚠ **이 절의 타입에는 `[Serializable]`을 붙이지 않는다.** 그것이 P3의 구조적 표현이다.

```csharp
    /// <summary>
    /// ⚠ <b>Hidden이 없다 (2026-08-18 구현에서 뺐다).</b> §G5의 "표시조건 미달이면 목록에
    /// **만들지 않는다**"를 그대로 옮기면 숨긴 것은 목록에 <b>없는 것</b>이지 Hidden으로
    /// 표시된 항목이 아니다. 목록에 넣어 두면 호스트가 전부 그리다 숨겨야 할 것을 보여 주는
    /// 사고가 열리고, 그 값은 어차피 아무도 그리면 안 되므로 영원히 안 타는 분기가 된다
    /// (NotEqual을 뺀 것과 같은 판단). 몇 개가 숨겨졌는지는 ChapterAdvance.HiddenCount가 진다.
    /// </summary>
    public enum OptionVisibility { Shown, Locked }

    public readonly struct ResolvedOption
    {
        public EpisodeOption Option { get; }
        public OptionVisibility Visibility { get; }
        public bool IsSelectable { get; }

        /// <summary>
        /// 저작자가 쓴 안내문. <b>비어 있을 수 있다</b> — 이 패키지는 대신 문장을
        /// 지어내지 않는다(무해석성).
        /// </summary>
        public string LockedReason { get; }

        /// <summary>
        /// <b>왜 잠겼는지 지목한다</b> — 미달인 첫 조건 (P5).
        /// 툴의 증명기가 이미 원인 조건을 지목하므로 여기도 같아야 규약 사본이 안 생긴다.
        /// </summary>
        public ProgressionCondition BlockingCondition { get; }
    }

    public enum ChapterAdvanceKind
    {
        AwaitPlayerChoice,   // 고를 수 있는 것이 있다
        AutoAdvance,         // 문구 없는 간선으로 자동 진행
        ChapterEnded,        // 그것도 없다 — 챕터 런이 여기서 끝난다
    }

    public readonly struct ChapterAdvance
    {
        public ChapterAdvanceKind Kind { get; }

        /// <summary>
        /// 화면에 그릴 목록. 표시 순서 = 배열 순서.
        /// <b>AwaitPlayerChoice가 아니면 비어 있다</b> — 누를 수 없는 목록을 띄워 놓고
        /// 아무 일도 안 일어나는 것이 더 나쁘다.
        /// </summary>
        public IReadOnlyList<ResolvedOption> Options { get; }

        public EpisodeOption AutoOption { get; }                // AutoAdvance일 때
        public string EndingKey { get; }                        // ChapterEnded일 때

        /// <summary>목록에서 빠진 개수. 게임이 아니라 저작 도구와 로그가 본다.</summary>
        public int HiddenCount { get; }
    }

    public static class ChapterTransition
    {
        /// <summary>
        /// ⚠ 조건 판정은 <b>커밋 전 값</b>으로 한다 (§G6, P4) —
        /// 플레이어가 선택지를 보는 시점의 값이다.
        /// </summary>
        public static ChapterAdvance Resolve(
            ChapterProgression chapter, ProgressionState state);
    }
```

```csharp
    public enum ScenarioAdvanceKind { NextChapter, ScenarioEnded }

    public readonly struct ScenarioAdvance
    {
        public ScenarioAdvanceKind Kind { get; }
        public string NextChapterId { get; }
        public EndingRule MatchedRule { get; }
    }

    public static class ScenarioTransition
    {
        /// <summary>
        /// 챕터가 <see cref="ChapterAdvanceKind.ChapterEnded"/>로 끝났을 때 다음 챕터를 정한다.
        /// 맞는 규칙이 없으면 <see cref="ScenarioAdvanceKind.ScenarioEnded"/> —
        /// <b>조용히 첫 챕터로 돌아가지 않는다.</b>
        /// </summary>
        public static ScenarioAdvance Resolve(
            ScenarioProgression scenario, ProgressionState state, string endingKey);
    }
```

```csharp
    public static class ConditionEvaluator
    {
        /// <summary>
        /// ⚠ <b>모르는 값에 대한 분기가 없다 (P2).</b> 여기 도달한 조건은 이미
        /// 생성자·로더를 통과했으므로 <c>Kind</c>도 <c>Op</c>도 <c>Key</c>도 유효하다.
        /// <c>default:</c>에서 <c>false</c>를 돌려주면 오타가 "영원히 안 열리는 문"이
        /// 되고 아무 소리도 안 난다 — test13이 정확히 그렇게 했다.
        /// </summary>
        public static bool IsMet(in ProgressionCondition condition, ProgressionState state);
    }
```

### 2.7 유저 데이터

```csharp
    /// <summary>
    /// 세이브의 <b>진행 블록</b>. 대사 위치(<c>lineId</c>·<c>nodeName</c>·선택 재현 기록)는
    /// 이 패키지가 모른다 — 호스트의 <c>VNSaveData</c>가 계속 소유한다.
    ///
    /// ⚠ <c>[Serializable]</c>도 JSON 어트리뷰트도 붙이지 않는다 (규율 2). 굽는 것은 호스트다.
    /// </summary>
    public sealed class ProgressionSave
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; }
        public string ScenarioId { get; }
        public string CurrentChapterId { get; }
        public string CurrentEpisodeId { get; }

        public IReadOnlyDictionary<string, int> Stats { get; }
        public IReadOnlyList<string> ClearedEpisodeIds { get; }
        public IReadOnlyList<string> ClearedChapterIds { get; }
        public IReadOnlyList<ChapterEnding> EndingHistory { get; }

        public static ProgressionSave From(ProgressionState state, string scenarioId);
    }

    public static class ProgressionSave
    {
        public const int CurrentSchemaVersion = 1;

        /// <summary>시나리오를 **객체로** 받는다 — ID를 문자열로 받으면 엉뚱한 이름이
        /// 붙은 세이브가 조용히 만들어진다.</summary>
        public static ProgressionSaveDto Capture(
            ScenarioProgression scenario, ProgressionState state);

        /// <summary>
        /// 세이브는 <b>어제 만든 콘텐츠</b>로 저장되고 <b>오늘 고친 콘텐츠</b>로 로드된다.
        ///
        /// ⚠ <b>경고면 상태를 만들고 오류면 안 만든다</b>(2026-08-18 구현에서 정정).
        /// 초안에는 "진단이 있어도 상태는 만든다"고 적었는데, 지금 에피소드가 사라진
        /// 세이브로는 <b>이어할 수가 없다</b> — 반쯤 되살린 진행으로 시작하면 무엇이
        /// 어긋났는지 플레이해 봐야 안다. 값이 조정된 정도(clamp·버려진 스탯)는 경고로
        /// 두고 상태를 낸다.
        /// </summary>
        public static ProgressionRestoreResult Restore(
            ScenarioProgression scenario, ProgressionSaveDto save);
    }
```

**콘텐츠가 바뀌었을 때** — 이 표가 곧 테스트 목록이다.

| 상황 | 처리 |
|---|---|
| 세이브에 없는 스탯이 정의에 생김 | `Initial`로 채운다 — **조용해도 되는 유일한 경우** |
| 정의에 없는 스탯이 세이브에 있음 | 버린다 + 진단 |
| 스탯 값이 새 경계 밖 | clamp + 진단 (조용한 clamp는 없다) |
| `CurrentEpisodeId`가 사라짐 | 진단. **조용히 시작 에피소드로 보내지 않는다** |
| `CurrentChapterId`가 사라짐 | 진단 |
| `SchemaVersion`이 더 높음 | **거부.** 미래 세이브를 추측하지 않는다 |

### 2.8 진단

```csharp
    public enum ProgressionDiagnosticSeverity { Error, Warning }

    public sealed class ProgressionDiagnostic
    {
        public ProgressionDiagnosticSeverity Severity { get; }

        /// <summary>"Chapters[ch01].Nodes[ep03].NextOptions[1].Conditions[0]" (P5).</summary>
        public string Path { get; }

        public string Message { get; }
    }
```

---

## 3. 불변식이 어디 사는가

**같은 규칙을 세 곳에 쓰지 않는다.** 위로 올릴 수 있으면 올린다 (P1).

| 층 | 무엇을 막나 | 예 |
|---|---|---|
| **타입 (팩토리)** | 타이핑 자체가 안 된다 | `EpisodeCleared + GreaterOrEqual` · 관문 달린 자동 진행 · 스탯만 커밋하고 안 옮기기 |
| **생성자** | 프로그래머 실수의 마지막 방어선. 예외를 던진다 | 허공 간선 · 미정의 스탯 키 · 갈 곳 없는 엔딩키 · bool 어휘 |
| **로더** | 데이터의 잘못. **진단으로 모아서** 낸다 | 알 수 없는 enum 이름 · bool과 키의 불일치 · 위 전부를 앞당겨 |

로더는 **규칙을 새로 쓰지 않는다.** 생성자가 예외로 던지는 판정을 앞당겨 진단으로 모을 뿐이다.
그래야 한 규칙의 정의가 한 곳에만 남는다 (규약 사본 금지).

---

## 4. 세 층이 같은 모양인데 제네릭으로 묶지 않는 이유

시나리오·챕터가 둘 다 "노드 + 출발 노드가 소유하는 간선 + 간선의 조건"이다.
`ProgressionGraph<TNode, TEdge>`로 묶고 싶어지는 자리고, **묶지 않는다.**

- **노드마다 불변식이 다르다.** 에피소드는 `DialogueEntryId`가 필요하고, 챕터는 자기 안에
  그래프를 품는다. 제네릭 제약으로 표현하면 제약이 타입보다 복잡해진다
- **간선의 의미가 다르다.** 에피소드 간선은 스탯을 커밋하고, 챕터 간선은 엔딩키로 매칭된다
- **읽는 사람이 두 번 추론한다.** `ProgressionGraph<ChapterProgression, EndingRule>`을
  보고 "챕터가 노드구나"를 매번 다시 세워야 한다

대신 **반복되는 것은 불변식이지 타입이 아니다** — ID 중복, 시작 노드 실재, 간선 착지.
그건 `GraphInvariants` 정적 헬퍼로 뽑는다.

> **타입은 나누고 불변식은 공유한다.** 반대로 하면 타입 하나에 두 도메인이 들어온다.

---

## 5. 지금 코드에서 바뀌는 것

| | 무엇 | 왜 | 깨지나 |
|---|---|---|---|
| 1 | `ProgressionCondition` 생성자 → 팩토리 셋 | P1 | 테스트 다수 (기계적) |
| 2 | `ConditionKind.ChapterCleared` 추가 | 시나리오 층 | 아니오 |
| 3 | `EpisodeOption` 생성자 → `Choice` / `Auto` | P1 — **문구 지움 사고** | 테스트 다수 (기계적) |
| 4 | `EpisodeNode.IsChapterEndingCandidate` 제거 | sentinel 쌍 소멸 | 예 |
| 5 | `ProgressionState`에 챕터·엔딩 이력 추가 | 시나리오 층 | 아니오 |
| 6 | `WithStatChanges` + `WithMovedTo` → `Commit` 하나 | P1 — 트랜잭션 경계 | 예 |
| 7 | `ChapterProgression.Stats`가 소유 → 참조 | D1 | 예 |
| 8 | `EndingRule` · `ScenarioProgression` 신설 | — | 아니오 |

`0.x`는 공개 표면을 약속하지 않는 구간이다. **지금이 가장 싸다.**

---

## 6. 결정

| | 결정 | 답 |
|---|---|---|
| **D1** ✅ | 스탯 정의의 소유 — 챕터냐 시나리오냐 | **시나리오.** 아래 참조 |
| **D2** ✅ | 엔딩을 무엇이 판정하나 | **노드의 `EndingKey`.** 아래 참조 |
| **D3** ✅ | `Tokens` · `Flags` | **둘 다 안 넣는다.** `Flags`는 v9가 `Stat` 0/1로 통일했고, `Tokens`(아이템·열쇠)는 저작 쪽 확인 결과 **작가 계층에서 Yarn 변수로 살고 진행 JSON에 나오지 않는다** — 두 계층이 다르니 섞지 않는다 |
| **D4** ✅ | 세이브 스키마 버전 정책 | **처음부터 넣었다.** 필드가 사라지거나 뜻이 바뀔 때만 올린다 — 추가는 안 올린다(없는 값은 정의의 초기값이 메운다). 더 높은 버전은 **로드 거부** |
| **D5** | 저작 `간선` 시트에 `종류` 열을 둘 것인가 | **권고: 둔다.** 안 두면 "문구를 실수로 지웠다"와 "의도한 자동 진행"을 데이터로 구별할 방법이 없다. 그때까지 로더가 자동 진행 간선을 **경고로 보고**한다 |

### D1 확정 — 스탯 정의는 시나리오가 소유한다 (2026-08-18)

챕터 워크북이 값을 적지만, 챕터가 이어지는 순간 그 초기값이 두 번 의미를 갖는다. ch01을
끝내고 ch02에 들어갈 때 `trust`가 ch02의 초기값으로 되돌아가면 *"스탯이 챕터를 넘나든다"*가
거짓이 된다.

| | 주인 | 뜻 |
|---|---|---|
| 존재 · 경계 · 타입 · 표시명 | **시나리오** | 시나리오 전체에서 하나 |
| 실제 플레이의 시작값 | **시나리오** | `ScenarioProgression.CreateInitialState()`가 한 번만 세운다 |
| 챕터에 적힌 `초기값` | 챕터 | **도달성 증명의 진입 가정** — "이 값으로 시작하면 어떻게 되나" |

**경계와 타입은 갈리면 오류, 초기값은 갈려도 된다.** 경계가 챕터마다 다르면 증명이 걷는
상태공간과 실제 플레이가 갈린다(§G7이 경고한 상황). 초기값은 단독 검증용 가정이므로 달라도
된다. `ScenarioInvariants.VerifyChapterStats`가 그 선을 지킨다.

**챕터가 스탯을 안 적으면 시나리오 것을 쓴다.** 조용한 기본값이 아니라 소유 규칙의 적용이다.

### D2 확정 — 엔딩은 노드가 정한다 (2026-08-18)

`EpisodeNode.EndingKey`가 어느 엔딩인지 정하고, `EndingRule`은 그 키로 **조회되는 표**다.
규칙의 `Conditions`는 엔딩을 판정하지 않고 **같은 키에서 다음 챕터가 갈릴 때** 갈래를 고른다.

**시그니처가 그것을 강제한다** — `ScenarioTransition.Resolve(scenario, state)`는 엔딩키를
인자로 받지 않고 지금 노드에서 읽는다. 호출자가 엉뚱한 키를 넘길 자리가 없다.

따라오는 불변식 셋:

1. 엔딩키를 내는 노드에는 그 키의 규칙이 있어야 한다 (엔딩인데 갈 곳이 없으면 안 된다)
2. 아무도 안 내는 키의 규칙은 오류다 (영원히 안 타는 분기)
3. 같은 키의 **마지막 규칙은 조건이 없어야 한다** — 전부 미달일 때 무슨 일이 일어나야 하는지
   아무도 안 적으면 런타임이 추측하게 되고, 추측하는 규칙은 언젠가 틀린다

⚠ 규칙이 **하나도 없는** 챕터는 정상이다 — 단일 챕터 시나리오에서 엔딩이 곧 종착이다.
1번은 규칙이 하나라도 있을 때만 본다.

### 남은 sentinel 하나와, 남기지 않은 하나

`EndingRuleDto.Outcome`은 `"NextChapter"` / `"ScenarioEnd"`를 **명시로** 받는다.
`NextChapterId`가 비었는지로 판별하면 *"여기서 끝난다"*와 *"다음을 실수로 안 적었다"*가 같은
모양이 되기 때문이다 — `ChoiceLabel`이 비면 자동 진행이던 것과 같은 사고다.

그쪽(D5)은 저작 데이터가 이미 sentinel로 굳어 있어 로더가 번역할 수밖에 없지만, 이 모양은
아직 아무도 안 쓰므로 처음부터 명시할 수 있었다. **툴이 나중에 이 모양에 맞춘다(X2).**
