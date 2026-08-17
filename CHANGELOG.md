# CHANGELOG

형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/), 버전은 [SemVer](https://semver.org/lang/ko/).

`0.x` 동안은 **공개 표면을 약속하지 않는다.** 타입이 굳으면 `1.0.0`을 붙인다.

## [Unreleased]

### 추가
- `ProgressionCondition` · `ConditionKind` — 조건 모델 (§G1·G2)
- `StatDefinition` · `StatType` — **§G7의 빈칸.** 초기값·경계의 유일한 집
- `ProgressionState` · `StatChange` — 불변 상태. 나중에 세이브가 담을 내용
- `ConditionEvaluator` — 조건 판정 (§G3·G4·G6)
- 계약 테스트 27개

### 결정
- **정의되지 않은 스탯 키는 0이 아니라 예외다.** 조용히 0을 주면 오타 낸 조건이
  "언제나 통과하는 관문"으로 바뀌고, 그 버그는 재생해 봐도 안 보인다
  (코어의 `RectNodeTree.GetState("없는키")`와 같은 규율)
- **초기값이 경계 밖이면 거부한다.** 조용히 clamp하면 작가가 쓴 값과 다른 값으로 시작한다
- `EpisodeCleared`는 `Exists`만 받는다. 다른 연산은 예외 — 저작 출력 확인이 먼저다

### 예정
- `ChapterProgression` · `EpisodeNode` · `EpisodeOption` (§G1·G5)
- `ProgressionLoader` — DTO → 모델 검증 ⚠ **스탯 정의의 출처 결정 대기**
- `ChapterTransition` — 전이 3갈래 (§G6)
- VnTool의 `ChapterReachabilityProver` 이관 (오라클)

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
