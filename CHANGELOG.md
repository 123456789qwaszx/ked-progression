# CHANGELOG

형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/), 버전은 [SemVer](https://semver.org/lang/ko/).

`0.x` 동안은 **공개 표면을 약속하지 않는다.** 타입이 굳으면 `1.0.0`을 붙인다.

## [Unreleased]

### 예정
- `ProgressionCondition` · `StatDefinition` · `ChapterProgression` 모델 (§G1·G7)
- `ProgressionLoader` — DTO → 모델 검증 (§G2·G4, 침묵 금지)
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
