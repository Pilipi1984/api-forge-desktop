using ApiForge.Desktop.Tests.Fakes;
using ApiForge.Desktop.ViewModels;
using ApiForge.Domain.Enums;
using ApiForge.Domain.GeneratedApiSolution;

namespace ApiForge.Desktop.Tests.ViewModels
{
    /// <summary>
    /// Tests for the <see cref="MainViewModel"/> class.
    /// </summary>
    public sealed class MainViewModelTests : IDisposable
    {
        private readonly string _tempDir =
            Path.Combine(Path.GetTempPath(), "apiforge-desktop-tests-" + Guid.NewGuid().ToString("N"));
        private readonly FakeFilePicker _picker = new();
        private readonly FakeApiForgeGenerator _generator = new();
        private readonly MainViewModel _vm;

        public MainViewModelTests()
        {
            Directory.CreateDirectory(_tempDir);
            _vm = new MainViewModel(_generator, _picker);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private async Task LoadSpecAsync(string content = "openapi: 3.0.0")
        {
            var path = Path.Combine(_tempDir, "spec.yaml");
            await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
            _picker.SpecToReturn = path;
            await _vm.PickSpecCommand.ExecuteAsync(null);
        }

        private async Task GenerateSampleAsync()
        {
            await LoadSpecAsync();
            await _vm.GenerateCommand.ExecuteAsync(null);
        }

        // ---------- Initial state ----------

        /// <summary>
        /// Tests that the initial state offers three architecture options, defaulting to auto.
        /// </summary>
        [Fact]
        public void InitialState_DefaultsToAutoArchitecture()
        {
            Assert.Equal(3, _vm.Architectures.Count);
            Assert.Null(_vm.SelectedArchitecture.Style);
            Assert.Equal("Ningún archivo seleccionado", _vm.SpecFileName);
            Assert.Empty(_vm.Tree);
            Assert.Equal(string.Empty, _vm.PreviewText);
        }

        /// <summary>
        /// Tests that Generate and Save commands are disabled before a spec is loaded and generated.
        /// </summary>
        [Fact]
        public void InitialState_CommandsAreDisabled()
        {
            Assert.False(_vm.GenerateCommand.CanExecute(null));
            Assert.False(_vm.SaveToFolderCommand.CanExecute(null));
            Assert.False(_vm.SaveZipCommand.CanExecute(null));
        }

        // ---------- Picking the spec ----------

        /// <summary>
        /// Tests that picking a spec stores its path, shows the file name and enables Generate.
        /// </summary>
        [Fact]
        public async Task PickSpec_SetsPathAndEnablesGenerate()
        {
            _picker.SpecToReturn = Path.Combine("some", "dir", "spec.yaml");

            await _vm.PickSpecCommand.ExecuteAsync(null);

            Assert.Equal(_picker.SpecToReturn, _vm.SpecPath);
            Assert.Equal("spec.yaml", _vm.SpecFileName);
            Assert.True(_vm.GenerateCommand.CanExecute(null));
            Assert.Contains("Generar", _vm.Status);
        }

        /// <summary>
        /// Tests that cancelling the file dialog leaves the state untouched.
        /// </summary>
        [Fact]
        public async Task PickSpec_Cancelled_KeepsState()
        {
            _picker.SpecToReturn = null;

            await _vm.PickSpecCommand.ExecuteAsync(null);

            Assert.Null(_vm.SpecPath);
            Assert.False(_vm.GenerateCommand.CanExecute(null));
        }

        /// <summary>
        /// Tests that picking a new spec discards the previous generation result.
        /// </summary>
        [Fact]
        public async Task PickSpec_ClearsPreviousResult()
        {
            await GenerateSampleAsync();
            Assert.NotEmpty(_vm.Tree);

            _picker.SpecToReturn = Path.Combine(_tempDir, "other.yaml");
            await _vm.PickSpecCommand.ExecuteAsync(null);

            Assert.Empty(_vm.Tree);
            Assert.Null(_vm.Solution);
            Assert.False(_vm.SaveZipCommand.CanExecute(null));
        }

        // ---------- Generate ----------

        /// <summary>
        /// Tests that a successful generation fills the tree, the solution and the status, and enables saving.
        /// </summary>
        [Fact]
        public async Task Generate_Success_PopulatesTreeAndEnablesSave()
        {
            await LoadSpecAsync("spec-text");

            await _vm.GenerateCommand.ExecuteAsync(null);

            Assert.Equal("spec-text", _generator.ReceivedSpecText);
            Assert.NotNull(_vm.Solution);
            Assert.Equal(["Shop.Domain", "Shop.sln"], _vm.Tree.Select(n => n.Name));
            Assert.Contains("2 archivos", _vm.Status);
            Assert.Contains("Shop", _vm.Status);
            Assert.True(_vm.SaveToFolderCommand.CanExecute(null));
            Assert.True(_vm.SaveZipCommand.CanExecute(null));
            Assert.False(_vm.IsBusy);
        }

        /// <summary>
        /// Provides the architecture option index and the override the generator is expected to receive.
        /// </summary>
        public static TheoryData<int, ArchitectureStyle?> ArchitectureCases => new()
        {
            { 0, null },
            { 1, ArchitectureStyle.Clean },
            { 2, ArchitectureStyle.Hexagonal }
        };

        /// <summary>
        /// Tests that the selected architecture option is forwarded to the generator ("auto" sends no override).
        /// </summary>
        [Theory]
        [MemberData(nameof(ArchitectureCases))]
        public async Task Generate_ForwardsSelectedArchitecture(int optionIndex, ArchitectureStyle? expected)
        {
            _vm.SelectedArchitecture = _vm.Architectures[optionIndex];
            await LoadSpecAsync();

            await _vm.GenerateCommand.ExecuteAsync(null);

            Assert.Equal(expected, _generator.ReceivedOverride);
        }

        /// <summary>
        /// Tests that a generator failure is shown in the status and leaves no stale result.
        /// </summary>
        [Fact]
        public async Task Generate_Failure_ShowsErrorAndClearsResult()
        {
            await GenerateSampleAsync();
            Assert.NotEmpty(_vm.Tree);

            _generator.ToThrow = new InvalidOperationException("parse failed");
            await _vm.GenerateCommand.ExecuteAsync(null);

            Assert.StartsWith("Error al generar", _vm.Status);
            Assert.Contains("parse failed", _vm.Status);
            Assert.Null(_vm.Solution);
            Assert.Empty(_vm.Tree);
            Assert.False(_vm.IsBusy);
        }

        /// <summary>
        /// Tests that specs larger than 5 MB are rejected without calling the generator.
        /// </summary>
        [Fact]
        public async Task Generate_FileOverLimit_IsRejectedWithoutCallingGenerator()
        {
            var path = Path.Combine(_tempDir, "big.yaml");
            await using (var fs = File.Create(path))
            {
                fs.SetLength((5 * 1024 * 1024) + 1);
            }

            _picker.SpecToReturn = path;
            await _vm.PickSpecCommand.ExecuteAsync(null);

            await _vm.GenerateCommand.ExecuteAsync(null);

            Assert.Equal(0, _generator.GenerateCalls);
            Assert.Contains("5 MB", _vm.Status);
            Assert.Null(_vm.Solution);
        }

        /// <summary>
        /// Tests that IsBusy is true and commands are disabled while the generator is running.
        /// </summary>
        [Fact]
        public async Task Generate_WhileRunning_IsBusyAndCommandsDisabled()
        {
            var gate = new TaskCompletionSource<GeneratedSolution>();
            _generator.Gate = gate.Task;
            await LoadSpecAsync();

            var running = _vm.GenerateCommand.ExecuteAsync(null);

            Assert.True(_vm.IsBusy);
            Assert.False(_vm.GenerateCommand.CanExecute(null));

            gate.SetResult(FakeApiForgeGenerator.SampleSolution());
            await running;

            Assert.False(_vm.IsBusy);
            Assert.NotNull(_vm.Solution);
        }

        // ---------- Preview ----------

        /// <summary>
        /// Tests that selecting a file node exposes its content as the preview text.
        /// </summary>
        [Fact]
        public void SelectedNode_File_ExposesContentAndNotifies()
        {
            var changed = new List<string?>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.SelectedNode = new FileNodeViewModel("Pet.cs", "class Pet");

            Assert.Equal("class Pet", _vm.PreviewText);
            Assert.Contains(nameof(MainViewModel.PreviewText), changed);
        }

        /// <summary>
        /// Tests that the preview is empty for folder nodes and when nothing is selected.
        /// </summary>
        [Fact]
        public void SelectedNode_FolderOrNull_HasEmptyPreview()
        {
            _vm.SelectedNode = new FileNodeViewModel("Models");
            Assert.Equal(string.Empty, _vm.PreviewText);

            _vm.SelectedNode = null;
            Assert.Equal(string.Empty, _vm.PreviewText);
        }

        // ---------- Save to folder ----------

        /// <summary>
        /// Tests that all generated files are written under the chosen folder, creating subfolders.
        /// </summary>
        [Fact]
        public async Task SaveToFolder_WritesAllFiles()
        {
            await GenerateSampleAsync();
            var outDir = Path.Combine(_tempDir, "out");
            _picker.FolderToReturn = outDir;

            await _vm.SaveToFolderCommand.ExecuteAsync(null);

            Assert.Equal("sln-content",
                await File.ReadAllTextAsync(Path.Combine(outDir, "Shop.sln"), TestContext.Current.CancellationToken));
            Assert.Equal("class Pet",
                await File.ReadAllTextAsync(Path.Combine(outDir, "Shop.Domain", "Models", "Pet.cs"), TestContext.Current.CancellationToken));
            Assert.Contains("Solución guardada", _vm.Status);
            Assert.False(_vm.IsBusy);
        }

        /// <summary>
        /// Tests that cancelling the folder dialog writes nothing.
        /// </summary>
        [Fact]
        public async Task SaveToFolder_Cancelled_WritesNothing()
        {
            await GenerateSampleAsync();
            _picker.FolderToReturn = null;

            await _vm.SaveToFolderCommand.ExecuteAsync(null);

            Assert.False(Directory.Exists(Path.Combine(_tempDir, "out")));
        }

        /// <summary>
        /// Tests that generated paths escaping the destination folder are rejected and nothing is written outside it.
        /// </summary>
        [Theory]
        [InlineData("../evil.txt")]
        [InlineData("../out2/x.txt")]
        [InlineData("a/../../evil.txt")]
        public async Task SaveToFolder_PathTraversal_IsRejected(string relativePath)
        {
            _generator.Solution = new GeneratedSolution
            {
                Name = "Evil",
                RootNamespace = "Evil",
                Files = [new GeneratedFile { RelativePath = relativePath, Content = "pwned" }]
            };
            await GenerateSampleAsync();

            var outDir = Path.Combine(_tempDir, "out");
            _picker.FolderToReturn = outDir;

            await _vm.SaveToFolderCommand.ExecuteAsync(null);

            var escaped = Path.GetFullPath(Path.Combine(outDir, relativePath));
            Assert.False(File.Exists(escaped));
            Assert.StartsWith("Error al guardar", _vm.Status);
            Assert.Contains("Ruta fuera", _vm.Status);
            Assert.False(_vm.IsBusy);
        }

        // ---------- Save zip ----------

        /// <summary>
        /// Tests that the zip is requested from the generator with the selected architecture and written to the chosen path.
        /// </summary>
        [Fact]
        public async Task SaveZip_WritesGeneratorOutput_WithSelectedArchitecture()
        {
            _vm.SelectedArchitecture = _vm.Architectures[2];
            await GenerateSampleAsync();
            var zipPath = Path.Combine(_tempDir, "out.zip");
            _picker.ZipPathToReturn = zipPath;

            await _vm.SaveZipCommand.ExecuteAsync(null);

            Assert.Equal("Shop.zip", _picker.SuggestedZipName);
            Assert.Equal(1, _generator.ZipCalls);
            Assert.Equal(ArchitectureStyle.Hexagonal, _generator.ReceivedOverride);
            byte[] fileBytes = await File.ReadAllBytesAsync(zipPath, TestContext.Current.CancellationToken);
            Assert.Equal<byte>([1, 2, 3], fileBytes);
            Assert.Contains("ZIP guardado", _vm.Status);
        }

        /// <summary>
        /// Tests that cancelling the save dialog does not call the generator again.
        /// </summary>
        [Fact]
        public async Task SaveZip_Cancelled_DoesNotCallGenerator()
        {
            await GenerateSampleAsync();
            _picker.ZipPathToReturn = null;

            await _vm.SaveZipCommand.ExecuteAsync(null);

            Assert.Equal(0, _generator.ZipCalls);
        }

        /// <summary>
        /// Tests that a failure while generating the zip is reported in the status.
        /// </summary>
        [Fact]
        public async Task SaveZip_GeneratorFails_ShowsError()
        {
            await GenerateSampleAsync();
            _picker.ZipPathToReturn = Path.Combine(_tempDir, "out.zip");
            _generator.ToThrow = new InvalidOperationException("boom");

            await _vm.SaveZipCommand.ExecuteAsync(null);

            Assert.StartsWith("Error al guardar", _vm.Status);
            Assert.Contains("boom", _vm.Status);
            Assert.False(File.Exists(_picker.ZipPathToReturn));
        }
    }
}
