using System.Collections.ObjectModel;
using ApiForge.Application.Interfaces;
using ApiForge.Desktop.Services;
using ApiForge.Domain.Enums;
using ApiForge.Domain.GeneratedApiSolution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiForge.Desktop.ViewModels;

public sealed partial class MainViewModel(IApiForgeGenerator generator, IFilePickerService files) : ObservableObject
{
    private const long MaxSpecSizeBytes = 5 * 1024 * 1024; // mismo límite que la Web API

    private static readonly ArchitectureOption[] ArchitectureOptions =
    [
        new("Auto (según la especificación)", null),
        new("Clean Architecture", ArchitectureStyle.Clean),
        new("Hexagonal (Ports & Adapters)", ArchitectureStyle.Hexagonal)
    ];

    public IReadOnlyList<ArchitectureOption> Architectures => ArchitectureOptions;

    public ObservableCollection<FileNodeViewModel> Tree { get; } = [];

    [ObservableProperty]
    private ArchitectureOption _selectedArchitecture = ArchitectureOptions[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpecFileName))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private string? _specPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveZipCommand))]
    private GeneratedSolution? _solution;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    private FileNodeViewModel? _selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveZipCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "Abre una especificación OpenAPI (JSON o YAML) para empezar.";

    public string SpecFileName =>
        string.IsNullOrEmpty(SpecPath) ? "Ningún archivo seleccionado" : Path.GetFileName(SpecPath);

    public string PreviewText => SelectedNode?.Content ?? string.Empty;

    private bool CanGenerate() => !IsBusy && !string.IsNullOrEmpty(SpecPath);

    private bool CanSave() => !IsBusy && Solution is not null;

    [RelayCommand]
    private async Task PickSpecAsync()
    {
        var path = await files.PickSpecAsync();
        if (path is null)
        {
            return;
        }

        SpecPath = path;
        ClearResult();
        Status = "Especificación cargada. Pulsa «Generar».";
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        IsBusy = true;
        try
        {
            await using var stream = OpenSpec();
            var solution = await generator.GenerateAsync(stream, SelectedArchitecture.Style);

            ClearResult();
            Solution = solution;
            foreach (var node in FileNodeViewModel.BuildTree(solution.Files))
            {
                Tree.Add(node);
            }

            Status = $"{solution.Files.Count} archivos generados para «{solution.Name}».";
        }
        catch (Exception ex)
        {
            ClearResult();
            Status = $"Error al generar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveToFolderAsync()
    {
        var folder = await files.PickFolderAsync();
        if (folder is null || Solution is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var file in Solution.Files)
            {
                var target = Path.GetFullPath(Path.Combine(root, file.RelativePath));
                if (!target.StartsWith(root + Path.DirectorySeparatorChar, comparison))
                {
                    throw new InvalidOperationException($"Ruta fuera de la carpeta de destino: {file.RelativePath}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await File.WriteAllTextAsync(target, file.Content);
            }

            Status = $"Solución guardada en {root}";
        }
        catch (Exception ex)
        {
            Status = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveZipAsync()
    {
        if (Solution is null)
        {
            return;
        }

        var path = await files.PickSaveZipAsync($"{Solution.Name}.zip");
        if (path is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = OpenSpec();
            var zip = await generator.GenerateZipAsync(stream, SelectedArchitecture.Style);
            await File.WriteAllBytesAsync(path, zip.Content);

            Status = $"ZIP guardado en {path}";
        }
        catch (Exception ex)
        {
            Status = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private FileStream OpenSpec()
    {
        var info = new FileInfo(SpecPath!);
        if (info.Length > MaxSpecSizeBytes)
        {
            throw new InvalidOperationException("El archivo supera el límite de 5 MB.");
        }

        return File.OpenRead(info.FullName);
    }

    private void ClearResult()
    {
        Solution = null;
        SelectedNode = null;
        Tree.Clear();
    }
}
