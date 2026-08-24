# Ked.Progression

챕터·에피소드 **진행 규칙**. 저작 도구(VNStoryEditor_avalonia)와 재생 런타임
(ked-presentation-runtime)이 **같은 구현을 공유한다.**

엔진 의존 0 · JSON 파서 없음 · `netstandard2.1`.

---

## 왜 있는가

이 규칙은 지금 양쪽에 **각자 구현**돼 있거나 한쪽에만 있다. 구현이 둘이면 갈린다 —
`runtime-contract.md` §G7이 그 위험을 이미 적어 두었다:

> 툴의 도달성 증명은 `Math.Clamp(값, 최소, 최대)`로 걷는데
> **런타임이 다른 경계로 clamp하면 증명과 실제 플레이가 갈린다.**

**구현을 하나로 만들면 갈릴 수가 없다.** 그리고 계약이 마크다운 문장에서
**컴파일러가 지키는 타입**으로 내려온다.

---

## 하는 일 / 안 하는 일

| 한다 | 안 한다 |
|---|---|
| 진행 모델 (챕터·에피소드·선택지·조건·스탯) | Yarn 재생 — `DialogueEntryId`는 문자열일 뿐이다 |
| 조건 평가 · 선택지 가시성 · 전이 판정 · 에피소드 트랜잭션(`EpisodeFlow`) | 그래프 편집·레이아웃 — 저작 도구의 일 |
| 스탯 정의(초기값·경계)와 clamp | 게임별 스탯의 **의미** — `game.definition.json`이 공급 |
| 도달성 증명 · 세이브 블록(굽기·되살리기) | 세이브 **파일**·슬롯·썸네일 — 호스트의 일 |
| 로드 시 검증과 진단 | JSON 파싱 — 호스트가 한다 |

---

## 현재 상태 — `0.1.0` 태그 이후, 미태그 (2026-08-21)

**코어는 완성 상태다.** 시나리오 → 챕터 → 에피소드 세 층, 전이·세이브·도달성 증명, 그리고
호스트가 모는 `EpisodeFlow`까지. 저작 도구가 낸 실제 챕터 JSON이 오류 0으로 실리고, 세이브를
굽고 되살리며, 도달성 증명이 저작 도구와 같은 답을 낸다는 것을 코퍼스로 고정했다.

**아직 아무 호스트도 이 코어를 몰아 보지 않았다.** 유니티도 VnTool도 참조 0건. 그것이 2기의
일이고, 코어가 바뀌는 것은 `StatChange` 지정(Set) 한 칸과 단일 챕터 시나리오 헬퍼뿐이다.

### 코어가 바깥에 부탁하는 것 — 이것이 전부다

| `FlowRequestKind` | 건너가는 것 | 호스트가 마쳤다고 알리는 법 |
|---|---|---|
| `PlayDialogue` | 이름 (`DialogueEntryId`) | `DialogueCompleted()` |
| `PresentOptions` | `ResolvedOption[]` | `Choose(index)` |
| `PlayVia` | 이름 (`ViaNodeId`) | `ViaCompleted()` |
| `PersistSave` | `ProgressionSaveDto` | `SavePersisted()` |
| `Finished` | `ScenarioAdvance` | — |

코어는 아무것도 부르지 않는다. 인터페이스·콜백·이벤트·async가 0개다. 값을 내놓고 멈추면
호스트가 제 방식으로 처리하고 완료를 알린다. 그래서 유니티의 프레임 루프와 Avalonia의
디스패처가 완전히 다른 시간 모델인데도 둘 다에 붙는다.

| 먼저 볼 것 | |
|---|---|
| [`docs/principles.md`](docs/principles.md) | **규칙의 정본** — P·D·규율·§G 대응, 경계면, 성장 규칙 |
| [`docs/architecture.md`](docs/architecture.md) | **타입의 정본** — 실제 형태 |
| [`docs/host-integration.md`](docs/host-integration.md) | **호스트가 코어를 쥐는 법** — 유니티 드라이버, 세이브 합성, Avalonia 채택 |
| [`docs/work-plan.md`](docs/work-plan.md) | 2기 순서 · 게이트 |
| [`docs/handoff.md`](docs/handoff.md) | 지금 무엇이 참인가 · 함정 |
| `Tests/ArchitectureWalkthroughTests.cs` · `Tests/EpisodeFlowTests.cs` | 흐름 전체와 펌프의 모양이 한 화면 |

`0.x` 동안은 공개 표면을 약속하지 않는다. 자유롭게 깨도 되는 구간이다.

---

## 쓰는 법

### Unity (UPM)

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ked.progression": "https://github.com/123456789qwaszx/ked-progression.git#0.1.0"
  }
}
```

끝의 `#0.1.0`이 **태그**다. 이게 버전 고정이고, "소스 복사"와 "패키지"의 차이 전부다.
태그를 빼고 브랜치를 가리키면 남의 커밋이 내 빌드를 깬다.

### .NET (Avalonia 도구)

```xml
<ProjectReference Include="..\ked-progression\Runtime\Ked.Progression.csproj" />
```

나중에 NuGet으로 옮길 수 있지만, 로컬 개발 중에는 프로젝트 참조로 충분하다.

---

## 개발

```bash
dotnet test Tests/Ked.Progression.Tests.csproj
```

**유니티 없이 돈다.** 엔진 의존이 0이기 때문이다.

### ⚠ dotnet 빌드는 유니티 빌드의 대역이다

두 곳에서 같은 `.cs`를 컴파일하므로, 컴파일 규칙이 다르면 "dotnet 초록"이
"유니티 초록"을 보장하지 못한다. 그래서 csproj에 **일부러 기능을 꺼 두었다**:

| 설정 | 왜 |
|---|---|
| `LangVersion 9.0` | 유니티 6000은 C# 9까지 컴파일한다 |
| `Nullable disable` | 유니티는 nullable 컨텍스트를 켜지 않는다 |
| `ImplicitUsings disable` | 유니티는 `using`을 자동으로 넣어 주지 않는다 |

세 줄을 켜는 순간 CI의 신뢰도가 사라진다. **켜지 말 것.**

### 빌드 산출물

`Directory.Build.props`가 전부 `artifacts/`로 몬다. `Runtime/obj/`가 생기면 유니티가
그 안의 `*.AssemblyInfo.cs`를 소스로 읽어 중복 어트리뷰트로 깨지기 때문이다.

---

## 규율

`Ked.Presentation.Core`에서 물려받는다. 새로 만들지 않는다.

| | |
|---|---|
| **1 — 침묵 금지** | 모르는 op, 없는 에피소드 ID, 미정의 스탯 키를 조용히 기본값으로 떨어뜨리지 않는다. 로더가 전부 모아서 진단으로 낸다 |
| **2 — 의존 0** | 역직렬화는 호스트가 한다. 이 패키지는 필드가 1:1인 DTO만 갖는다 |
| **3 — 게임 데이터는 인자** | 스탯 카탈로그·챕터 데이터는 전부 입력이다. 코드에 게임 어휘가 없다 |
| **4 — 순수 함수** | 평가·전이는 상태를 바꾸지 않고 새 상태를 돌려준다. 시간·랜덤·IO 없음 |

---

## 알려진 미완

| | |
|---|---|
| `.meta` 파일 | 아직 없다. **유니티에서 한 번 임포트해 생성한 뒤 커밋해야 한다** — UPM git 패키지는 커밋된 `.meta`의 GUID가 참조 안정성이다 |
| `StatChange` 지정(Set) | 깃발(bool 스탯)을 켜는 칸. 저작 쪽은 끝났고 이쪽 DTO에 `Op`가 설 때까지 **깃발을 쓰는 챕터는 내보내기가 거부된다** (`docs/work-plan.md` C1) |
| 단일 챕터 시나리오 | 툴이 시나리오를 아직 저작하지 않는다. 챕터 하나를 감싸는 헬퍼가 필요하다 (C2) |
| 간선 `Kind`를 JSON으로 | 저작엔 v11 `종류` 열이 있고 JSON엔 아직 `ChoiceLabel == ""`이 자동 진행이다. 그때까지 로더가 경고 한 줄로 모은다 (D5 · X3) |
| 챕터 연쇄 증명 | 챕터 하나 단위로 닫혔다. 잇는 방법은 정해졌고(`docs/work-plan.md` §7) 콘텐츠가 이어진 뒤에 한다 |
