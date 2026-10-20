# ApiForge.Desktop

Desktop Application (Avalonia 11 + .NET 10) that uses `ApiForge.Generator` in-process.

## Usar el paquete NuGet en vez del proyecto local

    dotnet run --project ApiForge.Desktop -p:UseLocalApiForge=false

## Publicar

    dotnet publish ApiForge.Desktop -c Release -r win-x64 --self-contained
    dotnet publish ApiForge.Desktop -c Release -r osx-arm64 --self-contained
    dotnet publish ApiForge.Desktop -c Release -r linux-x64 --self-contained
