// dotnet 전용 — 유니티에서는 통째로 빠진다(Fixtures/가 EditMode로 복사되지 않는다).
#if !UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Ked.Progression.Dto;
using NUnit.Framework;

namespace Ked.Progression.Tests
{
    // 픽스처 파일의 모양. 코어가 아니라 여기가 정한다.
    //
    // 툴은 챕터 JSON만 내므로 시나리오는 저작물이 아니고, 코어에 시나리오 JSON 모양이
    // 없다. 조립은 호스트의 일이고 ProgressionLoader.LoadScenario가 DTO 대신 인자를
    // 받는 이유가 그것이다 — 이 테스트 어셈블리가 그 호스트 노릇을 한다.
    internal sealed class ScenarioFixtureFile
    {
        public string ScenarioId { get; set; }
        public string DisplayName { get; set; }
        public string StartChapterId { get; set; }
        public List<ChapterProgressionDto> Chapters { get; set; }
    }

    internal static class ScenarioFixture
    {
        public const string TwoChapters = "scenario-two-chapters.json";

        public static ScenarioFixtureFile Read(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

            Assert.That(File.Exists(path), Is.True, $"픽스처가 없다: {path}");

            return JsonSerializer.Deserialize<ScenarioFixtureFile>(File.ReadAllText(path));
        }

        public static ScenarioLoadResult Load(ScenarioFixtureFile file) =>
            ProgressionLoader.LoadScenario(
                file.ScenarioId, file.DisplayName, file.StartChapterId, file.Chapters);
    }
}

#endif
