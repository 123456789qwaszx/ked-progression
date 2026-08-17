# Ked.Progression 현재 상태 — 이어받는 세션을 위한 인수인계

기준: 2026-08-17 · 테스트 **27 통과, 실패 0** (`dotnet test Tests/Ked.Progression.Tests.csproj`)
저장소: `C:\Users\river\Documents\GitHub\ked-progression` · https://github.com/123456789qwaszx/ked-progression

> **⚠ 이 박스가 현재 계약이다.**
>
> **스탯 소유권 (확정 — 툴이 이미 정해 두었다).**
> 스탯의 **존재**는 `game.definition.json`의 `variables`가, **값(초기·최소·최대·타입)**은
> **챕터 워크북의 `스탯` 시트**가 소유한다. 같은 `trust`라도 챕터마다 초기값이 다를 수 있기
> 때문이다. 근거: `GameDefinitionStore.cs:31` · `ChapterGraphView.axaml:157` ·
> VnTool `docs/handoff/current-state.md` v9 시트 규격.
> → 챕터 워크북의 한 행 = `ChapterStat(Key, DisplayName, Initial, Minimum, Maximum, Type)`.
>
> **⚠ 그런데 `ChapterProgressionExporter`가 `Stats`를 내보내지 않는다.** `ChapterJson`은
> `ChapterId · DisplayName · StartEpisodeId · Nodes · EndingRules`뿐이다. **계약서 §G7의 ⚠가
> 이것이고, 툴 쪽 한 줄 작업이다.** 이 패키지가 `Stats`를 받으려면 그쪽이 먼저 나가야 한다.
>
> **bool 스탯 (v9).** 경계 0·1 고정. **크기 비교와 증감이 금지된다(오류).** 조건은 `Equal`뿐.
>
> **관문은 노드가 아니라 간선의 것이다 (v8, 2026-08).** `Node.VisibleConditions` ·
> `Node.UnlockConditions`는 **언제나 빈 배열로 나간다.** 실제 값은
> `Option.VisibleConditions`(보여야 함) / `Option.Conditions`(고를 수 있어야 함)에 있다.
> ⚠ 구 런타임(test13)은 노드 조건만 읽으므로, 그 JSON을 그대로 먹이면 **에러 없이 관문이
> 전부 열린다.**
>
> **전이 규칙 (v9) — 세 갈래.**
> ① 고른 선택지의 간선 → `StatChanges`를 **원자적 1회 커밋** 후 `TargetEpisodeId`로 이동
> ② 고를 수 있는 것이 없으면 **문구 없는 간선**(빈 `ChoiceLabel` = 보이지 않는 기본).
>    에피소드당 하나, **관문 금지**
> ③ 그것도 없으면 **챕터 런 종료**
> **조건 판정은 커밋 전 값으로.** `NextOptions`의 배열 순서 = 화면에 뜨는 순서.
>
> **`IntValue`는 0이면 키 자체가 없다 (§G2).** DTO 필드는 반드시 `int` — `int?`로 두면
> 가장 흔한 조건인 `flag == false`가 통째로 어긋난다.
>
> **비교 연산 6종.** `GreaterOrEqual` · `LessOrEqual` · `Equal` · `Exists` ·
> **`GreaterThan` · `LessThan`**(2026-08-16 개방). `NotEqual`은 저작 파서가 닫아 두어
> 나오지 않는다 — 넣지 말 것.

| 이 문서가 가리키는 곳 | 무엇이 있나 |
|---|---|
| [`model-draft.md`](model-draft.md) | 타입 스케치 전문 + §G 대응표 + 미결 셋 |
| [`../README.md`](../README.md) | 규율 4개 · 쓰는 법 · 빌드 함정 |
| [`../CHANGELOG.md`](../CHANGELOG.md) | 버전별 변경과 **결정 기록** |
| `VNStoryEditor_avalonia/docs/runtime-contract.md` §G | 계약 원본 (v9, 2026-08-17) |
| `VNStoryEditor_avalonia/docs/handoff/current-state.md` | 저작 쪽 최신 규격 (v9 시트·전이) |

---

## 1. 무엇을 만드는가

**챕터·에피소드 진행 규칙**을 담는 순수 C# 패키지. 저작 도구와 재생 런타임이 **같은 구현을
공유**하기 위한 것이다.

```
[VnTool]  ──┐
            ├──► Ked.Progression (여기)
[Unity]   ──┘
```

**왜:** 지금 이 규칙이 양쪽에 각자 있거나 한쪽에만 있다. 실측으로 이미 갈렸다 —
조건 종류 5종 vs 2종, 연산자가 양쪽에 각각 2개씩 없고, 관문 위치가 정반대이며,
clamp는 툴에만 있다. **구현을 하나로 만들면 갈릴 수가 없다.**

`netstandard2.1` · 엔진 의존 0 · JSON 파서 없음.

---

## 2. 지금 어디까지 됐나

커밋 2개. 태그 `0.1.0`(껍데기)까지 푸시됨. **그 뒤 작업은 아직 태그 없음.**

### 있는 것

| | 파일 | 무엇 |
|---|---|---|
| ✅ | `Runtime/ComparisonOp.cs` | 비교 연산 6종 (§G3) |
| ✅ | `Runtime/ProgressionCondition.cs` | 조건 + `ConditionKind`(Stat·EpisodeCleared) |
| ✅ | `Runtime/StatDefinition.cs` | **§G7의 빈칸.** 경계·clamp·불변식 검사 |
| ✅ | `Runtime/ProgressionState.cs` | 불변 상태 + `StatChange` |
| ✅ | `Runtime/ConditionEvaluator.cs` | 조건 판정 |
| ✅ | `Tests/*.cs` | 계약 테스트 27개 |

### 없는 것 (다음 작업)

| | 무엇 | 막는 것 |
|---|---|---|
| ❌ | `ChapterProgression` · `EpisodeNode` · `EpisodeOption` | 없음 — 바로 착수 가능 |
| ❌ | `Dto/` (JSON 1:1, enum은 string) | 없음 |
| ❌ | `ProgressionLoader` (검증·진단) | Dto |
| ❌ | `ChapterTransition` (전이 3갈래) | 모델 |
| ❌ | 도달성 증명 이관 | 위 전부 |
| ❌ | `.meta` 파일 | 유니티에서 한 번 임포트해야 생성됨 |

---

## 3. 다음 작업 — 순서

### 3-1. 모델 본체

`ChapterProgression` · `EpisodeNode` · `EpisodeOption`. 모양은
[`model-draft.md`](model-draft.md) §3.3에 있다.

**모델에서 빼기로 한 것** (DTO에는 스키마 1:1로 남긴다):

| 뺀 것 | 왜 |
|---|---|
| `Node.VisibleConditions` · `UnlockConditions` | v8에서 관문이 간선으로 내려갔다. 언제나 빈 배열 |
| `IndexText` | v5에서 폐지. 언제나 빈 문자열 |
| `Position{X,Y}` | 저작 레이아웃이다. 평가 입력이 아니다 |

`ChapterProgression.Stats`(= `IReadOnlyList<StatDefinition>`)를 최상위에 둔다.

### 3-2. DTO + `ProgressionLoader`

**로더가 침묵 금지의 자리다.** 진단을 **전부 모아서 한 번에** 돌려준다 — 첫 오류에서 멈추면
작가가 왕복을 여러 번 해야 한다. 진단이 하나라도 있으면 로드 실패로 본다.

잡아야 할 것:

| # | 검사 |
|---|---|
| 1 | 알 수 없는 enum 이름 (`Kind` · `Op` · `Type`) — 이름 문자열로 오므로 오타가 조용히 통과한다 |
| 2 | 존재하지 않는 `TargetEpisodeId` |
| 3 | `Stats`에 정의되지 않은 스탯 키 (조건·StatChange 양쪽) |
| 4 | `Bool` 스탯인데 경계가 0·1이 아님 |
| 5 | `Bool` 스탯에 **크기 비교**(`GreaterOrEqual` 등) 또는 **증감**이 붙음 → v9에서 오류 |
| 6 | `Initial`이 경계 밖 |
| 7 | 빈 라벨 기본 선택지가 에피소드당 **2개 이상** |
| 8 | 빈 라벨 기본 선택지에 **관문이 달림** (v9: 관문 금지) |
| 9 | `StartEpisodeId`가 노드에 없음 |

⚠ **입력 시그니처는 §5-①이 정해져야 확정된다** (`Stats`를 챕터 JSON에서 받을지, 별도로 받을지).

### 3-3. `ChapterTransition`

`ChapterAdvanceKind` 3갈래(`AwaitPlayerChoice` · `AutoAdvance` · `ChapterEnded`)와
`OptionVisibility` 3종(`Shown` · `Locked` · `Hidden`). 상세는 `model-draft.md` §3.5.

### 3-4. 도달성 증명 이관 ← **이 패키지의 오라클**

원본: `VNStoryEditor_avalonia/src/Vn.Authoring/Chapters/ChapterReachabilityProver.cs` (458줄).
상태 = (에피소드, 스탯 정수 벡터)로 완전 탐색하며 `Math.Clamp(값, Minimum, Maximum)`으로 걷는다.

**판정 기준: 이관 전후로 증명 결과가 같아야 한다.** `Ked.Presentation.Core` 추출 때
등가성 하네스가 했던 역할을 여기서는 이 증명이 한다 — 동작 불변을 주장이 아니라 증거로 만든다.

---

## 4. 이 저장소의 규율

`Ked.Presentation.Core`에서 물려받는다. 상세는 [`../README.md`](../README.md).

| | |
|---|---|
| **1 — 침묵 금지** | 모르는 op·없는 에피소드·미정의 스탯 키를 조용히 기본값으로 떨어뜨리지 않는다 |
| **2 — 의존 0** | 역직렬화는 호스트가. 이 패키지는 필드 1:1 DTO만 |
| **3 — 게임 데이터는 인자** | 코드에 게임 어휘가 없다 |
| **4 — 순수 함수** | 상태를 바꾸지 않고 새 것을 돌려준다 |

**이미 침묵 금지를 박아 둔 세 곳** (구 런타임의 반대로 한 것):
- 정의되지 않은 스탯 키 → `KeyNotFoundException` (구 런타임은 조용히 `false`)
- 초기값이 경계 밖 → 생성 거부 (조용히 clamp하면 작가가 쓴 값과 다르게 시작한다)
- `EpisodeCleared`에 `Exists` 아닌 연산 → 예외

---

## 5. 열린 결정 — 소유자만 정할 수 있는 것

### ① `Stats`가 어떤 경로로 이 패키지에 오나 ← 다음 작업을 막는 유일한 항목

소유권은 정해졌다(⚠ 박스). 남은 것은 **배달 경로**다. 지금 exporter가 `Stats`를 안 낸다.

| 안 | 로더 시그니처 |
|---|---|
| (가) `progression.json` 최상위에 `Stats[]` 추가 | `Load(ChapterProgressionDto dto)` |
| (나) 별도 인자로 받음 | `Load(ChapterProgressionDto chapter, IReadOnlyList<StatDto> stats)` |

**(가)를 권한다** — 값의 주인이 챕터 워크북이므로, 챕터 JSON에 실려 나가는 것이 자연스럽다.
계약서 §G7의 권고와도 같다. 툴 쪽 작업은 `ChapterProgressionExporter.ChapterJson`에
`Stats` 한 줄 추가 + `Node()` 옆에 매핑 하나.

### ② 툴에 없는 세 개념을 살릴 것인가

구 런타임(test13)에는 있었고 툴은 내보내지 않는다.

| 개념 | 판단 후보 |
|---|---|
| `Flag` (bool 전용 조건 종류) | **버려도 될 듯** — v9는 `Stat` 0/1 + `Equal`로 통일했고 그쪽이 단순하다 |
| `ChapterCleared` | 챕터 간 진행이 생기면 필요하다. **언제 넣을지를 적어 둘 것** |
| `Token` (아이템/열쇠 보유) | 지금 쓰는지 확인 필요 |

### ③ `Option.ViaNodeId` (§G8)

"선택지 문구를 열쇠로 자유 씬을 매단다"가 저작에서는 되는데 내보내기에 자리가 없다.
계약서가 *"G7과 함께 결정하라"*고 적어 두었고 **G7은 이 패키지로 해결됐으므로 지금이 그 자리일
수 있다.** 모델에 칸 하나 더하는 비용은 0이고, 저작 쪽 발행 경로 수정이 실제 작업이다.

### ④ Attachment 표시 조건 (§G9)

v8에서 관문이 간선으로 내려가며 부착의 표시 조건이 갈 곳을 잃었다(들어오는 간선이 없다).
**v1 비범위.** 쓸 때 `EpisodeNode.VisibleConditions`를 **부착에 한해** 되살리는 것이
자연스럽고, 그때 "Main은 언제나 빈 배열"이라는 비대칭을 로더가 검사하게 한다.

---

## 6. ⚠ 함정 — 건드리지 말 것

### 6-1. csproj의 "꺼 둔 기능" 셋

```xml
<LangVersion>9.0</LangVersion>
<Nullable>disable</Nullable>
<ImplicitUsings>disable</ImplicitUsings>   <!-- Tests에도 -->
```

**dotnet 빌드를 유니티 빌드의 대역으로 만들기 위한 것이다.** 유니티 6000은 C# 9까지고,
nullable 컨텍스트를 켜지 않으며, `using`을 자동으로 넣어 주지 않는다.
**세 줄을 켜는 순간 "dotnet 초록"이 "유니티 초록"을 보장하지 못한다.**

### 6-2. 빌드 산출물은 `artifacts/`로 간다

`Directory.Build.props`가 그렇게 몬다. UPM은 `Runtime/` 폴더를 통째로 유니티에 넘기는데,
거기 `obj/.../AssemblyInfo.cs`가 있으면 유니티가 소스로 읽어 중복 어트리뷰트로 깨진다.
(`Runtime/obj/`에 NuGet 복원 메타데이터는 남지만 `.cs`가 아니고 gitignore된다.)

### 6-3. 태그는 소비자가 가져갈 만할 때

`[Unreleased]`에 쌓아 두고, 로더까지 들어가 실제로 쓸 수 있을 때 `0.2.0`으로 낸다.
`0.x`는 "아직 공개 표면을 약속하지 않았다"는 뜻이므로 그동안 자유롭게 깨도 된다.

### 6-4. `.meta`를 gitignore하지 말 것

UPM git 패키지는 유니티가 저장소에 `.meta`를 쓸 수 없다. **커밋된 `.meta`의 GUID가 곧 참조
안정성**이다. 아직 없으므로 런타임에서 한 번 임포트해 생성한 뒤 가져와 커밋해야 한다.

---

## 7. 참조 위치

| 무엇 | 어디 |
|---|---|
| 계약 원본 §G (v9) | `VNStoryEditor_avalonia/docs/runtime-contract.md` |
| 저작 쪽 최신 규격 | `VNStoryEditor_avalonia/docs/handoff/current-state.md` |
| 내보내기 (여기에 `Stats` 추가 필요) | `src/Vn.Authoring/Chapters/ChapterProgressionExporter.cs` |
| 챕터 모델 (`ChapterStat` 정의) | `src/Vn.Authoring/Chapters/ChapterGraphModel.cs:110` |
| 도달성 증명 (이관 원본) | `src/Vn.Authoring/Chapters/ChapterReachabilityProver.cs` |
| 구 런타임 구현 (명세 참고용, 이관 대상 아님) | `ked-presentation-runtime` 브랜치 `test13`, `Assets/@Scripts/EpisodeSelection/` |
| 런타임의 경계 문서 | `ked-presentation-runtime/SCOPE-BOUNDARY.md` |

> **구 런타임에서 코드를 가져오지 말 것.** 조건 모델·관문 위치·평가 방식이 전부 다르고,
> 평가기가 침묵 규율을 어긴다(없는 키 → 조용히 `false`). **명세로만 읽는다** — 특히
> 툴에 없는 세 개념(§5-②)이 무엇이었는지 확인하는 용도.
