using ApiForge.Application.Interfaces;
using ApiForge.Desktop.Services;
using ApiForge.Domain.Enums;
using ApiForge.Domain.GeneratedApiSolution;

namespace ApiForge.Desktop.Tests.Fakes
{
    /// <summary>
    /// Fake file picker whose results are configured by each test.
    /// </summary>
    internal sealed class FakeFilePicker : IFilePickerService
    {
        public string? SpecToReturn { get; set; }
        public string? FolderToReturn { get; set; }
        public string? ZipPathToReturn { get; set; }
        public string? SuggestedZipName { get; private set; }

        public Task<string?> PickSpecAsync() => Task.FromResult(SpecToReturn);

        public Task<string?> PickFolderAsync() => Task.FromResult(FolderToReturn);

        public Task<string?> PickSaveZipAsync(string suggestedFileName)
        {
            SuggestedZipName = suggestedFileName;
            return Task.FromResult(ZipPathToReturn);
        }
    }

    /// <summary>
    /// Fake generator that records what it receives and returns a configurable result.
    /// </summary>
    internal sealed class FakeApiForgeGenerator : IApiForgeGenerator
    {
        public GeneratedSolution Solution { get; set; } = SampleSolution();
        public byte[] ZipContent { get; set; } = [1, 2, 3];
        public Exception? ToThrow { get; set; }
        public Task<GeneratedSolution>? Gate { get; set; }

        public int GenerateCalls { get; private set; }
        public int ZipCalls { get; private set; }
        public ArchitectureStyle? ReceivedOverride { get; private set; }
        public string? ReceivedSpecText { get; private set; }

        public static GeneratedSolution SampleSolution() => new()
        {
            Name = "Shop",
            RootNamespace = "Shop",
            Files =
            [
                new() { RelativePath = "Shop.sln", Content = "sln-content" },
                new() { RelativePath = "Shop.Domain/Models/Pet.cs", Content = "class Pet" }
            ]
        };

        public async Task<GeneratedSolution> GenerateAsync(Stream openApiSpec, ArchitectureStyle? architectureOverride = null)
        {
            GenerateCalls++;
            ReceivedOverride = architectureOverride;

            using var reader = new StreamReader(openApiSpec, leaveOpen: true);
            ReceivedSpecText = await reader.ReadToEndAsync();

            if (ToThrow is not null)
            {
                throw ToThrow;
            }

            return Gate is null ? Solution : await Gate;
        }

        public async Task<GeneratedZipArchive> GenerateZipAsync(Stream openApiSpec, ArchitectureStyle? architectureOverride = null)
        {
            ZipCalls++;
            ReceivedOverride = architectureOverride;

            using var reader = new StreamReader(openApiSpec, leaveOpen: true);
            ReceivedSpecText = await reader.ReadToEndAsync();

            if (ToThrow is not null)
            {
                throw ToThrow;
            }

            return new GeneratedZipArchive { Name = Solution.Name, Content = ZipContent };
        }
    }
}
