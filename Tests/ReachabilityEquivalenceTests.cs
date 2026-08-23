// dotnet 전용 — 유니티에서는 통째로 빠진다.
//
// 픽스처를 System.Text.Json으로 읽는데 유니티에는 그 어셈블리가 없고(컴파일 자체가 안 된다),
// Tests/Fixtures/*.json도 EditMode로 복사되지 않는다. 규율 2대로 역직렬화는 호스트의 일이라
// 코어가 파서를 갖지 않기 때문이고, 그래서 이 파일들은 dotnet CI에서만 돈다.
#if !UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    /// <summary>
    /// <b>게이트 G5 — 도달성 증명이 이관 전후로 같은 답을 낸다.</b>
    ///
    /// 이 패키지의 <b>오라클</b>이다. 다른 테스트들은 "내가 생각한 대로 도는가"를 묻지만
    /// 여기는 <b>"저작 도구가 이미 내던 답과 같은가"</b>를 묻는다 — 동작 불변을 주장이 아니라
    /// 증거로 만드는 자리이고, <c>Ked.Presentation.Core</c> 추출 때 등가성 하네스가 했던
    /// 역할이 이것이다.
    ///
    /// 코퍼스는 저작 도구의 <c>ChapterReachabilityProver</c>를 그대로 돌려 뽑았다
    /// (2026-08-18, 프루버가 아직 안 건드려진 시점 — 기준선은 흔들리기 전에 박아야 한다).
    /// 케이스마다 <b>내보낸 챕터 JSON</b>과 <b>그때의 증명 결과</b>가 한 벌로 들어 있다.
    ///
    /// ⚠ 이 픽스처는 <b>우리가 뽑은 덤프이지 저작 계약이 아니다.</b> 그래서 이름 대소문자를
    /// 관대하게 읽는다 — 계약 픽스처(<c>chapter-sample-export.json</c>)를 엄격하게 읽는 것과
    /// 반대인데, 저기서는 이름 불일치가 곧 계약 위반이고 여기서는 아니기 때문이다.
    /// </summary>
    public sealed class ReachabilityEquivalenceTests
    {
        private sealed class OracleSpan
        {
            public string Key { get; set; }
            public int Minimum { get; set; }
            public int Maximum { get; set; }
        }

        private sealed class OracleCase
        {
            public string Name { get; set; }
            public string ChapterJson { get; set; }
            public List<string> Reachable { get; set; }
            public bool Complete { get; set; }
            public Dictionary<string, List<OracleSpan>> Spans { get; set; }
        }

        private static readonly JsonSerializerOptions Lenient =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private static List<OracleCase> ReadCorpus()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Fixtures", "reachability-oracle.json");

            Assert.That(File.Exists(path), Is.True, $"코퍼스가 없다: {path}");

            return JsonSerializer.Deserialize<List<OracleCase>>(File.ReadAllText(path), Lenient);
        }

        public static IEnumerable<string> CaseNames() =>
            ReadCorpus().Select(item => item.Name);

        [TestCaseSource(nameof(CaseNames))]
        public void 이관_전후로_증명_결과가_같다(string name)
        {
            OracleCase expected = ReadCorpus().Single(item => item.Name == name);

            ChapterProgressionDto dto =
                JsonSerializer.Deserialize<ChapterProgressionDto>(expected.ChapterJson);

            ProgressionLoadResult loaded = ProgressionLoader.Load(dto);

            Assert.That(loaded.HasErrors, Is.False,
                string.Join(" | ", loaded.Diagnostics.Select(d => d.ToString())));

            ReachabilityResult actual = ChapterReachability.Prove(loaded.Chapter);

            // ① 도달 가능 집합 — 증명의 본체
            Assert.That(
                actual.ReachableEpisodeIds.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                Is.EqualTo(expected.Reachable),
                $"[{name}] 도달 가능 집합이 갈렸다");

            // ② 완전 탐색 여부 — 증명하지 못한 것을 증명했다고 말하지 않는다
            Assert.That(actual.ExplorationComplete, Is.EqualTo(expected.Complete),
                $"[{name}] 탐색 완료 여부가 갈렸다");

            // ③ 도착 시점 스탯 폭 — D1의 챕터 연쇄가 이 값을 쓴다
            Assert.That(
                expected.Spans.Keys.OrderBy(x => x, StringComparer.Ordinal),
                Is.EqualTo(actual.ReachableEpisodeIds
                    .Where(id => actual.SpansFor(id).Count > 0)
                    .OrderBy(x => x, StringComparer.Ordinal)),
                $"[{name}] 폭이 기록된 에피소드가 갈렸다");

            foreach (KeyValuePair<string, List<OracleSpan>> pair in expected.Spans)
            {
                IReadOnlyList<StatSpan> mine = actual.SpansFor(pair.Key);

                Assert.That(mine.Select(s => $"{s.Key}:{s.Minimum}~{s.Maximum}").ToList(),
                    Is.EqualTo(pair.Value.Select(s => $"{s.Key}:{s.Minimum}~{s.Maximum}").ToList()),
                    $"[{name}] '{pair.Key}' 도착 시점 폭이 갈렸다");
            }
        }

        [Test]
        public void 코퍼스가_알고리즘의_각_부분을_덮는다()
        {
            // 케이스를 지우면 덮이지 않는 갈래가 생긴다 — 그때 이 테스트가 먼저 말한다.
            Assert.That(ReadCorpus().Select(item => item.Name), Is.EquivalentTo(new[]
            {
                "직선",           // 자동 진행만
                "분기수렴",       // 갈렸다 만나며 도착 폭이 벌어진다
                "관문",           // 스탯 관문 — 한 루트로만 열린다
                "도달불가",       // 어떤 경로로도 못 미치는 관문
                "cleared고정점",  // 도달 집합을 참조하는 조건 — 고정점 반복
                "부착",           // 간선 없는 것 · 관문 달린 것
                "clamp",          // 증감이 경계를 넘는다
            }));
        }
    }
}

#endif
