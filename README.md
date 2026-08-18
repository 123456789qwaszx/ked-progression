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
| 조건 평가 · 선택지 가시성 · 전이 판정 | 그래프 편집·레이아웃 — 저작 도구의 일 |
| 스탯 정의(초기값·경계)와 clamp | 게임별 스탯의 **의미** — `game.definition.json`이 공급 |
| 도달성 증명 | 세이브 직렬화 — 나중에, 별도로 |
| 로드 시 검증과 진단 | JSON 파싱 — 호스트가 한다 |

---

## 현재 상태 — `0.1.0` 태그 이후, 미태그

**진행 층이 돈다.** 저작 도구가 낸 실제 챕터 JSON이 오류 0으로 실리고, 시나리오 → 챕터 →
에피소드가 이어지며 **스탯이 챕터를 넘어간다.** 테스트 115개.

**아직 저장되지 않는다.** `ProgressionState`가 세이브가 담을 모양이지만 굽고 되읽는 경로가
없다 — 그것과 도달성 증명 이관이 남았다.

| 먼저 볼 것 | |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | **타입의 정본** — 원칙 다섯과 실제 형태 |
| [`docs/handoff.md`](docs/handoff.md) | 지금 무엇이 참인가 · 함정 · 부채 |
| [`docs/work-plan.md`](docs/work-plan.md) | 순서 · 게이트 · 남은 결정 |
| `Tests/ArchitectureWalkthroughTests.cs` | 흐름 전체가 한 화면 |

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
| 모델 본체 | `docs/model-draft.md` 검토 후 착수 |
| 스탯 정의의 출처 | `progression.json` vs `game.definition.json` — **소유자 결정 대기** (`docs/model-draft.md` §5-③) |
