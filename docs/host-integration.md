# 호스트 연결 — 코어를 두 도구 사이에 실제로 끼우는 법

기준: 2026-08-21 · **이 문서가 없어서 코어가 "견본"처럼 보였다.** 지금까지의 문서는
*왜 이렇게 만들었나*를 말했고, *어떻게 모는가*는 `EpisodeFlowTests`에만 있었다.
이 문서는 그것만 말한다 — 유니티와 Avalonia가 같은 코어를 어떻게 쥐는가.

```
                 ┌──────────────────────────────┐
   VnTool ───────►│        Ked.Progression        │◄─────── ked-presentation-runtime
  (Avalonia)      │  netstandard2.1 · 의존 0      │           (Unity 6000)
                  │  Load · Flow · Save · Prove   │
                  └──────────────────────────────┘
   화살표는 둘뿐이고 둘 다 코어를 향한다. 코어는 양쪽을 모른다.
```

---

## 0. 이음매는 셋이다 — 양쪽이 같은 셋을 쓴다

| 이음매 | 코어가 주는 것 | 호스트가 하는 것 | 유니티 | Avalonia |
|---|---|---|---|---|
| **Load** | `ProgressionLoader.Load(dto)` → 모델 + 진단 | JSON → DTO 역직렬화 | `exported/*.progression.json`을 읽는다 | 자기 `ChapterGraphModel`을 DTO로 바꾼다 (exporter가 이미 그 모양을 만든다) |
| **Pump** | `EpisodeFlow.Pending` (값) | 요청을 제 방식대로 수행하고 완료를 알린다 | 대사·연출은 `EpisodePlayer.StartGameAsync` · 선택지는 UI · 세이브는 파일 | **전부 즉시 완료** — 미리보기 걷기 · 도달성 · "이 상태면 뭐가 보이나" |
| **Save** | `ProgressionSave.Capture/Restore` (DTO) | 파일 포맷·슬롯·썸네일 · 대사 블록과 합성 | 슬롯 세이브에 **진행 블록**으로 싣는다 | 쓰지 않는다 (또는 픽스처 생성용) |

**같은 코어를 두 호스트가 다르게 쓰는 것이 아니라, 같은 셋을 다른 속도로 쓴다.**
`PlayDialogue`가 유니티에서 3초 걸리든 Avalonia에서 0초에 끝나든 코어에겐 구별이 없다.

---

## 1. 펌프 규칙 — 호스트 쪽에서 지켜야 코어가 순수하다

코어의 순수성은 코어 안에서 보장되지만, **펌프의 단일성은 호스트가 지켜야 한다.**

1. **`EpisodeFlow`를 쥐는 객체는 호스트에 하나다.** 여러 MonoBehaviour가 각자 `Pending`을
   보면 같은 요청을 두 번 수행하거나 완료를 두 번 알린다. (두 번째 완료는 `Require`가 던진다
   — 그래서 사고가 조용하지는 않지만, 원인이 펌프 쪽이라는 걸 알아야 한다.)
2. **완료 통지는 요청 하나에 한 번.** `DialogueCompleted` / `Choose` / `ViaCompleted` /
   `SavePersisted` 넷은 각각 한 `Phase`에서만 유효하다. 다른 자리에서 부르면 던진다 — 호스트
   버그가 코어에서 즉시 드러나게 해 둔 것이다.
3. **`SavePersisted()`의 뜻은 "처리했다"지 "디스크에 썼다"가 아니다.** 오토세이브 정책,
   슬롯, 쓰기 실패 처리는 호스트 것이다. 쓰지 않기로 했어도 부른다 — 흐름을 잇는 신호다.
   ⚠ 단, 쓰기 실패를 삼키고 부르면 "세이브됐다고 믿는 플레이어"가 생긴다. 실패는
   호스트가 화면에 내야 한다(규율 1은 호스트에도 적용된다).
4. **요청을 수행하는 동안 `EpisodeFlow`를 건드리지 않는다.** 요청은 값이라 복사해 둬도 된다.
   대사 재생 중 롤백·백로그·스킵은 전부 호스트 안의 일이고 코어는 모른다.
5. **`Phase`·`Pending`은 저장하지 않는다.** 세이브에는 `State`만 간다(P3). 이어 하기는
   `Resume(scenario, restoredState)`이고, 그 결과는 언제나 `EpisodeEntered` — 즉 **그 에피소드의
   대사를 처음부터 틀어 달라는 요청**이다. 줄 단위 이어 하기는 §3.

---

## 2. 유니티 — `ProgressionDriver` 하나

### 2.1 자리

```
ked-presentation-runtime/
├─ Packages/manifest.json          "com.ked.progression": "<git url>#<tag>"   ← UPM
└─ Assets/Scripts/
   ├─ Ked.Presentation.Runtime/    (asmdef: references += Ked.Progression)
   └─ Game/
      ├─ EpisodePlayer.cs          ← 이미 있다. 경계. 건드리지 않는다
      └─ ProgressionDriver.cs      ← 새로. EpisodeFlow를 쥐는 유일한 객체
```

- `Ked.Progression.asmdef`는 `noEngineReferences: true`다. 유니티가 읽으려면 **`.meta`가
  커밋돼 있어야** 한다(UPM git 패키지는 유니티가 저장소에 `.meta`를 쓸 수 없다). 한 번
  임포트해 생성한 뒤 가져와 커밋한다 — **이것이 채택의 첫 물리적 작업이다.**
- JSON: `ProgressionSaveDto.Stats`가 `Dictionary<string,int>`라 `JsonUtility`로는 안 된다.
  **`com.unity.nuget.newtonsoft-json`**을 쓴다. DTO는 `{ get; set; }` POCO라 그대로 물린다.
  PascalCase 키는 Newtonsoft 기본 동작과 맞는다.

### 2.2 루프 — `FlowRequestKind` 다섯을 그대로 옮긴다

```csharp
// 의사 코드. async는 호스트의 것이다 — 코어는 모른다.
public sealed class ProgressionDriver
{
    private readonly EpisodePlayer _player;     // 대사·연출 재생 (이미 있음)
    private readonly IChapterOptionsView _options; // 에피소드 선택지 UI (새로, §2.3)
    private readonly ISaveStore _store;         // 슬롯 세이브 (새로, §3)
    private EpisodeFlow _flow;                  // ← 유일한 소유자

    public async Task RunAsync(ScenarioProgression scenario, ProgressionState state /* null = 새 게임 */)
    {
        _flow = state == null ? EpisodeFlow.Begin(scenario) : EpisodeFlow.Resume(scenario, state);

        while (!_flow.IsFinished)
        {
            FlowRequest req = _flow.Pending;              // 값. 복사해 둬도 된다
            switch (req.Kind)
            {
                case FlowRequestKind.PlayDialogue:
                    await _player.StartGameAsync(req.NodeName);   // 노드가 끝나면 Task가 끝난다
                    _flow.DialogueCompleted();                    // ← 여기서 이 회차의 판정이 얼어붙는다
                    break;

                case FlowRequestKind.PresentOptions:
                    int i = await _options.ShowAsync(req.Options, req.HiddenCount);
                    _flow.Choose(i);
                    break;

                case FlowRequestKind.PlayVia:
                    await _player.StartGameAsync(req.NodeName);   // 연출도 Story 노드다 — 같은 경계
                    _flow.ViaCompleted();
                    break;

                case FlowRequestKind.PersistSave:
                    await _store.WriteAsync(slot, Compose(req.Save, _player.CaptureDialogueBlock()));
                    _flow.SavePersisted();
                    break;
            }
        }

        ShowEnding(_flow.Pending.Outcome);   // ScenarioEnded(의도한 끝) vs DeadEnd(미완성) — 섞지 않는다
    }
}
```

**`DialogueCompleted`의 근거가 `StartGameAsync`의 Task 완료 하나뿐이다.** 지금 런타임의
호출자는 전부 `async void`라 그 완료를 아무도 안 본다 — 드라이버가 첫 번째 소비자다.
`DialogueRunner.onNodeComplete`/`onDialogueComplete`는 씬에 슬롯만 있고 리스너가 없다.
**Task 완료로 충분하다** — 노드가 `<<jump>>`로 다른 노드에 가도 그 체인이 끝나야 Task가 끝난다.

> ⚠ `StartGameAsync`는 *어떻게* 끝났는지를 돌려주지 않는다(취소·오류·정상이 같은 모양).
> **런타임은 굳었으므로 시그니처를 바꾸지 않는다** (소유자 결정 2026-08-21). 대사를 멈출 수 있는
> 것은 드라이버가 부르는 `StopDialogueAsync`뿐이므로, 드라이버가 *자기가 멈췄는지*를 플래그로 들고
> `await` 뒤에 본다. 예외는 드라이버가 잡는다. — **U-1**은 런타임이 아니라 드라이버 안의 일이다.

### 2.3 선택지 UI — Yarn 선택지가 아니다

런타임의 `VNOptionsPresenter`는 **Yarn `->` 옵션**을 그린다. 에피소드 선택지(`ResolvedOption`)는
다른 것이다 — 잠긴 채 보이고(`Locked` + `LockedReason`), 순서가 배열 순서이며, 고르면 스탯이
커밋된다. **같은 프리팹을 재사용해도 되지만 같은 프레젠터는 아니다.**

드라이버가 넘기는 것: `ResolvedOption[]` 그대로. 화면은 `IsSelectable`·`LockedReason`만 본다.
`BlockingCondition`은 로그·디버그 오버레이용이다 — 플레이어에게 조건식을 보여 주지 않는다.

### 2.4 단일 챕터부터 — 시나리오 저작이 없어도 돈다

툴은 아직 챕터 JSON만 낸다(시나리오 저작 X4 미착수). 드라이버가 `ScenarioProgression`을
요구하므로, **단일 챕터를 시나리오로 감싸는 길**이 필요하다. 둘 중 하나:

| 안 | 어디 | 비고 |
|---|---|---|
| (가) 손으로 쓴 `scenario.json` (챕터 하나, 규칙 0개) | 게임 프로젝트 | 지금 픽스처와 같은 모양. 코드 변경 0 |
| (나) `ProgressionLoader.LoadAsSingleChapterScenario(chapterDto)` | **코어** | 챕터 `Stats`를 시나리오 것으로 승격. 타입이 "단일 챕터 플레이"를 표현한다 |

**(나)를 권한다.** 단일 챕터 플레이는 콘텐츠가 하나일 때만이 아니라 **챕터를 떼어 테스트
플레이할 때** 영원히 필요하다(작가가 ch03만 돌려 보고 싶다). 코어에 열 줄이고, 호스트 둘이
각자 감싸는 코드를 만들지 않게 된다. work-plan **H0**.

---

## 3. 세이브 합성 — 블록 둘, 주인 둘

```json
{
  "Progression": { "SchemaVersion": 1, "ScenarioId": "...", "CurrentChapterId": "ch01",
                   "CurrentEpisodeId": "믿는길", "Stats": { "trust": 2 }, ... },   ← 코어가 굽는다
  "Dialogue":    { "nodeName": "Story_믿는길", "lineId": "ln_014", "variables": {...},
                   "stageState": {...} }                                          ← 런타임이 굽는다
}
```

| | 주인 | 언제 굽나 |
|---|---|---|
| `Progression` | 코어 (`ProgressionSave.Capture`) | `PersistSave` 요청 때 — 즉 **에피소드 경계와 챕터 경계** |
| `Dialogue` | 런타임 (`SCOPE-BOUNDARY.md` §3.2의 `{nodeName, lineId, 변수, StageState}`) | 호스트 정책 — 줄마다, 또는 수동 세이브 때 |

**로드 순서**: `Progression` → `ProgressionSave.Restore` → 오류면 거부(사라진 에피소드 등) →
`EpisodeFlow.Resume` → `Pending == PlayDialogue(DialogueEntryId)` → 호스트가 **`Dialogue.lineId`로
그 노드 안을 시킹**한다. 코어는 "그 에피소드를 틀어라"까지만 말하고, 어디서부터인지는 대사
층의 것이다. 두 블록이 가리키는 노드가 다르면(`DialogueEntryId` ≠ `nodeName`) 그것은 합성
오류이고 호스트가 진단한다 — 코어가 볼 수 없는 자리다.

⚠ 줄 단위 시킹이 돌아오면 계약서 §C1(`#line:` 태그)과 §C3(선택지 리플레이가 위치 기반)이
되살아난다. 런타임이 `VNSeekKind.Load`를 생산자 없이 남겨 둔 자리가 그것이다.

**앨범(전역 진행)은 이 파일이 아니다.** "무엇을 본 적 있나"(엔딩·CG·읽은 줄)는 판을 넘는
전역 저장에 산다. 코어에 아직 자리가 없고, **전역 조건(`EndingSeen`)을 챕터 안 간선에
허용하지 않는다**는 규칙만 양쪽이 미리 합의했다 — 시나리오 층 간선에만 둔다. 그래야 증명이
(한 판 × 전역 이력)으로 곱해지지 않는다.

---

## 4. Avalonia — 코어를 "판정기"로 쥔다

**엑셀 시트의 형태는 굳었다 — 존중한다** (소유자 결정 2026-08-21). 에피소드 시트(대본 키·엔딩키·종류)와
간선 시트(문구·관문·스탯변화·연출)의 역할 분담은 코어 타입이 이미 그대로다(노드=엔딩키의 주인,
간선=조건·효과·연출의 주인). 시트를 바꾸는 요청은 하지 않는다 — JSON에 실리는 칸과 새 판(시나리오)만 더한다.

VnTool은 이미 같은 도메인을 자기 타입으로 들고 있다(`ChapterGraphModel` · `ChapterReachabilityProver`
545줄 · `ChapterValidator`). 채택은 **교체**이지 추가가 아니다.

### 4.1 자리

`src/Vn.Authoring/Vn.Authoring.csproj`에 `ProjectReference` 하나 — `Ked.Presentation.Core`가
이미 그 자리에 같은 방식으로 있다. (장기적으로 NuGet/패키지이지만 로컬 개발 중엔 충분하다.)

### 4.2 순서 — 오라클이 있으니 안전하다

| | 작업 | 판정 |
|---|---|---|
| T1 | `ChapterGraphModel → ChapterProgressionDto` 어댑터. **exporter가 이미 만드는 JSON 모양 그대로**이므로 exporter의 직렬화 직전 객체를 DTO로 바꾸면 된다 | `ProgressionLoader.Load`가 모든 견본 워크북에 진단 0 |
| T2 | `ChapterValidator`가 코어 진단을 **덧붙여** 본다 (자기 검사는 유지) | 두 검증기가 같은 파일에 같은 답 |
| T3 | `ChapterReachabilityProver.Prove` 호출부 → `ChapterReachability.Prove`. **옛 증명기를 한동안 나란히 돌려 결과를 대조**한다 (코퍼스 7케이스 + 모든 견본) | 차이 0 |
| T4 | 옛 증명기 삭제. 코어의 `reachability-oracle.json`이 더는 "옛 답"이 아니라 **유일한 답**이 된다 | — |
| T5 | **미리보기 플레이** — `EpisodeFlow`를 즉시 완료 호스트로 몬다. `PlayDialogue`→완료, `PresentOptions`→작가가 클릭, `PlayVia`→완료, `PersistSave`→무시. 작가가 챕터 그래프 위에서 "이 길로 가면 뭐가 열리나"를 걷는다 | 유니티와 같은 전이기가 같은 답 |

**T5가 "코어를 공유한다"의 실체다.** 도달성 증명은 *모든* 길을 말하고, 미리보기 플레이는
*이 길*을 말한다. 둘 다 같은 `ChapterTransition.Resolve`를 부른다.

### 4.3 저작 쪽이 계속 소유하는 것

- 엔딩키 충돌 거부(같은 에피소드로 들어오는 간선의 엔딩키가 다름) — JSON에 오면 이미 키가
  하나라 코어가 볼 수 없다. **검증 소유 경계의 유일한 예외.**
- 깃발 `Set`이 실린 챕터의 내보내기 거부 — 코어에 `StatChangeDto.Op`가 설 때까지.
- 레이아웃(`Position`) · `IndexText` · 시트 구조 전부.

---

## 5. 하지 말 것

| | 왜 |
|---|---|
| `EpisodeFlow`를 MonoBehaviour로 만들거나 `[SerializeField]`로 드는 것 | Domain Reload·직렬화가 코어 상태를 건드리기 시작한다. 드라이버가 **쥔다**, 유니티가 **보지 않는다** |
| Yarn `<<set>>`으로 A계층 스탯을 바꾸는 것 | 스탯 변이의 주인은 간선 하나다. 대본이 바꾸면 도달성 증명이 그 변화를 못 본다 |
| `FlowRequest`에 지속시간·이징·화자를 싣는 것 | 경계면이 넓어지는 순간. 연출 파라미터는 연출 쪽(Yarn 노드 안)에 산다 |
| 호스트가 `ProgressionState`를 손으로 만드는 것 | 생성 경로는 `CreateInitial`·`Commit`·`Restore` 셋뿐. `FromSave`가 `internal`인 이유 |
| 코어에 `Debug.Log` 하나 | `noEngineReferences: true`가 컴파일 오류로 막는다. csproj도 같이 깨진다. **그게 의도다** |
| `chapter.CreateInitialState()`로 플레이를 시작하는 것 | 그것은 **증명 진입 가정**이다. 플레이는 `scenario.CreateInitialState()`. (이름이 같아 헷갈린다 — work-plan C4에서 이름을 가른다) |

---

## 6. 유니티 쪽 작업 목록 (런타임 저장소에 **더해지는** 것 — 기존 코드는 고치지 않는다)

| | 무엇 | 막는 것 |
|---|---|---|
| **U-0** | `com.ked.progression` UPM 참조 + asmdef 참조 + Newtonsoft. **런타임 `.cs` 변경 0** | 코어 `.meta` (X6) |
| **U-1** | 완료 신호 = `StartGameAsync`의 Task 완료. 끊김/예외는 드라이버가 스스로 구별 (시그니처 불변) | 없음 |
| **U-2** | `ProgressionDriver` — §2.2 루프. `Game/`에 한 파일 | U-0·U-1 |
| **U-3** | 에피소드 선택지 뷰 `IChapterOptionsView` — `ResolvedOption` 그대로 받는다 | U-2 |
| **U-4** | `ISaveStore` + 블록 합성 (§3). 진행 블록 먼저, 대사 블록은 `nodeName`만으로 시작해도 된다(줄 시킹은 뒤에) | U-2 |
| **U-5** | 타이틀 → 새 게임 / 이어 하기 → 드라이버 진입. 디버그 키 `2`의 `_yarnEntryKey` 직접 재생은 남긴다 (대사 층 단독 테스트용) | U-2 |

**판정 (게이트 H1~H3)**: work-plan.md §4.
