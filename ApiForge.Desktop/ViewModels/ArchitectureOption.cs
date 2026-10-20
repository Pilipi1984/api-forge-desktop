using ApiForge.Domain.Enums;

namespace ApiForge.Desktop.ViewModels;

/// <summary>Opción del selector de arquitectura. <c>Style == null</c> significa "auto".</summary>
public sealed record ArchitectureOption(string Label, ArchitectureStyle? Style)
{
    public override string ToString() => Label;
}
