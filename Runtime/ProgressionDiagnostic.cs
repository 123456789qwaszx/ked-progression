using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    public enum ProgressionDiagnosticSeverity
    {
        /// <summary>로드 실패. 부분 통과를 만들지 않는다.</summary>
        Error = 0,

        /// <summary>실을 수는 있지만 저작자가 봐야 한다.</summary>
        Warning = 1,
    }

    /// <summary>
    /// 데이터의 잘못 하나.
    ///
    /// <b><see cref="Path"/>가 이 타입의 존재 이유다.</b> "정의되지 않은 스탯"만 받으면
    /// 저작자는 워크북 전체를 뒤져야 한다. <c>Nodes[ep_03].NextOptions[1].Conditions[0]</c>이
    /// 있으면 바로 그 자리로 간다.
    /// </summary>
    public sealed class ProgressionDiagnostic
    {
        public ProgressionDiagnosticSeverity Severity { get; }

        /// <summary>예: <c>Nodes[ep_03].NextOptions[1].Conditions[0]</c></summary>
        public string Path { get; }

        public string Message { get; }

        public ProgressionDiagnostic(
            ProgressionDiagnosticSeverity severity, string path, string message)
        {
            Severity = severity;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static ProgressionDiagnostic Error(string path, string message) =>
            new ProgressionDiagnostic(ProgressionDiagnosticSeverity.Error, path, message);

        public static ProgressionDiagnostic Warning(string path, string message) =>
            new ProgressionDiagnostic(ProgressionDiagnosticSeverity.Warning, path, message);

        public override string ToString() =>
            Path.Length == 0 ? Message : $"{Path}: {Message}";
    }

    /// <summary>시나리오 로드 결과. 챕터와 같은 규율이다 — 오류가 있으면 아무것도 내지 않는다.</summary>
    public sealed class ScenarioLoadResult
    {
        public ScenarioProgression Scenario { get; }
        public IReadOnlyList<ProgressionDiagnostic> Diagnostics { get; }

        public ScenarioLoadResult(
            ScenarioProgression scenario, IReadOnlyList<ProgressionDiagnostic> diagnostics)
        {
            Scenario = scenario;
            Diagnostics = diagnostics ?? Array.Empty<ProgressionDiagnostic>();
        }

        public bool IsValid => Scenario != null;

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Diagnostics.Count; i++)
                {
                    if (Diagnostics[i].Severity == ProgressionDiagnosticSeverity.Error)
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// 로드 결과. <b>진단이 하나라도 오류면 <see cref="Chapter"/>는 <c>null</c>이다</b> —
    /// 부분 통과를 만들지 않는다. 반쯤 실린 챕터는 재생해 봐야 무엇이 빠졌는지 알 수 있고,
    /// 그건 이 패키지가 막으려는 종류의 침묵이다.
    /// </summary>
    public sealed class ProgressionLoadResult
    {
        public ChapterProgression Chapter { get; }
        public IReadOnlyList<ProgressionDiagnostic> Diagnostics { get; }

        public ProgressionLoadResult(
            ChapterProgression chapter, IReadOnlyList<ProgressionDiagnostic> diagnostics)
        {
            Chapter = chapter;
            Diagnostics = diagnostics ?? Array.Empty<ProgressionDiagnostic>();
        }

        public bool IsValid => Chapter != null;

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Diagnostics.Count; i++)
                {
                    if (Diagnostics[i].Severity == ProgressionDiagnosticSeverity.Error)
                        return true;
                }

                return false;
            }
        }
    }
}