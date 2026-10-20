using System.Collections.ObjectModel;
using ApiForge.Domain.GeneratedApiSolution;

namespace ApiForge.Desktop.ViewModels;

/// <summary>Nodo del árbol de vista previa: carpeta (Content == null) o archivo.</summary>
public sealed class FileNodeViewModel(string name, string? content = null)
{
    public string Name { get; } = name;

    public string? Content { get; } = content;

    public bool IsFile => Content is not null;

    public ObservableCollection<FileNodeViewModel> Children { get; } = [];

    /// <summary>Construye el árbol a partir de las rutas relativas (separadas por '/').</summary>
    public static IReadOnlyList<FileNodeViewModel> BuildTree(IEnumerable<GeneratedFile> files)
    {
        var root = new FileNodeViewModel(string.Empty);

        foreach (var file in files)
        {
            var parts = file.RelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var current = root;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                current = current.GetOrAddFolder(parts[i]);
            }

            current.Children.Add(new FileNodeViewModel(parts[^1], file.Content));
        }

        root.SortRecursive();
        return root.Children;
    }

    private FileNodeViewModel GetOrAddFolder(string folderName)
    {
        var existing = Children.FirstOrDefault(c => !c.IsFile && c.Name == folderName);
        if (existing is not null)
        {
            return existing;
        }

        var folder = new FileNodeViewModel(folderName);
        Children.Add(folder);
        return folder;
    }

    private void SortRecursive()
    {
        var sorted = Children
            .OrderBy(c => c.IsFile)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Children.Clear();
        foreach (var child in sorted)
        {
            Children.Add(child);
            child.SortRecursive();
        }
    }
}
