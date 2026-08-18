# CHANGELOG

형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/), 버전은 [SemVer](https://semver.org/lang/ko/).

`0.x` 동안은 **공개 표면을 약속하지 않는다.** 타입이 굳으면 `1.0.0`을 붙인다.

## [Unreleased]

### 추가
- `ProgressionCondition` · `ConditionKind` — 조건 모델 (§G1·G2)
- `StatDefinition` · `StatType` — **§G7의 빈칸.** 초기값·경계의 유일한 집
- `ProgressionState` · `StatChange` — 불변 상태. 나중에 세이브가 담을 내용
- `ConditionEvaluator` — 조건 판정 (§G3·G4·G6)
- `EpisodeOption` — 간선 하나. **관문이 사는 자리** (§G5)
- `EpisodeNode` · `EpisodeKind` — 에피소드 하나. 선택지 배열 순서 = 화면 순서 (§G6)
- `ChapterProgression` — 챕터 전부. `Stats`를 최상위에 둔다 (§G7)
- `ConditionKind.ChapterCleared` — 챕터를 넘나드는 유일한 조건. 시나리오 층의 준비
- `OptionKind` — 간선이 플레이어 선택지인가 자동 진행인가
- `Dto/` — 저작 내보내기 JSON과 **필드 1:1**. 모델에 없는 칸도 남긴다(그래야 "왔는데 안
  쓴다"를 로더가 본다)
- `ProgressionLoader` — DTO → 모델. **진단을 전부 모아서** 낸다
- `ProgressionDiagnostic` · `ProgressionLoadResult` — 진단은 **자리를 짚는다**
  (`Nodes[ep_03].NextOptions[1].Conditions[0]`)
- `ChapterInvariants` — 챕터 불변식의 **유일한 구현.** 생성자는 첫 진단에서 던지고,
  로더는 전부 모은다. 사본이 생기면 한쪽만 고쳐지는 날이 온다
- `docs/architecture.md` — **타입의 정본.** 원칙 다섯과 실제 형태
- `docs/work-plan.md` — 순서·게이트·결정 대기
- `EndingRule` · `EndingOutcome` — **챕터에서 나가는 길.** 시나리오 층의 간선이다
- `ScenarioProgression` · `ScenarioInvariants` — 시나리오 하나. **스탯 정의의 주인**(D1)
- `ChapterTransition` · `ChapterAdvance` · `ResolvedOption` · `OptionVisibility` —
  **관문이 실제로 잠기는 자리.** 모델과 로더는 조건을 싣기만 하고 보지 않는다
- `ScenarioTransition` · `ScenarioAdvance` — 챕터가 끝난 뒤 무엇이 일어나는가
- `ProgressionState.CommitChapterEnding` · `ChapterEnding` · `EndingHistory` — 챕터 경계의 트랜잭션
- `ScenarioProgressionDto` · `EndingRuleDto` + `ProgressionLoader.Load(ScenarioProgressionDto)`
- `EpisodeOption.ViaNodeId` — **연출을 매다는 자리** (계약서 §H-3). 이 길을 지나며 거쳐 갈
  Yarn 노드의 이름이고, 에피소드 사이 트랜지션 연출과 엔딩 연출이 같은 칸을 쓴다
- `ChapterReachability` · `StatSpan` · `UnreachableCause` — **도달성 증명.** 저작 도구에서
  이관했고 등가성을 코퍼스로 고정했다
- `ProgressionSave` · `ProgressionSaveDto` · `ProgressionRestoreResult` — **유저 데이터.**
  굽고 되살린다. 어려운 것은 직렬화가 아니라 콘텐츠가 바뀐 뒤의 로드다
- `Tests/Fixtures/reachability-oracle.json` — 원본 증명기를 돌려 뽑은 등가성 코퍼스
- `Tests/Fixtures/chapter-ch01-sample.json` — 저작 쪽이 직접 만들어 보낸 표본
- `Tests/Fixtures/chapter-sample-export.json` — 툴이 실제로 낸 챕터 JSON
- `Tests/Fixtures/scenario-two-chapters.json` — 손으로 쓴 시나리오 (툴에 시나리오 저작이 없다)
- 계약 테스트 140개

### 변경 (호환 깨짐 — `0.x`라 지금이 가장 싸다)

원칙은 `docs/architecture.md` §1. 넷 다 같은 판단이다 —
**무효 조합을 예외로 잡고 있었다면, 타입으로 올릴 수 있는지 먼저 본다.**

- **`ProgressionCondition`의 공개 생성자를 없앴다.** `Stat` · `EpisodeCleared` ·
  `ChapterCleared` 팩토리만 연다. `EpisodeCleared + GreaterOrEqual`이 이제 **타이핑되지
  않는다** — 전에는 만들 수 있었고 평가기가 뒤에서 던졌다. 그래서 평가기에서 그 검사가
  사라졌고 안쪽이 그만큼 전체 함수가 됐다
- **`EpisodeOption`의 공개 생성자를 없앴다.** `Choice` · `Auto` 둘뿐이고 `Auto`는 문구·조건·
  잠금 인자를 **아예 받지 않는다.** "자동 진행에 관문이 달림"이 실행 시점 예외가 아니라
  컴파일 오류가 된다. 더 중요한 것은 `IsDefault => ChoiceLabel.Length == 0`이 사라진 것이다
  — 작가가 엑셀에서 선택지 문구를 실수로 지우면 **분기가 보이지 않는 자동 진행으로 조용히
  변신했다.** 검증은 통과하고 게임을 돌려 봐야 알았다
- **`EpisodeNode.IsChapterEndingCandidate`를 없앴다.** bool + `EndingKey`는 4조합 중 둘이
  무효인데(참인데 키가 빔 / 거짓인데 키가 있음) 아무도 막지 않았고 어느 쪽이 이기는지도
  없었다. `EndingKey` 하나로 판별한다. DTO에는 스키마 1:1로 남기고 로더가 불일치를 진단한다
- **`WithStatChanges` + `WithMovedTo` → `Commit(chapter, chosen)` 하나.** 따로 부를 수
  있으면 언젠가 따로 불리고, 그 순간 "스탯만 바뀌고 안 옮겨 간" 상태가 생긴다 — §3.3이
  막으려던 중복 가산이 정확히 그것이다. **"에피소드 = 트랜잭션 경계"가 문서가 아니라
  타입이 되는 자리.** 덤으로 `Commit`은 고른 길이 지금 노드에서 나가는 길인지 확인한다
- `ProgressionState.ClearedEpisodes` → `ClearedEpisodeIds`, `IsCleared` →
  `IsEpisodeCleared`. `CurrentChapterId` · `ClearedChapterIds` · `IsChapterCleared` ·
  `EndingHistory` 신설. `CreateInitial`이 챕터 ID를 함께 받는다

### 결정

- **세이브에서 조용해도 되는 것은 하나뿐이다** — 새로 생긴 스탯을 정의의 초기값으로
  채우는 것. 스탯이 느는 건 콘텐츠가 자라는 정상 경로이고, 그때 옛 세이브를 못 열게 하면
  개발이 멈춘다. 나머지는 전부 진단이 붙는다 — 버려진 스탯 · clamp된 값 · 사라진 클리어
  기록(그것을 보던 관문이 다시 잠긴다)
- **경고면 상태를 만들고 오류면 안 만든다.** 초안에는 "진단이 있어도 상태는 만든다"고
  적었는데, **지금 에피소드가 사라진 세이브로는 이어할 수가 없다** — 반쯤 되살린 진행으로
  시작하면 무엇이 어긋났는지 플레이해 봐야 안다. 값이 조정된 정도는 경고로 두고 상태를 낸다
- **임의의 상태를 손으로 만드는 길을 안 열었다.** 되살리기는 `ProgressionState.FromSave`를
  쓰는데 그것이 `internal`이다 — 공개하면 "그래프에 없는 자리에 있는 상태"가 만들어진다.
  정상 경로는 `CreateInitial`과 `Commit` 둘뿐이다
- **`Capture`가 시나리오를 객체로 받는다.** ID를 문자열로 받으면 엉뚱한 시나리오 이름이
  붙은 세이브가 조용히 만들어진다 — 객체에서 꺼내면 그럴 수가 없다 (D4)
- **증명을 옮기며 알고리즘을 개선하지 않았다.** 판정 기준이 "이관 전후로 결과가 같다"이므로
  더 나은 방법이 보여도 등가성이 먼저다. 등가성은 주장이 아니라 **코퍼스**로 남겼다 —
  원본 증명기를 그대로 돌려 케이스 일곱마다 내보낸 JSON과 그때의 결과를 한 벌로 저장했고,
  테스트가 도달 가능 집합·완전 탐색 여부·에피소드별 스탯 폭 셋을 대조한다
- **도달 불가 원인을 문장이 아니라 값으로 낸다** (`UnreachableCause` + `BlockingCondition`).
  저작 도구가 이미 사람이 읽을 문장을 만들고 있어 여기서 또 만들면 규약 사본이 된다 —
  `ResolvedOption.LockedReason`과 같은 판단이다
- **연출은 간선의 종류와 직교한다.** `ViaNodeId`는 선택지든 자동 진행이든 붙는다 —
  관문은 자동 진행에 뜻이 없어 인자를 뺐지만, 말없이 넘어가는 자리가 오히려 트랜지션
  연출의 주 무대다. 팀장이 요구한 "에피소드 사이 트랜지션"과 엔딩 연출이 **같은 기능의
  두 쓰임**이 된다
- **`ViaNodeId`에는 이름 하나만 들어간다.** 지속시간·이징·색 같은 파라미터가 붙기 시작하면
  그때가 경계면이 진짜로 넓어지는 순간이다 — 연출의 파라미터는 연출 쪽에서 산다.
  경계면이 셋(`DialogueEntryId` · `ViaNodeId` · `EndingKey`)이 됐지만 **전부 같은 종류**
  (호스트가 푸는 이름)라 층 구조는 그대로다
- **엔딩키는 노드에 그대로 둔다 — D2 유지.** 저작 쪽이 "엑셀 간선에 엔딩키를 적고
  exporter가 도착 에피소드 노드에 싣는다"를 제안했고 받아들였다. **저작 표면은 넉넉하게,
  계약 표면은 좁게** — 기획자는 간선 한 행에 종류·엔딩키·연출을 함께 적고, 계약은
  `ViaNodeId` 하나만 는다.
  ⚠ 대가: *"같은 엔딩 에피소드로 들어오는 간선들이 서로 다른 엔딩키를 가지면 거부"*를
  **저작 쪽만 볼 수 있다**(JSON에 오면 이미 키가 하나다). 검증 소유 경계의 유일한 예외다
- **`OptionVisibility`에 `Hidden`을 두지 않았다.** §G5는 표시조건 미달을 "목록에 만들지
  않는다"고 적는데, 그러면 숨긴 것은 목록에 **없는 것**이지 `Hidden`으로 표시된 항목이
  아니다. 목록에 넣어 두면 호스트가 전부 그리다 숨겨야 할 것을 보여 주는 사고가 열리고,
  그 값은 어차피 아무도 그리면 안 되므로 **영원히 안 타는 분기**가 된다(`NotEqual`을
  뺀 것과 같은 판단). 몇 개가 빠졌는지는 `ChapterAdvance.HiddenCount`가 진다
- **`ChapterAdvance.Options`는 `AwaitPlayerChoice`가 아니면 비어 있다.** 고를 수 있는
  것이 없을 때 잠긴 목록을 띄워 놓고 아무 일도 안 일어나는 것보다, 자동 진행으로
  넘어가는 편이 낫다(§G6-2). 무엇이 잠겼는지는 그때 화면의 관심사가 아니다
- **잠긴 사유를 지어내지 않는다.** `LockedReason`은 저작자가 쓴 문장 그대로이고 비어 있을
  수 있다. 대신 `BlockingCondition`이 **미달인 첫 조건**을 기계가 읽을 모양으로 진다 —
  툴의 증명기가 이미 원인 조건을 지목하므로 런타임이 따로 계산하면 규약 사본이 된다
- **D1 확정 — 스탯 정의의 주인은 시나리오다.** 챕터가 이어지는 순간 챕터의 초기값이 두 번
  의미를 갖는다(ch02에 들어갈 때 `trust`가 되돌아가면 "챕터를 넘나든다"가 거짓이 된다).
  실제 시작값은 시나리오가 한 번만 세우고, **챕터에 적힌 초기값은 도달성 증명의 진입
  가정으로 역할이 바뀐다.** 경계·타입은 갈리면 오류(증명과 실제 플레이가 갈린다),
  초기값은 갈려도 된다. 챕터가 스탯을 안 적으면 시나리오 것을 쓴다
- **D2 확정 — 엔딩은 노드의 `EndingKey`가 정한다.** `EndingRule`은 그 키로 조회되는 표이고,
  규칙의 조건은 판정이 아니라 같은 키에서 다음 챕터가 갈릴 때 갈래를 고른다.
  **`ScenarioTransition.Resolve`가 엔딩키를 인자로 받지 않는 것**이 그 결정의 구조적 표현이다 —
  지금 노드에서 읽으므로 호출자가 엉뚱한 키를 넘길 자리가 없다
- **`EndingRule`도 팩토리만 연다** — `To(다음 챕터)` / `Ends()`. `NextChapterId`가 비었는지로
  판별하면 "여기서 끝난다"와 "다음을 실수로 안 적었다"가 같은 모양이 된다.
  구 런타임의 `bool UnlockNextChapter` + `NextChapterId`를 안 가져온 이유이기도 하다
- **엔딩 규칙에 세 불변식.** 키를 내는 노드에 규칙이 있어야 하고 · 아무도 안 내는 키의
  규칙은 오류이며 · **같은 키의 마지막 규칙은 조건이 없어야 한다**(전부 미달일 때
  무슨 일이 일어나야 하는지 안 적으면 런타임이 추측하게 된다)
- **게이트 G1·G3이 열렸다.** 손으로 쓴 시나리오(챕터 3개)가 오류 0으로 실리고,
  ch01의 엔딩키가 스탯에 따라 다음 챕터를 갈라 내며, **스탯이 챕터를 넘어간다** —
  에피소드 레이어 마스터 플랜 §0-2가 참이 되는 지점
- **게이트 G2가 열렸다 (2026-08-18).** 툴 exporter가 `Stats`를 내기 시작했고
  (`java-start`, `Int`→`Number` 이름 번역 포함), **실제로 내보낸 JSON이 오류 0으로
  로드되어 시작에서 엔딩까지 걸어진다.** 그전까지 이 패키지는 실데이터를 한 번도
  실어 본 적이 없었다 — 스탯 관문이 있는 챕터는 생성 자체가 거부됐다
- **로더가 새로 쓰는 규칙은 "데이터의 모양"에 관한 것뿐이다.** 알 수 없는 enum 이름 ·
  언제나 비어야 할 칸이 안 비었을 때 · sentinel 쌍의 불일치. 챕터 전체의 불변식은
  `ChapterInvariants`가 소유하고 로더는 그것을 **모으는 방식으로** 쓴다
- **"왔는데 안 싣는 값"은 전부 오류다.** 노드에 실린 관문(v8 이전 데이터 — 그대로 실으면
  **에러 없이 관문이 전부 열린다**) · 부착(§G9 비범위) · 엔딩 규칙(시나리오 층 대기) ·
  자동 진행에 달린 잠금. 조용히 버리면 게임을 돌려 봐야 안다
- **자동 진행 경고는 간선마다 내지 않고 한 줄로 모은다.** 문구 빈 간선은 흔해서
  간선마다 경고하면 소음이 되고, 소음은 읽히지 않는다. 개수와 자리만 한 줄로 낸다 (D5)
- **⚠⚠ 위 정정에 다시 단서가 붙는다 (2026-08-18, 저작 쪽 지적으로 실측).**
  `ChapterEndingRule`이 있는 곳은 `test13`(08-12 정지)이고, 런타임 mainline `dev`에는
  **0파일**이다 — 진행·세이브·변수 저장소가 통째로 걷혔다. 그러니 *"런타임에 이미 모양이
  있다"*는 **살아 있는 계약이 아니라 한 번 그려졌던 설계**다.
  다행히 `EndingRule`은 받아 적은 것이 아니다 — `bool UnlockNextChapter`를 빼고
  `EndingOutcome` 두 갈래로 바꿨고, "같은 키의 마지막 규칙은 조건이 없어야 한다"는
  이쪽이 새로 세운 불변식이다. **시나리오 층에 챕터→챕터 간선이 필요하다는 근거만으로
  선다.** test13은 착상의 출처이지 정당성의 근거가 아니다
- **⚠ `EndingRules`에 대한 아래 판단은 전제가 틀렸다 (2026-08-18 정정, 지금은 구현됨).**
  *"데이터에 모양이 없다"*고 적었으나, 런타임 `test13`의 `ChapterEndingRule`에 온전한 모양이
  있었다 — `EndingKey · DisplayName · Conditions · UnlockNextChapter · NextChapterId`.
  **그것이 곧 시나리오 층의 간선이고**, 에피소드 층의 `NextOptions`와 같은 방식으로
  출발 노드가 자기 나가는 길을 소유한다. 툴이 안 낼 뿐이었다.
  *(1:1로 받아 적지는 않았다 — `bool UnlockNextChapter`는 위와 같은 sentinel 쌍이라 빼고
  `EndingOutcome` 두 갈래로 바꿨다.)*
- **정의되지 않은 스탯 키는 0이 아니라 예외다.** 조용히 0을 주면 오타 낸 조건이
  "언제나 통과하는 관문"으로 바뀌고, 그 버그는 재생해 봐도 안 보인다
  (코어의 `RectNodeTree.GetState("없는키")`와 같은 규율)
- **초기값이 경계 밖이면 거부한다.** 조용히 clamp하면 작가가 쓴 값과 다른 값으로 시작한다
- `EpisodeCleared`는 `Exists`만 받는다. 다른 연산은 예외 — 저작 출력 확인이 먼저다
- **`ChapterProgression` 생성자가 챕터 전체의 불변식을 강제한다.** 허공으로 가는 간선,
  정의되지 않은 스탯 키, bool 스탯의 크기 비교·증감(§G4)이 전부 여기서 걸린다.
  → 이 타입을 쥔 쪽(전이기·도달성 증명)이 "없는 도착"과 "없는 키"를 다시 걱정하지 않는다.
  로더(3-2)는 같은 규칙을 **진단으로 모아서** 내고, 생성자는 로더를 거치지 않은
  프로그래머 실수에 대한 마지막 방어선이 된다 (`StatDefinition`과 같은 배치)
- **`EndingRules`를 타입으로 만들지 않았다.** §G1에 이름은 있지만 모양이 없다 —
  내보내기가 `List<object>`를 언제나 비워서 낸다(v1 비범위, D5). 데이터에 없는 것을
  타입으로 먼저 만들면 영원히 안 타는 분기가 생긴다(`NotEqual`을 뺀 것과 같은 판단).
  엔딩 표시는 노드의 `IsChapterEndingCandidate`·`EndingKey`가 진다
- **노드에서 뺀 넷** — `VisibleConditions`·`UnlockConditions`(v8에서 간선으로 내려감) ·
  `IndexText`(v5 폐지) · `Position`(저작 레이아웃). 언제나 빈 값으로 나오는 칸을 모델에
  옮겨 오면 "여기에 조건을 달 수 있다"는 잘못된 여지가 생긴다. DTO에는 스키마 1:1로 남긴다
- **`cleared:` 대상 에피소드의 실재는 검사하지 않는다.** 오타는 "영원히 안 열리는 관문"이
  되는데(fail-closed), 그건 도달성 증명이 잡는 자리다. 저작 쪽 검증기도 여기를 보지 않는다

### 예정

순서와 게이트는 `docs/work-plan.md`. **게이트 G0~G5가 전부 닫혔다.**

- **`0.2.0` 태그** — 소비자가 가져갈 만해졌다. 다만 유니티가 물려면 `.meta`가 먼저다
- `.meta` 생성·커밋 — 한 번 임포트해 만든 뒤 가져와야 한다. **커밋된 GUID가 참조 안정성**이다
- **챕터 연쇄 증명** — ch01 출구 스팬 → ch02 진입 가정. 안 셋과 권고는 `work-plan.md` §9.
  콘텐츠가 실제로 둘 이상 이어진 뒤에 한다(지금은 검증할 실물이 없다)
- 저작 쪽(X2·X3·X7) — `EndingRules` 내보내기 · 간선 `종류` 열 · `ViaNodeId` 발행 경로
- 런타임(X5) — 진행 블록을 세이브에 싣기. **세이브 층 자체가 먼저 필요하다**
- bool 스탯을 무엇이 켜나 — 간선으로는 값이 절대 안 바뀐다(`handoff.md` §6-4)

## [0.1.0] - 2026-08-17

첫 껍데기. **코드가 아니라 배선이 이 릴리스의 내용이다.**

### 추가
- 저장소 골격 — UPM(`package.json` + asmdef)과 dotnet(`csproj`)이 **같은 `.cs`를 가리킨다**
- `ComparisonOp` — 비교 연산 6종 (§G3). `NotEqual`은 의도적으로 없다
- 계약 테스트 8개 — §G1(이름 문자열 왕복) · §G3(6종, NotEqual 부재)
- GitHub Actions — `dotnet test`. 유니티 없이 돈다

### 결정
- **`LangVersion 9.0` + `Nullable disable` + `ImplicitUsings disable`**
  → dotnet 빌드를 유니티 빌드의 대역으로 만든다. 이게 없으면 "dotnet 초록"이
  "유니티 초록"을 보장하지 못한다
- **빌드 산출물을 `artifacts/`로 몬다** (`Directory.Build.props`)
  → `Runtime/obj/`가 생기면 유니티가 그 안의 `.AssemblyInfo.cs`를 소스로 읽어 깨진다
