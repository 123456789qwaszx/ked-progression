# Ked.Progression — 모델·계약 초안 (v0, 검토용)

> **⛔ 이 문서는 v0 기록이다 (2026-08-18).** 타입의 정본은 [`architecture.md`](architecture.md)로
> 옮겨 갔다. 아래 셋은 그쪽에서 **뒤집혔다** — `EndingRules`는 모양이 있고(런타임
> `ChapterEndingRule`), 스탯 정의의 소유는 시나리오이며, 조건·선택지는 공개 생성자 대신
> 팩토리만 연다. **읽기 전에 architecture.md를 먼저 볼 것.**

> **⚠ §3.1~3.4는 이제 코드가 있다 (2026-08-17).** 스케치와 실제가 갈리면 **코드가 이긴다** —
> `Runtime/ChapterProgression.cs` · `EpisodeNode.cs` · `EpisodeOption.cs`.
> 이 문서는 **왜 그 모양인지**를 남기는 자리로 남는다. 갈린 곳 둘:
> `EndingRules`는 모양이 없어 타입을 만들지 않았고(handoff §3-1),
> `ChapterProgression`은 스케치에 없던 챕터 전체 불변식을 생성자에서 강제한다.
> §3.6(로더)과 §5는 아직 초안 그대로다.

챕터·에피소드 진행 층. `runtime-contract.md` §G를 **문서에서 타입으로** 옮긴 것.

> **왜 이 패키지가 있는가**
> 지금 이 규칙은 VnTool과 런타임에 **각자 구현**돼 있거나(도달성 증명 vs 실제 전이),
> 한쪽에만 있다. 구현이 둘이면 갈린다 — 계약서 G7이 그 위험을 이미 적어 두었다.
> **구현을 하나로 만들면 갈릴 수가 없다.**

---

## 0. 하는 일 / 안 하는 일

| 한다 | 안 한다 |
|---|---|
| 챕터 진행 규칙의 모델 (챕터·에피소드·선택지·조건·스탯) | Yarn 재생 — `DialogueEntryId`는 문자열일 뿐이다 |
| 조건 평가 · 선택지 가시성 판정 · 전이 판정 | 그래프 편집·레이아웃 — 저작 도구의 일이다 |
| 스탯 정의(초기값·경계)와 clamp | 게임별 스탯의 **의미** — `game.definition.json`이 공급한다 |
| 도달성 증명 (툴에서 이관) | 세이브 직렬화 — 나중에, 별도로 |
| 로드 시 검증과 진단 | JSON 파싱 — 호스트가 한다 (규율 2) |

**의존 0.** `netstandard2.1`, UnityEngine 없음, JSON 파서 없음.

---

## 1. 물려받는 규율

`Ked.Presentation.Core`의 규율을 그대로 쓴다. 새로 만들지 않는다.

| | |
|---|---|
| **규율 1 — 침묵 금지** | 모르는 op, 없는 에피소드 ID, 미정의 스탯 키를 **조용히 기본값으로 떨어뜨리지 않는다.** 로더가 전부 모아서 진단으로 낸다 |
| **규율 2 — 의존 0** | 역직렬화는 호스트가 한다. 이 패키지는 **필드가 1:1인 DTO**만 갖는다 |
| **규율 3 — 게임 데이터는 인자** | 스탯 카탈로그·챕터 데이터는 전부 입력이다. 코드에 게임 어휘가 없다 |
| **규율 4 — 순수 함수** | 평가·전이는 상태를 바꾸지 않고 새 상태를 돌려준다. 시간·랜덤·IO 없음 |

---

## 2. 층 구조

```
progression.json ──[호스트가 역직렬화]──> Dto  ──[ProgressionLoader: 검증·정규화]──> Model
                                          │                    │
                              문자열 그대로            여기가 침묵 금지의 자리
                              (enum도 string)          모르는 이름은 진단이 된다
                                                                │
                                                                ▼
                                              ConditionEvaluator · ChapterTransition
                                                      (순수 함수)
```

**DTO와 Model을 나누는 이유는 하나뿐이다** — enum이 이름 문자열로 오기 때문(G1).
그 변환 지점이 "모르는 이름을 만났을 때 소리를 내는" 유일한 자리다.

---

## 3. 타입 스케치

### 3.1 조건

```csharp
namespace Ked.Progression
{
    public enum ConditionKind { Stat, EpisodeCleared }

    /// <summary>
    /// 비교 연산 6종. 2026-08-16 소유자 개방으로 GreaterThan·LessThan이 더해졌다(G3).
    ///
    /// NotEqual은 **일부러 없다** — 저작 쪽 파서가 아직 닫아 두어 데이터로 나오지 않는다.
    /// 넣으면 평가기에 영원히 안 타는 분기가 생긴다. 파서가 열리면 그때 더한다.
    /// </summary>
    public enum ComparisonOp
    {
        GreaterOrEqual, LessOrEqual, Equal, Exists, GreaterThan, LessThan
    }

    public readonly struct ProgressionCondition
    {
        public ConditionKind Kind { get; }
        public string Key { get; }
        public ComparisonOp Op { get; }

        /// <summary>
        /// 비교 대상 값. DTO의 IntValue다.
        ///
        /// ⚠ G2 — 저작 쪽은 0을 **키 자체를 생략**해서 내보낸다
        /// (JsonIgnoreCondition.WhenWritingDefault). 따라서 DTO 필드는 반드시
        /// nullable이 아닌 int여야 한다. int?로 두면 "없음"과 "0"이 갈려
        /// bool 조건(flag == false)이 통째로 어긋난다 — 가장 흔한 조건인데도.
        /// </summary>
        public int Value { get; }
    }
}
```

### 3.2 스탯 — **G7의 빈칸을 이 패키지가 채운다**

```csharp
    public enum StatType { Number, Bool }

    /// <summary>
    /// 스탯의 정의. 초기값·경계가 **지금 어느 런타임 입력에도 없다**(G7) —
    /// progression.json에도 game.definition.json에도 없고, 툴의 도달성 증명
    /// 안에만 있다. 그래서 증명과 실제 플레이가 갈릴 수 있다.
    ///
    /// 이 타입이 그 경계의 유일한 집이다. 증명기와 런타임이 같은 값을 본다.
    /// </summary>
    public sealed class StatDefinition
    {
        public string Key { get; }
        public string DisplayName { get; }
        public StatType Type { get; }

        public int Initial { get; }
        public int Minimum { get; }
        public int Maximum { get; }
    }
```

> **Bool 스탯 규약 (G4):** 값 공간은 0·1이고 조건은 `Equal`뿐이다.
> 로더가 `Type == Bool`이면 `Minimum == 0 && Maximum == 1`을 강제한다.
> 런타임에 bool이라는 별도 종류를 만들지 않는다 — 그게 저작 쪽 출력 모양이다.

### 3.3 챕터·에피소드·선택지

```csharp
    public enum EpisodeKind { Main, Attachment }

    public sealed class ChapterProgression
    {
        public string ChapterId { get; }
        public string DisplayName { get; }
        public string StartEpisodeId { get; }

        public IReadOnlyList<StatDefinition> Stats { get; }     // ← G7 신설
        public IReadOnlyList<EpisodeNode> Nodes { get; }
        public IReadOnlyList<EndingRule> EndingRules { get; }

        public bool TryGetNode(string episodeId, out EpisodeNode node);
    }

    public sealed class EpisodeNode
    {
        public string EpisodeId { get; }
        public string Title { get; }
        public EpisodeKind Kind { get; }

        /// <summary>호스트가 재생할 대본의 키. 이 패키지는 내용을 모른다.</summary>
        public string DialogueEntryId { get; }

        /// <summary>NextOptions의 **배열 순서가 곧 화면에 뜨는 순서**다 (G6).</summary>
        public IReadOnlyList<EpisodeOption> NextOptions { get; }

        public bool IsChapterEndingCandidate { get; }
        public string EndingKey { get; }
        public string DesignerNote { get; }
    }

    public sealed class EpisodeOption
    {
        /// <summary>빈 문자열이면 **보이지 않는 기본 선택지**다 (G6-2). 에피소드당 하나.</summary>
        public string ChoiceLabel { get; }

        public string TargetEpisodeId { get; }

        /// <summary>미달이면 목록에 **만들지 않는다** (G5).</summary>
        public IReadOnlyList<ProgressionCondition> VisibleConditions { get; }

        /// <summary>미달이면 **잠긴 채 보인다**. HideWhenLocked면 숨긴다 (G5).</summary>
        public IReadOnlyList<ProgressionCondition> Conditions { get; }

        public bool HideWhenLocked { get; }
        public string LockedReasonText { get; }

        public IReadOnlyList<StatChange> StatChanges { get; }
    }

    public readonly struct StatChange
    {
        public string Key { get; }
        public int Amount { get; }
    }
```

### 3.4 상태 — 불변

```csharp
    /// <summary>
    /// 진행 상태. StageState와 같은 규율이다 — 바꾸지 않고 새 것을 돌려준다.
    /// 이것이 나중에 **세이브가 담을 내용**이다.
    /// </summary>
    public sealed class ProgressionState
    {
        public string CurrentEpisodeId { get; }
        public IReadOnlyDictionary<string, int> Stats { get; }
        public IReadOnlyCollection<string> ClearedEpisodes { get; }

        /// <summary>StatDefinition의 Initial로 세운 시작 상태.</summary>
        public static ProgressionState CreateInitial(ChapterProgression chapter);

        /// <summary>StatChanges를 **원자적으로 1회** 적용하고 경계로 clamp한다 (G6-1, G7).</summary>
        public ProgressionState WithCommitted(
            ChapterProgression chapter,
            EpisodeOption chosen);
    }
```

### 3.5 판정과 전이 — 순수 함수

```csharp
    public enum OptionVisibility { Shown, Locked, Hidden }

    public readonly struct ResolvedOption
    {
        public EpisodeOption Option { get; }
        public OptionVisibility Visibility { get; }
        public string LockedReason { get; }   // Locked일 때만
    }

    /// <summary>G6의 세 갈래를 타입으로 고정한다.</summary>
    public enum ChapterAdvanceKind
    {
        AwaitPlayerChoice,   // 고를 수 있는 선택지가 있다
        AutoAdvance,         // 빈 라벨 기본 선택지로 자동 진행
        ChapterEnded,        // 그것도 없다 — 챕터 런이 여기서 끝난다
    }

    public readonly struct ChapterAdvance
    {
        public ChapterAdvanceKind Kind { get; }
        public IReadOnlyList<ResolvedOption> Options { get; }  // 표시 순서 = 배열 순서
        public EpisodeOption AutoOption { get; }
    }

    public static class ChapterTransition
    {
        /// <summary>
        /// ⚠ 조건 판정은 **커밋 전 값**으로 한다 (G6) —
        /// 플레이어가 선택지를 보는 시점의 값이다.
        /// </summary>
        public static ChapterAdvance Resolve(
            ChapterProgression chapter,
            ProgressionState state);
    }

    public static class ConditionEvaluator
    {
        public static bool IsMet(
            in ProgressionCondition condition,
            ProgressionState state);
    }
```

### 3.6 로더 — 침묵 금지의 자리

```csharp
    public sealed class ProgressionDiagnostic
    {
        public string Where { get; }    // "Node[ep_03].Option[1].Conditions[0]"
        public string Message { get; }  // "알 수 없는 Op 'NotEqual'. 가능한 값: GreaterOrEqual, ..."
    }

    public sealed class ProgressionLoadResult
    {
        public ChapterProgression Chapter { get; }
        public IReadOnlyList<ProgressionDiagnostic> Diagnostics { get; }
        public bool IsValid => Diagnostics.Count == 0;
    }

    public static class ProgressionLoader
    {
        /// <summary>
        /// DTO → 모델. **진단을 전부 모은 뒤 한 번에 돌려준다** — 첫 오류에서 멈추면
        /// 작가가 고치기 위해 왕복을 여러 번 해야 한다.
        ///
        /// 진단이 하나라도 있으면 로드 실패로 본다. 부분 통과를 만들지 않는다.
        /// </summary>
        public static ProgressionLoadResult Load(ChapterProgressionDto dto);
    }
```

**로더가 잡는 것 (전부 규율 1):**

| # | 검사 | 왜 |
|---|---|---|
| 1 | 알 수 없는 enum 이름 (`Kind` · `Op` · `Type`) | 이름 문자열로 오므로 오타가 조용히 통과할 수 있다 |
| 2 | 존재하지 않는 `TargetEpisodeId` | 전이가 허공으로 간다 |
| 3 | `Stats`에 정의되지 않은 스탯 키 (조건·StatChange 양쪽) | 없는 키를 0으로 읽으면 조건이 조용히 참/거짓이 된다 |
| 4 | `Bool` 스탯인데 `Minimum/Maximum`이 0/1이 아님 | G4 위반 |
| 5 | `Initial`이 경계 밖 | 시작부터 clamp되어 작가 의도와 다름 |
| 6 | 빈 라벨 기본 선택지가 에피소드당 **2개 이상** | G6-2가 "에피소드당 하나"를 전제한다 |
| 7 | 빈 라벨 기본 선택지에 조건이 달림 | G6-2가 "조건 없음"을 전제한다 |
| 8 | `StartEpisodeId`가 노드에 없음 | 챕터를 시작할 수 없다 |

---

## 4. G절 대응표 — 어느 조항이 어디로 갔나

| 계약 조항 | 어디로 |
|---|---|
| G1 모양 (PascalCase, enum=이름 문자열) | `Dto/` 전체 + `ProgressionLoader` |
| G2 `IntValue` 0 생략 | `ProgressionCondition.Value`의 주석 — **DTO는 `int?`가 아니라 `int`** |
| G3 op 6종 (+GreaterThan/LessThan, NotEqual 제외) | `ComparisonOp` |
| G4 bool = 0/1 + Equal | `StatType.Bool` + 로더 검사 4 |
| G5 관문이 노드→길로 내려옴 | `EpisodeOption.VisibleConditions` / `.Conditions` + `OptionVisibility` 3종 |
| G6 전이 규칙 3갈래 | `ChapterAdvanceKind` + `ChapterTransition.Resolve` |
| G7 스탯 경계 **(빈칸)** | `StatDefinition` — **이 패키지가 새로 소유** |
| G8 `ViaNodeId` **(미결)** | §5 참조 |
| G9 Attachment 표시 제어 **(미결)** | §5 참조 |
| `Node.VisibleConditions` / `UnlockConditions` (언제나 빈 배열) | **모델에서 뺐다** — G5로 길에 내려갔다. DTO에는 스키마 1:1로 남긴다 |
| `IndexText` (언제나 빈 문자열) | **모델에서 뺐다** — v5에서 폐지됐다 |
| `Position{X,Y}` | **모델에서 뺐다** — 저작 레이아웃이다. DTO 통과값 |

---

## 5. 결정이 필요한 것 셋

### ① G8 — 선택지별 자유 씬 점프 (`ViaNodeId`)

계약서 권고: *"Option에 `ViaNodeId` 한 칸을 두고 런타임이 그 Yarn 노드를 거쳐 `TargetEpisodeId`로 잇는다."*

현재 상태: **저작은 되는데 내보내기에 안 나간다.** 발행 경로의 점프가 대본의 *줄*에 매여 있어서 문구를 열쇠로 한 점프를 실을 자리가 없다.

→ **이 스케치에는 넣지 않았다.** 넣으려면 저작 쪽 발행 경로도 같이 고쳐야 하고, 그건 이 패키지 밖이다. 다만 **모델에 한 칸 더하는 것 자체는 비용이 0**이라, 저작 쪽을 언제 고칠지 정하면 그때 같이 넣는 게 좋다(계약서도 G7과 함께 결정하라고 적어 두었다 — G7은 이 초안에서 해결됐으니 지금이 그 자리일 수 있다).

### ② G9 — Attachment 에피소드의 표시 조건

v8에서 관문이 노드→길로 내려가면서 **부착의 표시 조건이 갈 곳을 잃었다.** 부착은 들어오는 간선이 없으니 길이 없고, 따라서 노드가 유일한 주인이다.

→ **v1 비범위로 두었다.** 부착을 실제로 쓸 때 `EpisodeNode.VisibleConditions`를 **부착에 한해** 되살리는 것이 자연스럽다. 그때 "Main은 언제나 빈 배열"이라는 비대칭을 로더가 검사하게 한다.

### ③ 스탯 정의는 어느 파일에서 오나

`StatDefinition`을 만들었지만 **출처를 정해야 한다.** 후보 둘:

| | 장점 | 단점 |
|---|---|---|
| `progression.json` 최상위 `Stats[]` | 챕터와 함께 이동. 계약서 G7의 권고 | 챕터마다 중복 |
| `game.definition.json` | 게임당 하나. 규율 3에 더 맞다 | 두 파일을 같이 읽어야 한다 |

→ **`game.definition.json` 쪽을 권한다.** 스탯은 챕터의 속성이 아니라 게임의 속성이고,
저작 쪽에서 이미 변수·화자·커맨드 어휘를 공급하는 파일이다. 다만 계약서 G7의 권고는
전자이므로 **소유자 결정 필요.**

---

## 6. 오라클 — 어떻게 검증하나

이 패키지는 **처음부터 오라클이 있다.** 새로 짓는 게 아니라 툴에서 이관하는 것이기 때문이다.

| 단계 | 판정 |
|---|---|
| 1. 모델·로더 이관 | 툴의 기존 progression JSON을 로드 → **진단 0건**이어야 한다 |
| 2. 평가기 이관 | `ChapterReachabilityProver`가 이 패키지를 쓰도록 교체 → **증명 결과가 이관 전과 같아야 한다** |
| 3. 런타임 채택 | 실제 전이가 증명과 같은 길을 간다 — G7이 소멸하는 지점 |

**2번이 핵심이다.** `Ked.Presentation.Core` 추출 때 등가성 하네스가 했던 역할을 여기서는
도달성 증명이 한다 — 동작 불변 리팩터를 주장이 아니라 증거로 만든다.

---

## 7. 저장소 구성 (제안)

```
ked-progression/
├─ package.json                        ← UPM. Unity가 git URL로 직접 참조
├─ Runtime/
│  ├─ Ked.Progression.asmdef           ← noEngineReferences: true
│  ├─ Dto/                             ← JSON 1:1. enum도 string
│  ├─ Model/                           ← §3의 타입들
│  ├─ ProgressionLoader.cs             ← 침묵 금지의 자리
│  └─ Evaluation/                      ← ConditionEvaluator · ChapterTransition
├─ Tests/
│  └─ Ked.Progression.Tests.asmdef     ← NUnit
├─ Ked.Progression.csproj              ← 같은 .cs를 가리킨다. dotnet/NuGet용
└─ README.md                           ← 이 문서
```

같은 `.cs`를 asmdef와 csproj가 동시에 가리킨다 — `Ked.Presentation.Core`가 이미
`noEngineReferences: true`로 그 조건을 만족하고 있어 검증된 길이다.

**부수 효과: `dotnet test`로 CI가 돈다.** Unity 없이. 런타임 저장소에서 계속 걸리던
"에디터가 열려 있어 배치가 막힌다"가 이 저장소에는 처음부터 없다.
