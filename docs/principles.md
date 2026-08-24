# 원칙 — 이 저장소의 규칙 전부, 한 장

기준: 2026-08-21 · **코드 주석의 태그(P·D·규율·§G)는 전부 여기서 정의된다.**
태그가 여기 없으면 태그를 지우거나 여기에 더한다 — 정의 없는 태그는 6개월 뒤 암호가 된다.

> 한 줄: **"결정과 실행을 가르고, 결정 쪽에서 시간과 세계를 추방한다."**
> 이 패키지는 내러티브 진행이라는 도메인의 **sans-IO 결정 코어**다. 유니티도 파일도
> 시계도 모른다. 대사·연출·저장은 전부 **이름과 값**으로만 건너가고, 호스트가 바깥에서
> 펌프를 돌린다. 그래서 런타임과 저작 도구가 **문자 그대로 같은 static 함수**로 판정한다.

---

## 0. 메타 규칙 — 모든 것에 주인이 하나다

| 무엇 | 주인 | 다른 곳에서 하면 |
|---|---|---|
| 스탯 변이 | **간선** (`EpisodeOption.StatChanges`) | Yarn `<<set>>`으로 바꾸면 도달성 증명이 못 본다 |
| 엔딩키 | **노드** (`EpisodeNode.EndingKey`) | 규칙이 엔딩을 판정하면 두 곳이 어긋난다 (D2) |
| 스탯 정의·초기값 | **시나리오** (`ScenarioProgression.Stats`) | 챕터가 다시 세우면 "스탯이 챕터를 넘는다"가 거짓 (D1) |
| 파일 IO · 직렬화 | **호스트** | DTO에 어트리뷰트가 붙는 순간 직렬화기가 코어에 들어온다 (규율 2) |
| 검증 | **경계** (생성자 · 로더 · Restore) | 안쪽 방어 코드는 경계가 샜다는 신호 (P2) |
| 시간 | **호스트** | 코어가 기다리기 시작하면 스레드·생명주기 문제가 코어에 생긴다 |
| 펌프 (`EpisodeFlow`를 모는 것) | **호스트의 단 한 곳** | 여러 곳이 `Pending`을 보면 순수성이 바깥에서 무너진다 |

버그 추적이 빠른 코드베이스의 공통점이 이것이다. 고칠 자리가 언제나 하나다.

---

## 1. 규율 — `Ked.Presentation.Core`에서 물려받은 넷

| | 규율 | 이 저장소에서의 모양 |
|---|---|---|
| **규율 1** | **침묵 금지.** 모르는 것을 조용히 기본값으로 떨어뜨리지 않는다 | 없는 스탯 키 → 예외. 알 수 없는 enum 이름 → 진단. 초기값이 경계 밖 → 생성 거부 |
| **규율 2** | **의존 0.** 역직렬화는 호스트가 한다 | `Dto/`는 필드 1:1의 POCO. 어트리뷰트도 직렬화기도 없다. `using`은 `System`·`System.Collections.Generic`뿐 |
| **규율 3** | **게임 데이터는 인자.** 코드에 게임 어휘가 없다 | 스탯 카탈로그·챕터·시나리오가 전부 입력. `trust`라는 낱말이 코드에 없다 |
| **규율 4** | **순수 함수.** 바꾸지 않고 새 상태를 돌려준다 | `ProgressionState`는 불변. 판정은 `static`. `DateTime`·`Random`·가변 static 없음 |

---

## 2. P 계열 — 런타임 원칙 다섯

### P1 — 불가능한 상태를 만들 수 없게 한다

없음이 null도 빈 문자열도 아닌 **타입 안의 한 항목**이다. 실제 형태 셋:

- 생성자는 private, 팩토리만 연다 — `ProgressionCondition.Stat/EpisodeCleared/ChapterCleared`, `EpisodeOption.Choice/Auto`, `EndingRule.To/Ends`
- sentinel 쌍을 없앤다 — `bool IsChapterEndingCandidate + string EndingKey`는 4조합 중 2개가 무효. 키 하나로 합쳤다
- 함께여야 하는 연산은 함께 노출한다 — `WithStatChanges` + `WithMovedTo`를 `Commit` 하나로. 따로 부를 수 있으면 언젠가 따로 불린다

**우선순위: 타입이 막는 것 > 생성자가 막는 것 > 로더가 모으는 것.** 위로 올릴 수 있으면 올린다.

### P2 — 검증은 경계에서 한 번, 안쪽은 전체 함수

평가기·전이기에 `default: return false`가 없다. 모르는 것은 **로드 시점에** 죽는다.
안쪽 함수에 null 체크를 넣고 싶어지면 **코드를 고치지 말고 멈춰라** — 경계가 샜다는 신호다.

경계 셋: 생성자(프로그래머 실수의 마지막 방어선) · `ProgressionLoader`(데이터의 잘못) ·
`ProgressionSave.Restore`(어제 콘텐츠로 저장된 세이브).

### P3 — 스펙 / 진행 / 해석을 섞지 않는다

| | 예 | 수명 | 저장 |
|---|---|---|---|
| **스펙** | `ScenarioProgression` · `ChapterProgression` · `EpisodeNode` | 콘텐츠와 같다 | 콘텐츠로 |
| **진행** | `ProgressionState` | 플레이어의 것 | **세이브** |
| **해석** | `ChapterAdvance` · `ResolvedOption` · `FlowRequest` · `EpisodePhase` · `ReachabilityResult` | 이 순간뿐 | **안 한다 — 재계산한다** |

구조적 표현: 해석 타입에는 `[Serializable]`을 붙이지 않는다. `ProgressionSaveDto`에
잠김·표시·도달 가능 집합·`Phase`·`Pending`이 없는 이유다.

### P4 — 판정은 한 번, 뒤로는 재판정하지 않는다

`ChapterTransition.Resolve`가 선택지 목록을 **값으로 고정**하고, `EpisodeFlow.Choose`는
그 목록에서 고르기만 한다. 플레이어가 10분을 고민해도 상태는 불변이다. 화면에 뜬 것과
실제로 일어나는 일이 갈릴 틈이 없다. 같은 이유로 조건 판정은 **커밋 전 값**으로 한다(§G6).

### P5 — 조용히 버리지 않는다

- 진단은 **자리를 짚는다** — `Chapters[ch01].Nodes[ep03].NextOptions[1].Conditions[0]`
- 진단은 **전부 모아서 한 번에** 낸다 — 첫 오류에서 멈추면 작가가 왕복을 여러 번 한다
- 잠긴 이유는 **원인 조건을 지목**한다 (`ResolvedOption.BlockingCondition`) — 문장을 지어내지 않는다
- "왔는데 안 싣는 값"은 전부 오류다 — 노드에 실린 관문, 자동 진행에 달린 잠금

⚠ 단서 — 로더는 2단계다(DTO→모델, 그 다음 참조·불변식). "전부 모은다"는 **단계 안에서만**
참이다. 파싱 오류가 하나라도 있으면 그 회차에 참조 진단은 나오지 않는다.

---

## 3. D 계열 — 데이터·저작 결정 다섯

| | 결정 | 답 | 근거 |
|---|---|---|---|
| **D1** ✅ | 스탯 정의의 주인 | **시나리오.** 실제 시작값은 시나리오가 한 번만 세운다. 챕터의 `초기값`은 **도달성 증명의 진입 가정**으로 역할이 바뀐다. 경계·타입은 갈리면 오류, 초기값은 갈려도 된다 | 챕터를 넘어도 스탯이 되돌아가지 않아야 "넘나든다"가 참 |
| **D2** ✅ | 엔딩을 무엇이 정하나 | **노드의 `EndingKey`.** `EndingRule`은 그 키로 조회되는 표. 규칙의 조건은 판정이 아니라 같은 키에서 다음 챕터가 갈릴 때 갈래를 고른다. `ScenarioTransition.Resolve`가 엔딩키를 인자로 받지 않는 것이 구조적 표현 | 두 곳에서 엔딩을 정하면 어긋난다 |
| **D3** ✅ | `Tokens` · `Flags` 조건 종류 | **둘 다 안 넣는다.** 깃발은 `Stat` 0/1 + `Equal`로 통일. 아이템·열쇠는 작가 계층의 Yarn 변수라 진행 JSON에 안 나온다 | 두 계층을 섞지 않는다 |
| **D4** ✅ | 세이브 스키마 버전 | **처음부터 넣었다**(`SchemaVersion = 1`). 필드가 사라지거나 뜻이 바뀔 때만 올린다 — 추가는 안 올린다(없는 값은 정의의 초기값이 메운다). 더 높은 버전은 **로드 거부** | 나중에 넣으면 버전 없는 세이브를 영원히 특별 취급한다 |
| **D5** ⚠ | 저작 `간선` 시트의 `종류` 열 | **권고: 둔다.** 없으면 "문구를 실수로 지운 것"과 의도한 자동 진행을 데이터로 구별할 수 없다. 저작 쪽은 v11에서 `종류` 열을 만들었고, JSON에는 아직 `ChoiceLabel == ""`이 자동 진행이다. 그때까지 로더가 자동 진행 간선을 **경고 한 줄로** 모아 보고한다 | sentinel은 사고를 연다 |

---

## 4. §G / §F / §H — 계약서 조항이 어느 타입으로 갔나

계약 원본: `java-start/docs/runtime-contract.md` 2부·3부 (v9, 2026-08-18 전면 개정).
**충돌하면 계약서가 이긴다.** 다만 계약서가 "모양이 없다"고 적은 것 중 이쪽이 먼저
세운 것이 있다(`EndingRules`) — 그때는 이쪽 DTO가 규격이고 툴이 뒤따른다.

| 조항 | 내용 | 이 저장소에서 |
|---|---|---|
| §G1 / F | PascalCase · enum은 이름 문자열 | `Dto/` 전체 + `ProgressionLoader`의 이름 변환 (알 수 없는 이름 → 진단) |
| §G2 / F1 | `IntValue`가 0이면 키 자체가 없다 | `ConditionDto.IntValue`는 **`int`** — `int?`면 `flag == false`가 통째로 어긋난다 |
| §G3 / F2 | 비교 연산 6종, `NotEqual` 없음 | `ComparisonOp` — 저작 파서가 닫아 두어 데이터로 안 나온다. 넣으면 영원히 안 타는 분기 |
| §G4 / F3 | bool 스탯은 0/1 + `Equal`뿐. 크기 비교·증감 금지 | `StatType.Bool` + `ChapterInvariants` (로더가 앞당겨 진단) |
| §G5 / F4 | 관문은 간선의 것. `VisibleConditions`(미달이면 목록에 없음) / `Conditions`(잠긴 채 보임) | `EpisodeOption` + `OptionVisibility { Shown, Locked }` — `Hidden`은 **목록에 없는 것**이지 값이 아니다. 개수는 `ChapterAdvance.HiddenCount` |
| §G6 / F5 | 전이 3갈래 ① 고른 간선 커밋 ② 문구 없는 자동 간선(에피소드당 하나, 관문 금지) ③ 챕터 런 종료. **판정은 커밋 전 값** | `ChapterAdvanceKind` + `ChapterTransition.Resolve`. `EpisodeOption.Auto`는 관문 인자를 **받지 않는다** |
| §G6-1 | `StatChanges`는 원자적 1회 커밋 + clamp | `ProgressionState.Commit` — 스탯·클리어·이동이 한 연산 |
| §G7 / G-1 ✅ | 스탯 경계 — 툴만 알던 빈칸 | `StatDefinition` (이 패키지가 소유). 툴 exporter가 `Stats[]`를 낸다(2026-08-18) |
| §G8 / H-3 ✅ | `Option.ViaNodeId` — 연출을 매다는 자리 | `EpisodeOption.ViaNodeId` · `FlowRequestKind.PlayVia`. **이름 하나만** 들어간다 — 파라미터가 붙는 순간 경계면이 넓어진다 |
| §G9 / H-5 | Attachment 표시 조건 | **v1 비범위.** 부착을 쓸 때 `EpisodeNode`의 표시 조건을 부착에 한해 되살린다 |
| F6 | 생성자를 통과했다는 것의 뜻 | `ChapterInvariants` 8개 · `ScenarioInvariants` 4개 (architecture.md §2.3·2.4) |
| F7 | 침묵 금지 | 규율 1 |
| H-4 | `EndingRules` 모양 | **이쪽이 규격이다.** `EndingRuleDto.Outcome`은 `"NextChapter"`/`"ScenarioEnd"` 명시 문자열. 툴이 시나리오 저작 때 맞춘다 |
| G-6 ⛔ | 깃발을 켜는 `StatChange` 지정(Set) | **이쪽 미구현.** `StatChangeDto.Op`("Add" 기본 / "Set", bool에만) — 저작 쪽은 끝났고 그때까지 깃발 쓰는 챕터는 내보내기가 거부된다. work-plan §C1 |
| H-7 | 깃발의 수명 (챕터를 넘으면?) | **미결.** D1과 같은 덩어리 — 스탯이 넘어가므로 깃발도 넘어가는 것이 일관된다. 되돌리려면 간선의 `Set false`로 명시 |

---

## 5. 경계면 — 열거 가능하다는 것이 증거다

코어가 바깥에 부탁하는 것은 `FlowRequestKind` **다섯**이 전부다.

| 요청 | 건너가는 것 | 호스트가 마쳤다고 알리는 법 |
|---|---|---|
| `PlayDialogue` | 이름 (`DialogueEntryId`) | `EpisodeFlow.DialogueCompleted()` |
| `PresentOptions` | `ResolvedOption[]` + `HiddenCount` | `Choose(index)` |
| `PlayVia` | 이름 (`ViaNodeId`) | `ViaCompleted()` |
| `PersistSave` | `ProgressionSaveDto` (값) | `SavePersisted()` |
| `Finished` | `ScenarioAdvance` (의도한 끝 / 막다른 곳) | — |

경계를 건너는 것은 언제나 **이름과 값**이지 동작이 아니다. 콜백·이벤트·`async`가 0개라
"어느 스레드에서 불리나"·"구독 해제 타이밍"·"동기화 컨텍스트"가 코어에 존재하지 않는다.

> `FlowRequestKind`에 여섯 번째를 더하는 것은 경계면 확장이다. **혼자 결정하지 않는다.**

---

## 6. 성장 규칙 — 표현력을 넓힐 때

조건 언어가 `(Kind, Key, Op, 상수)`의 평면 AND인 것은 실력 부족이 아니라, 그래야
`ChapterReachability`가 "어떤 경로로도 도달 불가"를 **단언**할 수 있기 때문이다.
과대근사(min/max 축별 독립)라 "도달 불가"라고 말하면 진짜 도달 불가다 — **거짓 경보 없음.**

🔒 **불변식: 무엇을 더하든 "거짓 경보 없음"은 깨지 않는다.**

넓힐 때는 반드시 이 표를 채운다. 답 못 하는 확장은 보류:

| 확장 | 런타임 평가기 | 로더·불변식 | 도달성 분석 | 분석이 못 하는 것을 어떻게 드러내나 |
|---|---|---|---|---|
| (예) `NotExists` | case 하나 | 검사 없음 | bool 도메인이라 구간 전파 무사 | — |
| (예) OR | 실행 가능 | 정합성 조금 | **깨진다** — 우선 간선 복제로 버틴다 | 도입 시 해당 간선 `Unknown` + 커버리지 % 노출 |
| (예) 스탯 간 비교 · 난수 | 가능 (난수는 시드를 상태에, PRNG는 코어 안에) | — | **불가** | `Unknown` |

상세: work-plan.md §E.
