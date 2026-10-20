using ApiForge.Application.Interfaces;
using ApiForge.Desktop.Services;
using ApiForge.Desktop.ViewModels;
using ApiForge.Desktop.Views;
using ApiForge.Generator;
using ApiForge.Infrastructure.Generator;
using ApiForge.Infrastructure.Parser;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiForge.Desktop;

// OJO: se califica "Avalonia.Application" porque, dentro del namespace ApiForge.*,
// el identificador "Application" resolvería al namespace ApiForge.Application.
public sealed class App : Avalonia.Application
{
    public IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddDebug());

        services.AddSingleton<IOpenApiParser, OpenApiParser>();
        services.AddSingleton<ICodeGenerator, CodeGenerator>();
        services.AddSingleton<IApiForgeGenerator>(sp => new ApiForgeGenerator(
            sp.GetRequiredService<IOpenApiParser>(),
            sp.GetRequiredService<ICodeGenerator>(),
            sp.GetService<ILogger<ApiForgeGenerator>>()));

        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
