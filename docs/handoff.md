# Ked.Progression 현재 상태 — 이어받는 세션을 위한 인수인계

기준: 2026-08-21 · 브랜치 `dev` · **`origin/dev`보다 앞서 있고 아직 푸시하지 않았다** —
GitHub에는 커밋 2개(8/17, `ComparisonOp`까지)뿐이고 이 디스크에만 Flow·Save·Reachability가 있다.
저장소: `C:\Users\river\Documents\GitHub\ked-progression` · https://github.com/123456789qwaszx/ked-progression

테스트: `dotnet test Tests/Ked.Progression.Tests.csproj` — 마지막 실측(8/19) 140 통과 · 빌드 경고 0.
그 뒤 `EpisodeFlowTests` 4개가 더해졌다. *(2026-08-21 세션은 NuGet이 막힌 환경이라 테스트를
돌리지 못했고, `Runtime/`을 `csc`로 netstandard2.1·C# 9로 컴파일해 **경고 0·오류 0**만 확인했다.
다음 세션이 첫 일로 `dotnet test`를 돌려 숫자를 여기 적을 것.)*

---

## 0. 세 문장 요약

1. **코어는 완성 상태다.** 시나리오 → 챕터 → 에피소드 세 층, 전이·세이브·도달성 증명, 그리고
   호스트가 모는 `EpisodeFlow`까지. `FlowRequestKind` 다섯이 외부 연결점의 전부다.
2. **아직 아무 호스트도 이 코어를 몰아 보지 않았다.** 유니티도 VnTool도 참조 0건(8/21 실측).
   그래서 "견본"처럼 보인다 — 코어의 문제가 아니라 바깥의 문제이고, 2기가 그것을 끝낸다.
3. **차단 항목은 둘이다.** `StatChange` 지정(Set) 칸이 없어 깃발을 쓰는 챕터는 툴이 내보내기를
   거부하고(C1), 시나리오 저작이 없어 단일 챕터를 감쌀 길이 필요하다(C2). 둘 다 하루 일이다.

---

## 1. 문서 지도 — 어디부터 읽나

| 문서 | 역할 | 언제 본다 |
|---|---|---|
| [`principles.md`](principles.md) | **규칙의 정본.** P1~P5 · D1~D5 · 규율 1~4 · §G 대응 · 경계면 · 성장 규칙 | 코드 주석의 태그를 만났을 때 |
| [`architecture.md`](architecture.md) | **타입의 정본.** 실제 형태 + D1·D2 근거 | 코드를 고치기 전에 |
| [`host-integration.md`](host-integration.md) | **호스트가 코어를 쥐는 법.** 유니티 드라이버 · 세이브 합성 · Avalonia 채택 순서 | 유니티·툴 쪽 작업 전에 |
| [`work-plan.md`](work-plan.md) | 2기 순서 · 게이트 · 결정 대기 | 무엇을 다음에 할지 |
| **이 문서** | 지금 무엇이 참인가 · 함정 | 세션 시작할 때 |
| [`design-review.md`](design-review.md) | 외부 리뷰 + 이쪽 대조 | 구조를 다시 의심할 때 |
| [`../CHANGELOG.md`](../CHANGELOG.md) | 결정 기록 (왜 그렇게 했나) | 판단이 뒤집히려 할 때 |
| [`vntool-handoff.md`](vntool-handoff.md) | 저작 쪽에 보낸 2차 회신 | 툴과의 왕복 이력 |
| `java-start/docs/runtime-contract.md` 2부·3부 | 계약 원본 (v9) | **충돌하면 계약서가 이긴다** |
| `java-start/docs/progression-handoff.md` | 저작 쪽 3차 회신 — **깃발 Set 요청이 여기 있다** | C1 착수 전 |
| [`archive/model-draft.md`](archive/model-draft.md) | v0 기록 | 역사만 |

**갈리면**: 코드 > 계약서 > architecture.md > principles.md > work-plan.md.

---

## 2. 있는 것 — 폴더가 곧 층이다

| 폴더 | 파일 | 무엇 |
|---|---|---|
| `Vocabulary/` | `ComparisonOp` · `ProgressionCondition` · `StatDefinition` · `ProgressionDiagnostic` | 세 층이 함께 쓴다. 조건은 **팩토리만** 연다 |
| `Spec/` | `ScenarioProgression` · `ChapterProgression` · `EpisodeNode` · `EpisodeOption` · `EndingRule` · `ScenarioInvariants` · `ChapterInvariants` | 콘텐츠의 모양. 불변식은 타입과 함께 살고 로더는 앞당길 뿐 |
| `State/` | `ProgressionState` (+ `ChapterEnding` · `StatChange`) | 세이브가 담을 내용. 생성 경로는 `CreateInitial`·`Commit`·`Restore` 셋 |
| `Transition/` | `ConditionEvaluator` · `ChapterTransition` · `ScenarioTransition` | 지금 무엇을 할 수 있는가. **저장하지 않는다.** 방어 코드 없음 |
| `Flow/` | `EpisodeFlow` · `FlowRequest` · `EpisodePhase` | **호스트와 만나는 자리.** Pull 모델 — 코어는 아무것도 부르지 않는다 |
| `Loading/` | `ProgressionLoader` · `LoadResults` · `Dto/` | 침묵 금지가 사는 자리. 2단계(DTO→모델, 참조·불변식) |
| `Save/` | `ProgressionSave` | 유저 데이터 — 굽고 되살린다. 어려운 것은 어제 콘텐츠로 저장하고 오늘 콘텐츠로 여는 것 |
| `Reachability/` | `ChapterReachability` | 증명. 진행 층과 독립, 플레이 중엔 안 돈다 |

24파일 · `using`은 `System`·`System.Collections.Generic`뿐 · 비결정성 0(`DateTime`·`Random`·가변 static 없음) ·
인터페이스·콜백·이벤트·async 0 · 네임스페이스는 `Ked.Progression` 하나(`Dto`만 예외).

**픽스처 넷** — `chapter-sample-export.json`(툴이 낸 것) · `chapter-ch01-sample.json`(저작 쪽이
독립적으로 만들어 보낸 것, `ViaNodeId` 실림) · `scenario-two-chapters.json`(손으로 쓴 것) ·
`reachability-oracle.json`(옛 증명기 등가성 코퍼스 7케이스).

**흐름을 읽으려면** `Tests/ArchitectureWalkthroughTests.cs`(로드부터 시나리오 종료까지 한 화면)와
`Tests/EpisodeFlowTests.cs`(호스트 펌프의 모양) 둘이면 된다.

---

## 3. 게이트

| | 판정 | 상태 |
|---|---|---|
| G0 | 무효 조합이 **컴파일되지 않는다** | ✅ |
| G1 | 손으로 쓴 시나리오(챕터 3개)가 오류 0으로 실린다 | ✅ `ScenarioProgressionTests` |
| G2 | **툴이 낸 챕터 JSON**이 오류 0으로 실리고 끝까지 걸어진다 | ✅ `RealExportLoadTests` |
| G3 | 챕터가 엔딩키로 갈리고 **스탯이 넘어간다** | ✅ `ScenarioProgressionTests` |
| G4 | 껐다 켜도 같다 — 세이브 왕복 | ✅ `ProgressionSaveTests` |
| G5 | 도달성 증명이 이관 전후로 같은 답 | ✅ `ReachabilityEquivalenceTests` |
| **H1** | **유니티가 코어를 몬다** | ⬜ 2기 — `work-plan.md` §2 |
| **T3** | VnTool의 옛 증명기와 코어가 같은 답 → 옛 것 삭제 | ⬜ 2기 — `work-plan.md` §3 |

---

## 4. 다음 작업 — 순서대로

1. **`dotnet test`** 돌려 숫자 확인 (8/21 세션이 못 했다).
2. **C1** `StatChange` 지정(Set) — `java-start/docs/work-orders/bool-stat-orders.md` §3 규격 그대로.
   `StatChangeDto.Op`는 없으면 `"Add"`. 불변식 셋을 `ChapterInvariants`에, 로더가 앞당긴다.
   `ChapterReachability`의 적용 함수가 `Set`에서 현재 값을 안 본다. 코퍼스 7케이스는 불변.
3. **C2** `ProgressionLoader.LoadAsSingleChapterScenario`.
4. **C4** `ChapterProgression.CreateInitialState` → `CreateProofEntryState` 개명.
5. **푸시.** `dev`를 `origin/dev`로. 논리 단위 커밋은 이미 그렇게 되어 있다.
6. 유니티에서 한 번 임포트해 **`.meta` 생성·커밋** → **`0.2.0` 태그**.
7. 그 다음은 런타임 저장소의 일이다 — `host-integration.md` §6 U-0~U-5.

---

## 5. ⚠ 함정 · 부채

### 5-1. csproj의 "꺼 둔 기능" 셋 — 건드리지 말 것

```xml
<LangVersion>9.0</LangVersion>
<Nullable>disable</Nullable>
<ImplicitUsings>disable</ImplicitUsings>   <!-- Tests에도 -->
```

dotnet 빌드를 유니티 빌드의 대역으로 만드는 장치다. 유니티 6000은 C# 9까지고 nullable
컨텍스트를 안 켜며 `using`을 자동으로 안 넣는다. 세 줄을 켜는 순간 "dotnet 초록"이 "유니티
초록"을 보장하지 못한다.

### 5-2. 빌드 산출물은 `artifacts/`로 간다

`Directory.Build.props`가 그렇게 몬다. UPM은 `Runtime/`을 통째로 유니티에 넘기는데 거기
`obj/.../AssemblyInfo.cs`가 있으면 유니티가 소스로 읽어 중복 어트리뷰트로 깨진다.

### 5-3. `.meta`가 없다 — 유니티 채택 전에 반드시

UPM git 패키지는 유니티가 저장소에 `.meta`를 쓸 수 없다. **커밋된 `.meta`의 GUID가 참조
안정성**이다. 한 번 임포트해 생성한 뒤 가져와 커밋한다. `.gitignore`에 `.meta`를 넣지 말 것.
(`Tests/Fixtures/`는 유니티가 안 읽으므로 픽스처 테스트는 dotnet 전용이다.)

### 5-4. 깃발을 켜는 칸이 아직 없다 (C1)

§G4가 bool 증감을 금지하므로 간선으로는 값이 안 바뀌고 초기값이 영원한 값이었다.
**2026-08-19 소유자 결정: 지정(Set), bool에만, 간선의 `스탯변화` 칸에서.** 저작 쪽은 끝났고
(`BoolStatSetTests` 15개), 이쪽 DTO에 칸이 설 때까지 깃발을 쓰는 챕터는 내보내기가 거부된다.
`Yarn <<set>>`으로 켜는 안은 **반대** — 대본이 값을 바꾸면 도달성 증명이 그 변화를 못 본다.

### 5-5. 구 런타임(test13)에서 코드를 가져오지 말 것

`ked-presentation-runtime`의 `test13`(8/12 정지)은 **명세로만** 읽는다. 평가기가 조용히
틀린다(`EpisodeConditionEvaluator.cs:100`이 null 조건을 통과, `:193`이 `Equal`에서 `exists`를
돌려줌). 런타임 mainline(`main`, 8/21)에는 진행·세이브·변수 저장소가 **0파일**이다 — 의도적
제거이고 `SCOPE-BOUNDARY.md` §3.3이 이 저장소를 새 주인으로 지목한다.

### 5-6. `CreateInitialState()`가 둘이고 값이 다르다 (C4가 고친다)

```csharp
scenario.CreateInitialState()   // 실제 플레이 — 시나리오 초기값 (D1)
chapter.CreateInitialState()    // 챕터 단독 검증 — 챕터 초기값 = 증명 진입 가정
```

픽스처 `scenario-two-chapters.json`이 일부러 다르게 넣어 두었다(시나리오 `trust=0`, `ch01` `trust=5`).
잘못 부르면 예외 없이 **다른 플레이가 조용히 시작된다.** 타입이 막지 못하는 자리라 이름으로 가른다.

### 5-7. 로더는 2단계다

1단계(DTO→모델)가 통과해야 2단계(참조·불변식)가 돈다. `Op`에 알 수 없는 이름 하나가
섞이면 그 회차에는 없는 에피소드·정의되지 않은 스탯 진단이 안 나온다. 작가에겐 *"고쳤더니
새 오류가 나타났다"*로 보인다. 맞는 동작이고, 알고만 있으면 된다.

### 5-8. `SavePersisted()`는 "썼다"가 아니라 "처리했다"다

`PersistSave` 요청마다 호스트가 디스크에 써야 하는 것이 아니다. 오토세이브 정책은 호스트
것이고, 안 쓰기로 했어도 부른다 — 흐름을 잇는 신호다. 단 쓰기 실패를 삼키고 부르면
"세이브됐다고 믿는 플레이어"가 생기니 실패는 호스트가 화면에 낸다.

### 5-9. `Resume`은 에피소드 처음부터다

`Resume`의 결과는 언제나 `EpisodeEntered` — 그 에피소드의 대사를 처음부터 틀어 달라는
요청이다. 줄 단위 이어 하기는 대사 블록(`lineId`)을 가진 호스트가 그 노드 안을 시킹한다.
코어는 "그 에피소드"까지만 말한다. `host-integration.md` §3.

### 5-10. 두 저장소 모두 `find`를 믿지 말 것

낡은 디렉터리 항목을 보여 준 일이 두 번 있다(저작 쪽 기록). 존재 확인은 `ls`·`grep`으로.

---

## 6. 열린 결정

| | 결정 | 상태 |
|---|---|---|
| D1~D4 | `principles.md` §3 | ✅ |
| D5 | 간선 `종류` 열 | 저작은 v11에서 만들었다. JSON `Kind`는 X3 — 그때까지 로더 경고 |
| H-7 | 깃발의 수명 — 챕터를 넘으면? | **미결.** 권고: 스탯처럼 넘어간다(D1과 일관). 되돌리려면 간선의 `Set false` |
| E-1 | 전역 진행(앨범)의 자리 | 엔딩이 실제로 서너 개 생긴 뒤. 전역 조건은 시나리오 층 간선에만(합의됨) |
| §7 | 챕터 연쇄 증명의 진입 가정 | (나) 엔딩키별 스팬 권고. 콘텐츠가 이어진 뒤 |
