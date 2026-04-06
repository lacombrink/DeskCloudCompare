using DeskCloudCompare.Data;
using DeskCloudCompare.Services;
using DeskCloudCompare.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace DeskCloudCompare;

public partial class App : Application
{
    public static string AppFolder { get; private set; } = string.Empty;

    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        AppFolder = AppDomain.CurrentDomain.BaseDirectory;
        var dbFolder = Path.Combine(AppFolder, "Database");
        var dbPath = Path.Combine(dbFolder, "deskcloudcompare.db");
        Directory.CreateDirectory(dbFolder);

        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        services.AddTransient<FolderTypeService>();
        services.AddTransient<PresetExclusionService>();
        services.AddTransient<PathTranslationService>();
        services.AddTransient<PresetService>();
        services.AddTransient<FolderScanService>();
        services.AddTransient<BinaryCompareService>();
        services.AddTransient<SpecialFileRuleService>();
        services.AddTransient<CountryManagerScanService>();
        services.AddTransient<FrameworkManagerScanService>();

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CountryManagerViewModel>();
        services.AddTransient<FrameworkManagerViewModel>();
        services.AddTransient<SubFrameworkManagerViewModel>();
        services.AddTransient<PresetsViewModel>();
        services.AddTransient<ComparisonViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

