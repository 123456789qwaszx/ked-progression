# Ked.Progression 현재 상태 — 이어받는 세션을 위한 인수인계

기준: 2026-08-19 재실측 · 테스트 **140 통과, 실패 0** · 빌드 **경고 0**
(`dotnet test Tests/Ked.Progression.Tests.csproj`)
저장소: `C:\Users\river\Documents\GitHub\ked-progression` · https://github.com/123456789qwaszx/ked-progression

브랜치 `dev` — 논리 단위 커밋(`fb5ea44` 모델 · `5a75152` 전이 · `6ea7cf4` 로더 ·
`ec36d8e` 관통 · `3e1c1a9` 문서 · 이후 저작 쪽 회신 반영). **앞 넷은 따로 빌드·테스트해
확인했으므로 bisect가 성립한다.**
**아직 푸시하지 않았다** — 2026-08-19 기준 `dev`가 `origin/dev`보다 **13커밋** 앞서 있다.
이 디스크에만 있는 작업이다.

---

## 0. 세 문장 요약

1. **챕터 층은 돈다.** 저작 도구가 낸 실제 JSON이 오류 0으로 실리고, 시작에서 엔딩까지 걸어진다.
2. **시나리오 층이 생겼다.** 챕터가 엔딩키로 이어지고 **스탯이 챕터를 넘어간다** —
   에피소드 레이어 마스터 플랜 §0-2가 참이 되는 지점.
3. **세이브 모양이 섰다.** 굽고 되살리며, 콘텐츠가 바뀐 뒤의 로드 규칙까지 고정했다.
   **다만 호스트 쪽 세이브 층이 아직 없다** — 파일로 굽는 것은 이 패키지 밖이다.

> **⚠ 참조한 런타임 코드는 전부 멈춘 브랜치의 것이다 (2026-08-18 실측).**
>
> `ked-presentation-runtime`의 mainline은 `dev`(08-17)인데, 여기서 진행·세이브·변수
> 저장소를 **통째로 걷어냈다.** 직접 확인한 결과 `dev`에 다음이 **0파일**이다:
> `VNSaveData` · `EpisodeSelectionStateData` · `ChapterEndingRule` ·
> `PresentationCore/Flow/`(`StepGate*` · `SequenceSpecSO` · `PresentationSession`).
> 전부 `test13`(08-12 정지) 또는 `phase2-*` 브랜치에만 있다.
>
> **원칙은 유효하고 전제는 거짓이다.** 그 코드에서 뽑은 설계 원칙(P1·P3·P4)은 읽고
> 배운 것이라 브랜치와 무관하게 남는다. 하지만 *"런타임에 이미 있으니 붙이면 된다"*는
> 종류의 문장은 전부 다시 봐야 한다 — 특히 아래 G4를 볼 것.
>
> 출처: 저작 쪽이 보낸 `docs/vntool-handoff.md`의 대응 문서(java-start `25a2810`)와
> 이쪽 실측이 일치한다.

---

## 1. 문서 지도 — 어디부터 읽나

| 문서 | 역할 | 언제 본다 |
|---|---|---|
| [`architecture.md`](architecture.md) | **타입의 정본.** 원칙 다섯 + 실제 형태 + 결정 근거 | 코드를 고치기 전에 |
| [`work-plan.md`](work-plan.md) | 순서 · 게이트 · 남은 결정 | 무엇을 다음에 할지 |
| **이 문서** | 지금 무엇이 참인가 · 함정 · 부채 | 세션 시작할 때 |
| [`../CHANGELOG.md`](../CHANGELOG.md) | 결정 기록 (왜 그렇게 했나) | 판단이 뒤집히려 할 때 |
| [`model-draft.md`](model-draft.md) | **v0 기록.** architecture.md가 대체했다 | 역사만 |
| [`vntool-handoff.md`](vntool-handoff.md) | **저작 도구 쪽에 보내는 것** — 계약 · 요청 · 픽스처 재생성 | 툴을 고칠 때 |
| `java-start/docs/runtime-contract.md` §G | 계약 원본 | **충돌하면 계약서가 이긴다** |

**갈리면**: 코드 > 계약서 > architecture.md > 이 문서.

---

## 2. 원칙 다섯 — 코드를 고칠 때 이것부터

전문은 [`architecture.md`](architecture.md) §1. 셋은 세 구현을 읽고 뽑은 것이다.

| | | 어겼을 때 |
|---|---|---|
| **P1** | 불가능한 상태를 만들 수 없게 한다 | 무효 조합이 타이핑된 뒤 어딘가에서 예외로 걸린다 |
| **P2** | 검증은 경계에, 안쪽은 전체 함수 | `default: return false`가 오타를 "영원히 안 열리는 문"으로 만든다 |
| **P3** | 스펙 / 진행 / 해석을 섞지 않는다 | 세이브에 어제의 판정이 딸려 들어간다 |
| **P4** | 계획을 먼저 확정하고 그 뒤엔 커서만 | 화면에 뜬 것과 실제 판정이 갈린다 |
| **P5** | 조용히 버리지 않는다 | 게임을 돌려 봐야 무엇이 빠졌는지 안다 |

**규칙을 위로 올린다**: 타입이 막는 것 > 생성자가 막는 것 > 로더가 모으는 것.
같은 규칙이 두 곳에 있으면 한쪽만 고쳐지는 날이 온다.

---

## 3. 게이트

| | 판정 | 상태 |
|---|---|---|
| **G0** | 무효 조합이 **컴파일되지 않는다** | ✅ |
| **G1** | 손으로 쓴 시나리오(챕터 3개)가 오류 0으로 실린다 | ✅ `ScenarioProgressionTests` |
| **G2** | **툴이 낸 챕터 JSON**이 오류 0으로 실리고 끝까지 걸어진다 | ✅ `RealExportLoadTests` |
| **G3** | 챕터가 엔딩키로 갈리고 **스탯이 넘어간다** | ✅ `ScenarioProgressionTests` |
| **G4** | 껐다 켜도 같다 — 세이브 왕복 | ✅ `ProgressionSaveTests` |
| **G5** | 도달성 증명이 이관 전후로 같은 답 | ✅ `ReachabilityEquivalenceTests` (코퍼스 7 케이스) |

---

## 4. 있는 것

**폴더가 곧 층이다** (2026-08-19). `Runtime/` 아래 일곱이고, 문서가 쓰는 층 이름을
그대로 쓴다 — 새 어휘를 만들지 않는다.

| 폴더 | 파일 | 무엇 |
|---|---|---|
| `Vocabulary/` | `ComparisonOp` · `ProgressionCondition` · `StatDefinition` · `ProgressionDiagnostic` | 세 층이 함께 쓴다. 조건은 **팩토리만** 연다 |
| `Spec/` | `ScenarioProgression` · `ChapterProgression` · `EpisodeNode` · `EpisodeOption` · `EndingRule` · `ScenarioInvariants` · `ChapterInvariants` | 콘텐츠의 모양. **불변식이 여기 사는 이유는 §3** — 규칙은 타입과 함께 살고 로더는 앞당길 뿐이다 |
| `State/` | `ProgressionState` (+ `ChapterEnding`) | 세이브가 담을 내용 |
| `Transition/` | `ConditionEvaluator` · `ChapterTransition` · `ScenarioTransition` | 지금 무엇을 할 수 있는가. **저장하지 않는다.** 평가기에 방어 코드가 없다 |
| `Loading/` | `ProgressionLoader` · `LoadResults` · `Dto/` | 침묵 금지가 사는 자리 |
| `Save/` | `ProgressionSave` | 유저 데이터 — 굽고 되살린다 |
| `Reachability/` | `ChapterReachability` | 증명. **진행 층과 독립이고 플레이 중엔 안 돈다** |

> `ProgressionDiagnostic`이 `Loading/`이 아니라 `Vocabulary/`에 있다 — 로더뿐 아니라
> **불변식(Spec)과 세이브 복원(Save)도 낸다.** 로더 전용인 `ScenarioLoadResult` ·
> `ProgressionLoadResult`만 `Loading/LoadResults.cs`로 갈라 두었다.
>
> **네임스페이스는 `Ked.Progression` 하나 그대로다**(`Dto`만 예외). 폴더는 읽는 사람을
> 위한 것이고, 호스트는 `using` 한 줄이면 된다.

**픽스처 둘** — `Tests/Fixtures/chapter-sample-export.json`(툴이 실제로 낸 것) ·
`scenario-two-chapters.json`(손으로 쓴 것, 툴에 시나리오 저작이 없다).

**흐름을 읽으려면** `Tests/ArchitectureWalkthroughTests.cs` 하나면 된다 —
로드부터 시나리오 종료까지가 한 화면이고, 트레이스를 문자열로 고정해 두어 흐름이 바뀌면 먼저 깨진다.

---

## 5. 다음 작업 — **이 저장소 안에는 없다**

계획한 게이트 여섯이 전부 닫혔다. 남은 것은 순서대로 이렇다.

### 5-1. `.meta` — 유니티가 물기 전에 반드시

한 번 임포트해 생성한 뒤 가져와 커밋해야 한다. **커밋된 GUID가 참조 안정성**이다(§6-3).
유니티를 여는 사람만 할 수 있다.

### 5-2. `0.2.0` 태그

*"소비자가 가져갈 만할 때"*가 기준이었고 지금이 그렇다(§6-6). 다만 유니티 채택이
계획에 있으면 `.meta`가 먼저다 — 태그를 낸 뒤에 `.meta`를 넣으면 그 태그는 쓸모가 없다.

### 5-3. 챕터 연쇄 증명 — 결정이 남았다

증명은 챕터 하나 단위로 닫혔다. 이으려면 앞 챕터의 **출구 스탯 폭**이 다음 챕터의
**진입 가정**이 되어야 하고, 재료(`ReachabilityResult.SpansFor`)는 이미 있다.
남은 것은 *무엇을* 진입 가정으로 삼을지이고, 안 셋과 권고는
[`work-plan.md`](work-plan.md) §9에 있다.

**콘텐츠가 실제로 둘 이상 이어진 뒤에 한다** — 지금은 손으로 쓴 픽스처뿐이라
만들어도 검증할 실물이 없다.

### 5-4. 저장소 밖 — §7 표 참조

저작 쪽(X2·X3·X7) · 런타임 쪽(X5) · bool 스탯 결정(§6-4).

---

## 6. ⚠ 함정 · 부채

### 6-1. csproj의 "꺼 둔 기능" 셋 — 건드리지 말 것

```xml
<LangVersion>9.0</LangVersion>
<Nullable>disable</Nullable>
<ImplicitUsings>disable</ImplicitUsings>   <!-- Tests에도 -->
```

**dotnet 빌드를 유니티 빌드의 대역으로 만드는 장치다.** 유니티 6000은 C# 9까지고,
nullable 컨텍스트를 안 켜며, `using`을 자동으로 안 넣는다.
세 줄을 켜는 순간 "dotnet 초록"이 "유니티 초록"을 보장하지 못한다.

### 6-2. 빌드 산출물은 `artifacts/`로 간다

`Directory.Build.props`가 그렇게 몬다. UPM은 `Runtime/`을 통째로 유니티에 넘기는데
거기 `obj/.../AssemblyInfo.cs`가 있으면 유니티가 소스로 읽어 중복 어트리뷰트로 깨진다.

### 6-3. `.meta`가 없다 — 유니티 채택 전에 반드시

UPM git 패키지는 유니티가 저장소에 `.meta`를 쓸 수 없다. **커밋된 `.meta`의 GUID가 참조
안정성**이다. 한 번 임포트해 생성한 뒤 가져와 커밋해야 한다.
(`Tests/Fixtures/`는 유니티가 안 읽으므로 그쪽 픽스처 테스트는 dotnet 전용이다.)

### 6-4. bool 스탯을 켤 방법이 데이터에 없다

§G4가 bool 스탯의 증감을 금지하므로, **간선으로는 값이 절대 안 바뀐다.** 초기값이 전부다.
`met_willow` 같은 플래그를 실제로 어떻게 켜는지(Yarn 브리지? 별도 경로?)가
**계약서에도 이 저장소에도 안 적혀 있다.** 실제로 쓰기 전에 소유자가 정해야 한다.

### 6-5. 구 런타임에서 코드를 가져오지 말 것

`ked-presentation-runtime` `test13`의 `EpisodeSelection`은 **명세로만** 읽는다.
평가기가 조용히 틀린다 — `EpisodeConditionEvaluator.cs:100`이 null 조건을 통과시키고,
`:193`이 `Equal`에서 `BoolValue`를 안 보고 `exists`를 돌려준다.
`EpisodeSelectionStateData.cs:57`의 `ShouldShowEpisode`는 둘째 줄이 도달하지 않는다.

반면 같은 저장소의 `PresentationCore/Flow`는 **기준으로 삼을 만하다** —
`GateToken.Immediately`(없음을 명시적 값으로) · `GateInFlight`(저장할 가치 없는 상태를
타입으로 분리) · 계획 선확정이 이 패키지 원칙 P1·P3·P4의 출처다.

### 6-6. 태그는 소비자가 가져갈 만할 때

`[Unreleased]`에 쌓아 두고, G4까지 들어가 실제로 쓸 수 있을 때 `0.2.0`으로 낸다.
`0.x`는 공개 표면을 약속하지 않았다는 뜻이므로 그동안 자유롭게 깨도 된다.

### 6-7. 로더는 2단계다 — "진단을 전부 모은다"는 **단계 안에서만** 참이다

1단계(DTO → 모델)가 통과해야 2단계(참조·불변식)가 돈다. 그래서 `Op`에 알 수 없는 이름
하나가 섞이면 **없는 에피소드·정의되지 않은 스탯 진단이 그 회차에 안 나온다.** 모델을
못 만들면 참조를 검사할 수가 없으니 맞는 동작이지만, 작가가 보기에는 *"고쳤더니 새
오류가 나타났다"*가 된다.

실측(2026-08-19): 같은 데이터에 파싱 오류 1건을 섞으면 진단 3건, 빼면 4건.

### 6-8. `CreateInitialState()`가 둘이고 값이 다르다

```csharp
scenario.CreateInitialState()   // 실제 플레이 — 시나리오 초기값 (D1)
chapter.CreateInitialState()    // 챕터 단독 검증 — 챕터 초기값 = 증명 진입 가정
```

픽스처 `scenario-two-chapters.json`이 일부러 다르게 넣어 두었다 — 시나리오는 `trust=0`,
`ch01`은 `trust=5`. 잘못 부르면 예외 없이 **다른 플레이가 조용히 시작된다.** 주석에 ⚠가
있지만 **타입이 막지는 못하는 자리**다.

---

## 7. 저장소 밖에 걸린 일

| | 어디 | 무엇 | 상태 |
|---|---|---|---|
| X1 | VnTool | exporter가 `Stats`를 낸다 (`Int`→`Number` 이름 번역) | ✅ 닫힘 (`559a1fc`) |
| X2 | VnTool | exporter가 `EndingRules`를 낸다 — `Outcome`을 **명시 문자열**로 | 규격은 정해짐 |
| X3 | VnTool | `간선` 시트에 `종류` 열 (D5) | 미정 — 없으면 로더가 경고로 대체 |
| X7 ✅ | VnTool | `Option.ViaNodeId` (§H-3) — **연출을 매다는 자리** | 이쪽 칸은 들어갔다. 남은 것은 저작 UI와 발행 경로 |
| X4 | VnTool | 시나리오 저작 | 손으로 쓴 JSON으로 먼저 간다 |
| X5 | 런타임 | 진행 블록을 세이브에 싣기 | G4 뒤 |
| X6 | 여기 | `.meta` 생성·커밋 | §6-3 |

> ✅ **X1은 커밋됐다** (`java-start` `559a1fc`). 저작 쪽이 표본까지 보내 주어
> `Tests/Fixtures/chapter-ch01-sample.json`으로 받았다 — 견본 워크북에서 뽑은 이쪽
> 것과 달리 **독립적으로 만들어진 입력**이라 이름 규약이 우연히 맞은 게 아님을 확인해 준다.
>
> ⚠ **저작 쪽이 알려 온 것 하나 더** — `.yarn` 트리오(`Story_*`/`Set_*`/`Pres_*`)가
> **`Story_{이름}.yarn` 하나로 합쳐졌고**, 런타임 경계가
> `EpisodePlayer.StartGameAsync(string nodeName)`이다. **런타임은 챕터도 에피소드 구조도
> 모른다** — 어느 노드를 재생할지는 이쪽 전이기가 정한다.
> `EpisodeNode.DialogueEntryId`가 그 이름이라는 설계가 저쪽에서 확인됐다.

---

## 8. 열린 결정

닫힌 것은 [`architecture.md`](architecture.md) §6에 근거와 함께 있다 (D1 · D2).

| | 결정 | 막는 것 |
|---|---|---|
| **D3** ✅ | `Tokens` · `Flags` | **둘 다 안 넣는다.** 아이템·열쇠는 작가 계층의 Yarn 변수라 진행 JSON에 안 나온다(저작 쪽 확인) |
| **D4** ✅ | 세이브 스키마 버전 정책 | 처음부터 넣었다. 더 높은 버전은 거부 |
| **D5** | 저작 `간선` 시트에 `종류` 열 | 막지 않는다. 없으면 **선택지 문구를 실수로 지운 것**과 의도한 자동 진행을 데이터로 구별할 수 없다 |
| — | bool 스탯을 무엇이 켜나 (§6-4) | 실제로 쓸 때 |
