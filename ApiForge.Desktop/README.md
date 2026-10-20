# ApiForge.Desktop

App de escritorio multiplataforma (Avalonia 11 + .NET 10) que usa `ApiForge.Generator` en proceso.

## Añadir a la solución

Copia esta carpeta junto a `ApiForge.Generator/` y ejecuta:

    dotnet sln ApiForge.sln add ApiForge.Desktop/ApiForge.Desktop.csproj
    dotnet run --project ApiForge.Desktop

## Usar el paquete NuGet en vez del proyecto local

    dotnet run --project ApiForge.Desktop -p:UseLocalApiForge=false

## Publicar

    dotnet publish ApiForge.Desktop -c Release -r win-x64 --self-contained
    dotnet publish ApiForge.Desktop -c Release -r osx-arm64 --self-contained
    dotnet publish ApiForge.Desktop -c Release -r linux-x64 --self-contained

No actives trimming/AOT de momento (Microsoft.OpenApi usa reflexión).
