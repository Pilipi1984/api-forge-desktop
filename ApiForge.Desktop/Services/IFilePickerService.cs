namespace ApiForge.Desktop.Services;

/// <summary>Abstrae los diálogos de archivo/carpeta para poder probar los ViewModels.</summary>
public interface IFilePickerService
{
    /// <summary>Pide al usuario una especificación OpenAPI (.yaml, .yml, .json).</summary>
    Task<string?> PickSpecAsync();

    /// <summary>Pide al usuario la carpeta de destino.</summary>
    Task<string?> PickFolderAsync();

    /// <summary>Pide al usuario dónde guardar el .zip.</summary>
    Task<string?> PickSaveZipAsync(string suggestedFileName);
}
