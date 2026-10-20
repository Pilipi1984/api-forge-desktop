using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace ApiForge.Desktop.Services;

public sealed class FilePickerService : IFilePickerService
{
    private static TopLevel? GetTopLevel() =>
        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<string?> PickSpecAsync()
    {
        if (GetTopLevel() is not { } top)
        {
            return null;
        }

        var result = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona una especificación OpenAPI",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("OpenAPI (JSON / YAML)") { Patterns = ["*.yaml", "*.yml", "*.json"] }
            ]
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFolderAsync()
    {
        if (GetTopLevel() is not { } top)
        {
            return null;
        }

        var result = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Carpeta de destino",
            AllowMultiple = false
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveZipAsync(string suggestedFileName)
    {
        if (GetTopLevel() is not { } top)
        {
            return null;
        }

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar solución generada",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "zip",
            FileTypeChoices = [new FilePickerFileType("Archivo ZIP") { Patterns = ["*.zip"] }]
        });

        return file?.TryGetLocalPath();
    }
}
