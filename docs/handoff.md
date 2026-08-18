# Ked.Progression 현재 상태 — 이어받는 세션을 위한 인수인계

기준: 2026-08-18 · 테스트 **115 통과, 실패 0** · 빌드 **경고 0**
(`dotnet test Tests/Ked.Progression.Tests.csproj`)
저장소: `C:\Users\river\Documents\GitHub\ked-progression` · https://github.com/123456789qwaszx/ked-progression

브랜치 `dev` — 논리 단위 커밋 5개(`fb5ea44` 모델 · `5a75152` 전이 · `6ea7cf4` 로더 ·
`ec36d8e` 관통 · `3e1c1a9` 문서). **각 커밋을 따로 빌드·테스트해 확인했으므로 bisect가 성립한다.**
아직 푸시하지 않았다.

---

## 0. 세 문장 요약

1. **챕터 층은 돈다.** 저작 도구가 낸 실제 JSON이 오류 0으로 실리고, 시작에서 엔딩까지 걸어진다.
2. **시나리오 층이 생겼다.** 챕터가 엔딩키로 이어지고 **스탯이 챕터를 넘어간다** —
   에피소드 레이어 마스터 플랜 §0-2가 참이 되는 지점.
3. **아직 저장되지 않는다.** `ProgressionState`가 세이브가 담을 모양이지만,
   그것을 굽고 되읽는 경로가 없다. 남은 절반이 그것이다.

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
| **G4** | 껐다 켜도 같다 — 세이브 왕복 | ❌ **다음 작업** |
| **G5** | 도달성 증명이 이관 전후로 같은 답 | ❌ |

---

## 4. 있는 것

| | 파일 | 무엇 |
|---|---|---|
| 어휘 | `ComparisonOp` · `ProgressionCondition` · `StatDefinition` | 세 층이 함께 쓴다. 조건은 **팩토리만** 연다 |
| 스펙 | `ScenarioProgression` · `ChapterProgression` · `EpisodeNode` · `EpisodeOption` · `EndingRule` | 콘텐츠의 모양 |
| 불변식 | `ScenarioInvariants` · `ChapterInvariants` | **규칙의 유일한 구현.** 생성자는 던지고 로더는 모은다 |
| 진행 | `ProgressionState` (+ `ChapterEnding`) | 세이브가 담을 내용 |
| 해석 | `ChapterTransition` · `ScenarioTransition` | 지금 무엇을 할 수 있는가. **저장하지 않는다** |
| 입구 | `Dto/` · `ProgressionLoader` · `ProgressionDiagnostic` | 침묵 금지가 사는 자리 |
| 평가 | `ConditionEvaluator` | 방어 코드가 없다 — 경계에서 이미 좁혔다 |

**픽스처 둘** — `Tests/Fixtures/chapter-sample-export.json`(툴이 실제로 낸 것) ·
`scenario-two-chapters.json`(손으로 쓴 것, 툴에 시나리오 저작이 없다).

**흐름을 읽으려면** `Tests/ArchitectureWalkthroughTests.cs` 하나면 된다 —
로드부터 시나리오 종료까지가 한 화면이고, 트레이스를 문자열로 고정해 두어 흐름이 바뀌면 먼저 깨진다.

---

## 5. 다음 작업

### 5-1. G4 — 유저 데이터 (`ProgressionSave` · `ProgressionSaveLoader`)

지금 런타임에서 **진행 상태가 어디에도 저장되지 않는다**
(`EpisodeSelectionStateData`가 메모리에만 있고, `VNSaveData`는 대사 위치만 담는다).
둘을 잇는 것이 `chapterLabel`(표시 문자열) 하나뿐이라 신원으로 복원할 방법이 없다.

**어려운 건 직렬화가 아니라 콘텐츠가 바뀐 뒤의 로드다.** 규칙은
[`architecture.md`](architecture.md) §2.7의 표 6줄이고, 그중 조용해도 되는 것은
"새로 생긴 스탯을 `Initial`로 채우는 것" 하나뿐이다.

### 5-2. G5 — 도달성 증명 이관 ← **이 패키지의 오라클**

원본: `java-start/src/Vn.Authoring/Chapters/ChapterReachabilityProver.cs`.
**판정: 이관 전후로 증명 결과가 같아야 한다.** 그다음 챕터 연쇄
(ch01 출구 스팬 → ch02 진입 가정)로 확장한다 — D1이 그것을 전제로 설계됐다.

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

---

## 7. 저장소 밖에 걸린 일

| | 어디 | 무엇 | 상태 |
|---|---|---|---|
| X1 | VnTool | exporter가 `Stats`를 낸다 (`Int`→`Number` 이름 번역) | ✅ **커밋 전** |
| X2 | VnTool | exporter가 `EndingRules`를 낸다 — `Outcome`을 **명시 문자열**로 | 규격은 정해짐 |
| X3 | VnTool | `간선` 시트에 `종류` 열 (D5) | 미정 — 없으면 로더가 경고로 대체 |
| X4 | VnTool | 시나리오 저작 | 손으로 쓴 JSON으로 먼저 간다 |
| X5 | 런타임 | 진행 블록을 세이브에 싣기 | G4 뒤 |
| X6 | 여기 | `.meta` 생성·커밋 | §6-3 |

> ⚠ **`java-start` 작업 트리가 섞여 있다 (2026-08-18 확인).**
> X1(exporter + 그 테스트)과 **무관한 병행 작업**(`YarnBundleEmitter` ·
> `DocumentOutputOptions` · 골든 파일 삭제 · `InlineMarkerTests` 삭제)이 같은 트리에 있다.
> 커밋할 때 **X1만 따로 떼는 것**이 낫다 — 이쪽 픽스처가 그 출력에 묶여 있다.
> 툴 테스트는 631 통과(골든 정리로 수가 줄었다).

---

## 8. 열린 결정

닫힌 것은 [`architecture.md`](architecture.md) §6에 근거와 함께 있다 (D1 · D2).

| | 결정 | 막는 것 |
|---|---|---|
| **D3** | `Tokens`를 살리나 (`Flags`는 버리기로 함) | 없음 — 쓸 때 정한다 |
| **D4** | 세이브 스키마 버전 정책 | G4 |
| **D5** | 저작 `간선` 시트에 `종류` 열 | 막지 않는다. 없으면 **선택지 문구를 실수로 지운 것**과 의도한 자동 진행을 데이터로 구별할 수 없다 |
| — | bool 스탯을 무엇이 켜나 (§6-4) | 실제로 쓸 때 |
