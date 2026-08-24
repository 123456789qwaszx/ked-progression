# 실행 계획 — 2기를 실제로 시작하기 위한 순서

기준: 2026-08-21 · 이 문서는 **손을 움직이는 순서**다. 단계마다 *무엇을 · 왜 · 주의 · 완료 정의*를
적는다. 배경과 근거는 [`work-plan.md`](work-plan.md), 설계는 [`host-integration.md`](host-integration.md),
규칙은 [`principles.md`](principles.md). 여기서는 반복하지 않는다.

> **목표 한 줄.** 툴이 만든 챕터 JSON을 유니티가 *진행 층까지* 재생한다 — 선택지가 스탯을
> 바꾸고, 관문이 잠기고, 엔딩이 갈리고, 껐다 켜도 같다. 그리고 툴이 같은 코어로 같은 답을 낸다.
>
> **순서의 원칙.** ① 코어를 먼저 닫는다(호스트가 붙은 뒤 DTO를 바꾸면 둘이 같이 움직여야 한다)
> ② 유니티에서 한 번 끝까지 걸어 본다(가장 얇은 경로로) ③ 그 다음 두껍게 한다 ④ VnTool은 그 뒤.
> 단계를 건너뛰지 않는다. 단계 안에서 "완료 정의"가 참이 되기 전에 다음으로 가지 않는다.
>
> **누가 누구에게 맞추나 (소유자 결정 2026-08-21).**
> - **`ked-presentation-runtime`은 굳었다.** 그쪽 것이 우선이다. 경계는 `EpisodePlayer.StartGameAsync(nodeName)`
>   하나이고 **그 시그니처를 바꾸지 않는다.** 세이브의 대사 블록 모양(`{nodeName, lineId, 변수, StageState}`),
>   선택지 프레젠터, 롤백·백로그 — 전부 그대로 쓴다. 드라이버는 런타임 **옆에** 서지 런타임을 고치지 않는다.
> - **VnTool의 엑셀 내부 형태도 굳었다.** 에피소드 시트가 드는 것(대본 키·엔딩키·종류)과 간선 시트가 드는 것
>   (문구·관문·스탯변화·연출)의 역할 분담을 존중한다. 코어가 그 역할을 이미 그대로 타입으로 옮겼다
>   (노드=엔딩키의 주인, 간선=조건·효과·연출의 주인). **시트를 바꾸는 요청은 하지 않는다** — JSON에
>   실려 나가는 칸을 늘리는 것(X3·X2)과 새 판을 더하는 것(X4 시나리오)만 한다.
> - **코어가 맞춘다.** 양쪽이 굳은 것과 코어가 어긋나면 고치는 쪽은 코어다.

---

## 전체 주의사항 — 모든 단계에 걸린다

| | |
|---|---|
| **세 저장소의 상태를 먼저 본다** | 코어 `dev`는 미푸시(디스크에만), VnTool 작업본은 `java-start`(GitHub `VNStoryEditor_avalonia`보다 앞섬), 런타임은 `main`(8/21). 남의 저장소 상태를 적을 때는 남의 저장소를 연다 — 저작 쪽이 "없는 병목"을 한 번 만들었던 이유 |
| **`find`를 믿지 말 것** | 두 저장소에서 낡은 디렉터리 항목을 보여 준 일이 있다. 존재 확인은 `ls`·`grep` |
| **csproj의 꺼 둔 셋을 켜지 않는다** | `LangVersion 9.0` · `Nullable disable` · `ImplicitUsings disable`. 켜는 순간 dotnet 초록이 유니티 초록을 보장하지 못한다 |
| **코어에 `using UnityEngine` 한 줄** | 패키지의 존재 이유가 끝난다. asmdef가 컴파일 오류로 막지만, 막히면 "어떻게 우회하나"가 아니라 "왜 넣으려 했나"를 묻는다 |
| **안쪽에 null 체크를 넣고 싶어지면 멈춘다** | 경계가 샜다는 신호(P2). 코드를 고치지 말고 어느 경계가 샜는지 찾는다 |
| **`FlowRequestKind` 여섯 번째는 혼자 정하지 않는다** | 경계면 확장이다 |
| **한 단계 = 한 커밋 묶음** | 1기처럼 논리 단위로 끊어 bisect가 성립하게 한다. 문서 변경은 코드 커밋에 같이 싣는다 (규범이 코드와 떨어지면 우연히 유지된다) |
| **테스트가 먼저 깨지게 만든다** | 기존 동작을 바꿀 때는 "바뀌면 깨지는 테스트"를 먼저 확인한 뒤 바꾼다. 코퍼스·골든 테스트가 그 장치다 |

---

## 0단계 — 세션 시작: 실측

**무엇.** `dotnet test Tests/Ked.Progression.Tests.csproj` · `git status` · `git log origin/dev..dev --oneline`.

**왜.** 8/21 세션은 NuGet이 막혀 테스트를 못 돌렸다. 컴파일(csc, netstandard2.1·C# 9)만 경고 0·오류 0이다.
숫자 없이 시작하면 뒤의 "깨지나"를 판단할 기준이 없다.

**주의.** `artifacts/`가 생기는 것이 정상이고 `Runtime/obj/`에 `.cs`가 생기면 비정상이다.

**완료.** `handoff.md` 머리의 테스트 수가 실측 숫자로 바뀌어 있다. 실패 0.

---

## 1단계 — 코어 C1: `StatChange` 지정(Set)

**무엇.** 간선의 스탯 변화에 "정하기"를 싣는 칸.

| 파일 | 변경 |
|---|---|
| `Loading/Dto/ProgressionDto.cs` | `StatChangeDto.Op` (`string`). 없음/`"Add"` = 더하기, `"Set"` = 정하기 |
| `State/ProgressionState.cs` | `StatChange`에 `Kind` (`StatChangeKind { Add, Set }`). 생성은 팩토리 둘 — `StatChange.Add(key, amount)` · `StatChange.Set(key, value)`. `Commit`의 적용이 `Set`에서 현재 값을 보지 않는다 |
| `Loading/ProgressionLoader.cs` | 이름 변환. 알 수 없는 `Op` → 진단(규율 1). 비어 있으면 `Add` |
| `Spec/ChapterInvariants.cs` | 셋: `Set`은 `StatType.Bool`에만 · 값은 0/1만 · 한 간선에 같은 키를 두 번 정하면 오류. 로더가 앞당긴다 |
| `Reachability/ChapterReachability.cs` | 증감 적용 함수가 `Set`에서 대입. **이 함수는 더 이상 단조가 아니다** — 주석으로 남긴다 |
| `Tests/` | 불변식 셋 각 1 · `Commit`에서 Set · 로더 이름 변환 · **코퍼스 7케이스 불변** · 깃발이 관문을 여는 케이스 1 (저작 쪽이 떠 준다고 했다 — `java-start` `BoolStatSetTests`의 그래프를 받아 픽스처로) |

**왜.** 저작은 8/19에 끝냈고(`java-start/docs/work-orders/bool-stat-orders.md`), 이 칸이 없어서
**깃발을 쓰는 챕터는 내보내기가 거부된다.** 실콘텐츠가 코어에 닿기 전에 막히는 유일한 자리.
Yarn `<<set>>`으로 켜는 대안은 이미 반려됐다 — 대본이 값을 바꾸면 도달성 증명이 못 본다.

**주의.**
- `Op`를 enum이 아니라 **이름 문자열**로 받는다(§G1). DTO에 enum을 두면 모르는 이름을 잡을 수 없다.
- `Amount`는 `int` 그대로. `Set`일 때 뜻만 "정할 값"이다. 필드를 늘리지 않는다.
- `StatChange`의 공개 생성자를 없애고 팩토리만 연다(P1) — `Set`에 2가 들어오는 것을 타이핑 단계에서
  막을 수는 없지만(bool 여부를 타입이 모른다), 적어도 `Kind`와 의미가 분리되지 않게 한다.
- 기존 픽스처 4개에 `Op`가 없다. **한 글자도 안 바뀌어야 한다.** 로더 테스트에 "Op 없음 = Add"를 명시로 건다.
- 저작 쪽에 알린다 — `runtime-contract.md` 3부 §G-6을 ✅로 바꾸는 것은 저쪽 일이고, 이쪽은
  `vntool-handoff.md`에 "칸이 섰다, `BoolSetNotCarried`를 지워도 된다"를 적는다.

**완료.** 테스트 전부 초록 · 코퍼스 불변 · 깃발 케이스에서 "켜면 도달 가능, 안 켜면 도달 불가" 짝이
둘 다 통과 · CHANGELOG "추가"와 "결정"에 한 항목씩 · `principles.md` §4 G-6 행이 ✅.

---

## 2단계 — 코어 C2: 단일 챕터 시나리오

**무엇.** `ProgressionLoader.LoadAsSingleChapterScenario(ChapterProgressionDto)` → `ScenarioLoadResult`.
챕터 하나를 `ScenarioId = ChapterId`, `StartChapterId = ChapterId`, 규칙 0개, `Stats`는 챕터 것을
승격한 시나리오로 감싼다.

**왜.** 툴은 챕터 JSON만 낸다(시나리오 저작 X4 미착수). `EpisodeFlow`는 시나리오를 요구한다.
손으로 시나리오 JSON을 쓰는 길도 있지만, "챕터 하나만 테스트 플레이"는 시나리오 저작이 생긴
뒤에도 영원히 필요하다 — 호스트 둘이 각자 감싸는 코드를 만들지 않게 코어에 한 번 둔다.

**주의.** 챕터 `Stats`가 없는 챕터(옛 JSON)는 **오류**다 — 시나리오가 스탯 정의의 주인인데 줄 것이
없다. 조용히 빈 목록으로 만들지 않는다. `EndingRules`가 있으면 `NextChapter`로 가는 규칙은 허공
간선이라 `ScenarioInvariants`가 잡는다 — 그게 맞는 동작이다(단일 챕터인데 다음 챕터를 적었다).

**완료.** `chapter-ch01-sample.json`을 이 헬퍼로 감싸 `EpisodeFlow.Begin` → 두 엔딩 다 `ScenarioFinished`까지
걸어지는 테스트. `ArchitectureWalkthroughTests`에 한 케이스 추가.

---

## 3단계 — 코어 C4: `CreateInitialState` 개명

**무엇.** `ChapterProgression.CreateInitialState()` → `CreateProofEntryState()`. 주석에 "플레이 시작이
아니다"를 남긴다. `ChapterReachability`·테스트의 호출부 교체.

**왜.** 시나리오 것과 이름이 같아 잘못 부르면 예외 없이 다른 초기값으로 플레이가 시작된다
(픽스처가 일부러 `trust` 0 vs 5로 다르게 둠). 타입이 못 막는 자리라 이름으로 가른다.

**주의.** 기계적 변경이다. 이 김에 다른 것을 손대지 않는다.

**완료.** 컴파일 · 테스트 초록 · `handoff.md` §5-6이 "고쳤다"로 바뀜.

---

## 4단계 — 푸시 · `.meta` · `0.2.0`

**무엇.**
1. `dev` → `origin/dev` 푸시. (1~3단계 커밋 포함)
2. 런타임 저장소에서 `Packages/manifest.json`에 `"com.ked.progression": "https://github.com/123456789qwaszx/ked-progression.git#dev"`로
   **임시** 참조 → 유니티가 임포트하며 `.meta`를 `Library/PackageCache` 안에 만든다 → 그 `.meta`들을
   코어 저장소의 같은 경로에 복사해 **커밋**.
3. `package.json` `version`을 `0.2.0`, CHANGELOG `[Unreleased]` → `[0.2.0] - 날짜`, 태그 `0.2.0`, 푸시.
4. 런타임 `manifest.json`을 `#0.2.0`으로 바꾼다.

**왜.** UPM git 패키지는 유니티가 저장소에 `.meta`를 쓸 수 없다. 커밋된 `.meta`의 GUID가 참조
안정성이다. 태그 뒤에 `.meta`를 넣으면 그 태그는 쓸모가 없다 — 그래서 이 순서다.

**주의.**
- `Tests/Fixtures/*.json`은 유니티가 읽지 않는다(픽스처 테스트는 dotnet 전용). `Tests/*.cs`는
  `Ked.Progression.Tests.asmdef`로 EditMode에 올라간다 — 픽스처를 읽는 테스트는 유니티에서 **실패**한다.
  `[Category("DotnetOnly")]`나 `#if !UNITY_2017_1_OR_NEWER`로 가른다. 이것을 4단계에서 처음 맞닥뜨린다.
- `.gitignore`에 `*.meta`가 없는지 확인. `artifacts/`·`obj/`는 무시되고 있어야 한다.
- 브랜치(`#dev`)를 가리킨 채 두지 않는다. 남의 커밋이 내 빌드를 깬다.

**완료.** 런타임 유니티 콘솔에 `Ked.Progression` 어셈블리가 뜨고 컴파일 오류 0 · EditMode에서
코어 테스트가 돈다(픽스처 제외) · `git tag`에 `0.2.0` · `handoff.md`가 "푸시됨, 0.2.0"으로 갱신.

---

## 5단계 — 유니티 U-0·U-1: 배선과 완료 신호 — **런타임을 고치지 않는다**

**무엇.**
- U-0: `Ked.Presentation.Runtime.asmdef`의 `references`에 `Ked.Progression`. `com.unity.nuget.newtonsoft-json` 추가.
  런타임 코드 변경은 **이 두 줄의 참조뿐**이다.
- U-1: 완료 신호는 `StartGameAsync`가 이미 돌려주는 `Task`의 완료다. **시그니처를 바꾸지 않는다.**
  "정상 완료"와 "끊김"의 구별은 드라이버가 스스로 안다 — 대사를 멈출 수 있는 것은 드라이버가 부르는
  `StopDialogueAsync`뿐이므로(타이틀로 나가기 등), 드라이버가 *자기가 멈췄는지*를 플래그로 들고
  `await` 뒤에 본다. 예외는 `Faulted`로 드라이버가 잡는다.

**왜.** 런타임은 굳었다. `SCOPE-BOUNDARY.md` §3.3이 정한 경계("이 노드를 재생해 줘"까지)를 넘어서
런타임에 무엇을 돌려 달라고 하면 경계가 두꺼워진다. 드라이버가 아는 정보로 충분하다.

**주의.**
- `DialogueRunner.onNodeComplete`/`onDialogueComplete`는 쓰지 않는다 — 노드마다 오고, `<<jump>>` 체인
  전체의 끝은 Task 완료가 정확하다.
- 디버그 키 `2`(`_yarnEntryKey` 직접 재생)는 그대로 둔다. 드라이버를 거치지 않는 대사 층 단독 테스트 경로다.
  단 **드라이버가 도는 중에 누르면** 두 재생이 겹친다 — 드라이버가 도는 동안은 키를 무시하게 하거나,
  그 키를 드라이버 밖의 디버그 씬에서만 쓴다. 런타임 코드를 고치는 대신 **입력 바인딩에서 가린다.**
- `Newtonsoft` 역직렬화는 PascalCase 키를 기본으로 맞춘다. `ConditionDto.IntValue`가 키 없이 오면 `int` 기본값 0 — 의도된 동작(§G2).

**완료.** 런타임 컴파일 0 오류 · `git diff`에 런타임 `.cs` 변경이 asmdef 참조 외에 **없다** ·
드라이버 스텁이 노드 하나를 `await`하고 "완료"를 로그로 찍는다.

## 6단계 — 유니티 U-2: `ProgressionDriver` — **게이트 H1**

**무엇.** `Assets/Scripts/Game/ProgressionDriver.cs` 한 파일. MonoBehaviour가 아니다. `host-integration.md`
§2.2의 루프 그대로. 선택지는 **임시 UI**(기존 `VNOptionItem` 프리팹을 빌려 텍스트만, 잠김 표시 없이).
세이브는 **로그만 찍고 `SavePersisted()`**(§1 규칙 3 — "처리했다"). `VNAppBootstrap`에서 생성,
타이틀의 "시작"이 `RunAsync(scenario, null)`을 부른다. 입력 JSON은 `StreamingAssets/progression/ch01.progression.json`
(= `chapter-ch01-sample.json` 복사본)을 C2 헬퍼로 감싼다.

**왜.** 이것이 2기 전체를 증명하는 지점이다. 리뷰가 "미수금"이라 부른 것이 여기서 현금이 된다.
두껍게 만들기 전에 **가장 얇은 경로로 끝까지** 간다 — 어디가 막히는지는 걸어 봐야 안다.

**주의.**
- **드라이버가 `EpisodeFlow`를 쥐는 유일한 객체다.** 다른 어떤 스크립트도 `Pending`을 보지 않는다.
  `VNScreenBindings`가 "다음 에피소드" 같은 것을 직접 하고 싶어지면 드라이버에 메서드를 하나
  더하지, 흐름을 밖으로 꺼내지 않는다.
- `PlayVia`도 `StartGameAsync(NodeName)`다 — 연출 노드는 Story 노드다. 별도 경로를 만들지 않는다.
- 런타임의 `VNScreenBindings.Title`이 이미 `EpisodePlayer`를 받아 "시작"을 배선한다. 그 자리에 드라이버를 **대신** 꽂지 말고, 드라이버가 `EpisodePlayer`를 안에 품고 타이틀이 드라이버를 부르게 한다. 런타임 쪽 파일은 `VNAppBootstrap`의 생성 한 줄만 는다.
- 요청을 수행하는 동안 롤백·백로그·스킵이 일어나도 드라이버는 모른다. `Stopped`로 끝나면
  `DialogueCompleted()`를 부르지 **않고** 드라이버를 멈춘다(같은 에피소드 재진입은 `Resume`으로).
- `Finished`에서 `Outcome.Kind`가 `DeadEnd`면 화면에 **"미완성"**이 떠야 한다. `ScenarioEnded`와 섞지 않는다.
- Domain Reload를 꺼도 코어 static은 빈 배열 하나뿐이라 안전하다. 드라이버 쪽에 static을 두지 않는다.

**완료 (H1).** `ch01.progression.sample.json`이 유니티에서 **시작 → "라루를 믿는다" → 믿는길 → (자동) →
좋은끝 → `ScenarioFinished("ch01_true")`**, 그리고 다른 길로 `ch01_alone`까지. 콘솔에 `PersistSave`
요청이 에피소드마다 찍히고 `Stats`에 `trust=2` 또는 `fatigue=1`이 보인다. `ViaNodeId`가 실린 간선에서
`PlayVia`가 찍힌다(재생은 H4에서).

---

## 7단계 — 유니티 U-3: 에피소드 선택지 뷰 — **게이트 H2**

**무엇.** `IChapterOptionsView.ShowAsync(IReadOnlyList<ResolvedOption>, int hiddenCount) → Task<int>`.
`Locked`는 회색 + `LockedReason`(비어 있으면 사유 없이 회색만). 배열 순서 그대로. 잠긴 항목은
클릭이 안 된다(드라이버가 `Choose`에서 던지기 전에 UI가 막는다 — 그래도 던지는 건 남긴다).

**왜.** Yarn `->` 옵션 프레젠터(`VNOptionsPresenter`)와는 다른 것이다 — 잠긴 채 보이고, 고르면
스탯이 커밋된다. 같은 프리팹은 재사용하되 같은 프레젠터가 아니다.

**주의.** `BlockingCondition`을 플레이어에게 보여 주지 않는다(디버그 오버레이에만). `HiddenCount`는
로그에만. `HideWhenLocked`는 이미 코어가 목록에서 뺐으므로 UI가 다시 볼 일이 없다.

**완료 (H2).** `trust >= 3` 관문이 달린 선택지가 있는 테스트 챕터(저작 쪽 견본 워크북에서 하나 내보낸다)에서
첫 방문엔 회색+사유, 스탯을 올린 뒤 재방문엔 활성. 숨김 조건 미달 선택지는 보이지 않는다.

---

## 8단계 — 유니티 U-4·U-5: 세이브와 이어 하기 — **게이트 H3**

**무엇.**
- `ISaveStore` (`Write(slot, SaveFile)` / `Read(slot)`). 구현은 `Application.persistentDataPath` 아래 JSON 한 파일.
- `SaveFile { Progression: ProgressionSaveDto, Dialogue: {...} }` — 대사 블록의 모양은 **런타임이 `SCOPE-BOUNDARY.md` §3.2에 선언한 것 그대로** (`{ nodeName, lineId, 변수, StageState }`). 이번엔 `nodeName`만 채우고 나머지 칸은 비워 둔다 — 칸의 이름을 이쪽이 정하지 않는다.
- 드라이버의 `PersistSave` 처리가 실제로 쓴다. 실패하면 화면에 알리고 그래도 `SavePersisted()`.
- 타이틀 "이어 하기": `Read` → `ProgressionSave.Restore` → 오류면 사유 표시하고 거부 → 경고면 로그 →
  `RunAsync(scenario, restored.State)`.

**왜.** "껐다 켜도 같다"를 실기로. 어려운 것은 직렬화가 아니라 **어제 콘텐츠로 저장하고 오늘 콘텐츠로
여는 것**이고, 그 규칙은 코어에 있다 — 호스트는 거부를 **거부로** 보여 주기만 하면 된다.

**주의.**
- 줄 단위 시킹은 하지 않는다. `Resume`은 에피소드 처음부터다. 줄 시킹이 돌아오면 §C1(`#line:`)·§C3이
  같이 되살아난다 — 그건 별도 작업이고 여기서 끼워 넣지 않는다.
- 두 블록의 노드가 다르면(`DialogueEntryId` ≠ `nodeName`) 합성 오류다. 지금은 대사 블록이
  `nodeName`만 들고 있어 코어 것이 우선이고, 로그로 남긴다.
- 슬롯은 하나로 시작한다. 슬롯·썸네일·오토세이브 정책은 호스트 것이고 지금 정하지 않는다.
- `SchemaVersion`을 호스트가 손대지 않는다. 파일 전체의 버전이 필요하면 **바깥 봉투**에 따로 둔다.

**완료 (H3).** 에피소드 경계에서 저장 → 플레이 종료 → 이어 하기 → 같은 에피소드에서 같은 `Stats`로 계속.
그 뒤 JSON에서 현재 에피소드를 지우고 이어 하기 → **거부되고 사유가 뜬다.** 스탯 하나를 정의에서
지우고 이어 하기 → 경고 로그 + 계속.

---

## 9단계 — 유니티 H4: 연출(`ViaNodeId`) 재생

**무엇.** `PlayVia` → `StartGameAsync(NodeName)` → `ViaCompleted()`. 표본의 `"엔딩 ch01_true"` 노드가
실제 Story 노드로 존재해야 한다(저작 쪽이 낸 `.yarn`에 있는지 확인, 없으면 견본에 하나 추가).

**왜.** 경계면 셋(`DialogueEntryId`·`ViaNodeId`·`EndingKey`) 중 `ViaNodeId`가 실물로 한 번 통과하는 지점.

**주의.** 연출 노드에 파라미터를 실으려는 유혹 — 지속시간·이징은 **노드 안의 커맨드**로 산다.
`FlowRequest`에 싣지 않는다.

**완료 (H4).** 간선을 지날 때 연출 노드가 재생되고 **그 뒤에** `Committed`·세이브가 찍힌다(순서가 중요하다 —
연출은 커밋 전, 상태는 아직 안 바뀐 채).

---

## 10단계 — VnTool T1·T2: 코어를 물고 진단을 덧붙인다

**무엇.**
- `src/Vn.Authoring/Vn.Authoring.csproj`에 `<ProjectReference Include="..\..\..\ked-progression\Runtime\Ked.Progression.csproj" />`
  (`Ked.Presentation.Core`와 같은 줄에). 경로는 두 저장소가 형제 폴더라는 전제 — CI에서는 서브모듈 또는 NuGet으로 바꾼다.
- `ChapterGraphModel → ChapterProgressionDto` 어댑터. **exporter가 직렬화 직전에 만드는 객체가 이미 그 모양**이므로
  `ChapterJson`을 DTO로 바꾸거나, `ChapterJson`을 DTO로 **대체**한다(후자를 권한다 — 모양 사본이 하나 준다).
- `ChapterValidator`가 `ProgressionLoader.Load(dto).Diagnostics`를 자기 진단에 **덧붙인다**. 경로 문자열(`Nodes[ep03]...`)을
  시트 좌표로 번역하는 작은 매핑이 필요하다.

**왜.** 같은 DTO를 유니티와 툴이 읽으면 계약이 실물로 두 번 통과한다. 시트는 건드리지 않는다 — 바뀌는 것은 `ChapterGraphModel` 뒤의 직렬화 객체뿐이고, 에피소드 시트/간선 시트의 역할(노드=엔딩키, 간선=관문·효과·연출)은 코어 타입이 이미 그대로다. 그리고 툴의 검증 보고에 코어
진단이 같이 서면 "툴은 통과했는데 런타임이 거부"가 구조적으로 사라진다.

**주의.**
- `ChapterJson`을 DTO로 대체하면 `ProgressionSampleGoldenTests`가 직렬화 결과의 키 순서 변화로 깨질 수 있다.
  **의미가 같으면 골든을 갱신**하되, `ViaNodeId`·`Stats`·`EndingKey`가 글자 그대로 남는지 따로 건 테스트는 그대로 초록이어야 한다.
- 두 검증기가 **다른** 답을 내는 파일이 나오면 그것이 진짜 발견이다. 어느 쪽이 맞는지 정하고 한쪽을 고친다 — 둘 다 남기지 않는다.
- 엔딩키 충돌 거부와 깃발 `Set` 거부(C1 뒤에는 지운다)는 저작 쪽이 계속 소유한다.

**완료 (T1·T2).** 견본 워크북 전부가 코어 로더에서 진단 0. 검증 보고에 코어 진단 절이 선다. 두 검증기의 답이 같다.

---

## 11단계 — VnTool T3·T4: 증명기 교체 — **게이트 T3**

**무엇.** `ChapterReachabilityProver.Prove` 호출부(`ChapterValidator.cs:75` · `ChapterFixtureWalker.cs:75`)를
`ChapterReachability.Prove`로. **한동안 둘 다 돌려** 결과를 대조하는 테스트(`ReachabilityParityTests`)를
두고, 모든 견본 + 코퍼스 7 + 깃발 케이스에서 차이 0을 확인한 뒤 옛 증명기와 파일 머리의
"바꾸면 알린다" 주석을 **삭제**한다. 결과 타입 변환(`ReachabilityResult` → 화면이 쓰는 `ChapterReachabilityResult`)은
어댑터 한 장.

**왜.** 구현이 하나가 되는 지점. 코어의 `reachability-oracle.json`이 "옛 답"이 아니라 **유일한 답**이 된다.
"증명기를 바꾸면 알린다"는 의무가 소멸한다.

**주의.**
- 대조는 **도달 가능 집합 · 완전 탐색 여부 · 에피소드별 스탯 폭** 셋 전부. 집합만 같고 폭이 다르면 다른 것이다.
- 화면의 캐시(내용해시 → 결과)는 그대로 둔다. 답을 바꾸지 않는다.
- `StateLimit`이 양쪽 같은지(250,000) 확인. 다르면 큰 그래프에서 `ExplorationComplete`가 갈린다.

**완료 (T3).** 대조 테스트 차이 0 → 옛 증명기 삭제 → 툴 테스트 전부 초록 → `vntool-handoff.md`·
`runtime-contract.md` 3부의 "증명기 통보" 항목을 양쪽에서 닫는다.

---

## 12단계 — VnTool T5: 미리보기 플레이 — **게이트 T5**

**무엇.** 챕터 그래프 탭에 "걷기" 모드. `EpisodeFlow`를 즉시 완료 호스트로 몬다 — `PlayDialogue`/`PlayVia`는
즉시 완료, `PresentOptions`는 작가가 노드 위의 간선을 클릭, `PersistSave`는 무시(`SavePersisted()`만),
현재 노드·스탯·잠긴 선택지를 그래프 위에 그린다. 시작 상태는 `chapter.CreateProofEntryState()`(챕터 진입 가정).

**왜.** "같은 판정기를 두 도구가 쓴다"의 실체. 도달성 증명은 모든 길을, 걷기는 이 길을 말한다 —
둘 다 `ChapterTransition.Resolve` 하나.

**주의.** 이 모드에서도 `EpisodeFlow`를 쥐는 객체는 뷰모델 하나다. 그래프 노드가 각자 `Pending`을 보지 않는다.
진입 가정 상태와 실제 플레이 초기값이 다름을 화면에 표시한다(D1).

**완료 (T5).** 같은 챕터·같은 선택 순서로 툴의 걷기와 유니티의 플레이가 같은 선택지 목록과 같은 엔딩을 낸다.
(손으로 대조한다 — 자동화는 시나리오 저작 뒤.)

---

## 13단계 — 저작 확장 X3 · X2 · X4 (순서대로, 필요해질 때)

| | 무엇 | 왜 | 완료 |
|---|---|---|---|
| **X3** | JSON 간선에 `Kind`("PlayerChoice"/"AutoAdvance"). 저작엔 v11 `종류` 열이 **이미 있다 — 시트 변경 없음**, 내보내기 칸 하나. 코어 로더는 `Kind`가 있으면 그것을 믿고 없으면 지금처럼 `ChoiceLabel == ""`로 읽되 경고 | D5가 닫힌다 — "문구를 실수로 지운 것"이 오류가 된다 | 로더의 자동 진행 경고가 `Kind` 있는 파일에서 사라진다. 문구 없는 `PlayerChoice`가 오류로 잡힌다 |
| **X2** | exporter가 `EndingRules`를 낸다 — `Outcome` 명시 문자열, 불변식 넷(`vntool-handoff.md` §2) | 시나리오 층의 간선. 없으면 챕터가 이어지지 않는다 | 두 챕터짜리 견본이 코어에서 `NextChapter`로 갈린다 |
| **X4** | 시나리오 저작(챕터를 잇는 판) → `ScenarioProgressionDto`. 기존 챕터 워크북은 그대로, **새 판(시트 또는 파일)을 더한다** | 손으로 쓴 JSON을 대체 | 툴이 낸 시나리오 JSON이 유니티에서 챕터 경계를 넘는다 (게이트 G3의 실기판) |

X4가 서면 `work-plan.md` §7(챕터 연쇄 증명, (나) 엔딩키별 스팬)을 착수한다.

---

## 14단계 — 검증: 실제 작품 한 챕터 포팅

**무엇.** 리뷰 §8의 제안. 일본식 분기 VN 한 장 · 브란테식 1막 · 텍스트RPG 한 챕터 중 **기획이 가장
가까운 하나**를 스프레드시트로 이 모델에 옮겨 본다. 못 쓰는 것 두세 개가 나오고, 그것이 진짜 갭 목록이다.

**왜.** `work-plan.md` §5 트랙 E의 다섯 갭은 상상이고 포팅 결과가 실측이다. 표현력을 넓힐 때마다
`principles.md` §6의 표(세 소비자 중 누가 감당하나)를 채운다 — 답 못 하는 확장은 보류.

**완료.** 갭 목록이 `work-plan.md` §5에 실측으로 교체되고, 각 항목에 "어느 소비자가 감당하고 못 하는
쪽은 어떻게 드러내는가"가 적혀 있다.

---

## 한 장 요약

```
 0 실측 ─► 1 Set ─► 2 단일챕터 ─► 3 개명 ─► 4 푸시·.meta·0.2.0
                                                 │
          ┌──────────────────────────────────────┘
          ▼
 5 배선·완료신호 ─► 6 드라이버 [H1] ─► 7 선택지뷰 [H2] ─► 8 세이브 [H3] ─► 9 연출 [H4]
          │
          └──► 10 툴 참조·진단 ─► 11 증명기 교체 [T3] ─► 12 걷기 [T5] ─► 13 X3·X2·X4 ─► 14 포팅
```

**뒤처질 때 버리는 순서**: 14 → 12 → 9 → 13 → 7의 잠김 표시(임시 UI로 버틴다).
**절대 안 버리는 것**: 1 · 4의 `.meta` 순서 · 6 · 8 · 11의 대조 · **런타임과 시트를 고치지 않는다는 전제.**
