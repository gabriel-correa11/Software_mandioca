using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Diagnostics;
using MandiocaCaipira.App.Services;
using Serilog;
using Velopack;

namespace MandiocaCaipira.Desktop;

public static class MauiProgram
{
    private static InstanciaUnica? _instancia;

    public const string Versao = "1.2.0";
    public const string RepoUrl = "https://github.com/gabriel-correa11/Software_mandioca";

    public static MauiApp CreateMauiApp()
    {
        Log.Logger = ConfiguracaoLogging.Criar(CaminhosDados.Logs);

        _instancia = new InstanciaUnica();
        if (!_instancia.IsOwner)
        {
            Log.Warning("Tentativa de abrir segunda instancia bloqueada");
            _instancia.Dispose();
            Environment.Exit(0);
        }

        ConfigurarTratamentoDeErros();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        var banco = ConfigurarBancoDeDados(builder.Services);

        builder.Services
            .AddRepositories(banco)
            .AddAppServices(banco)
            .AddDesktopServices();

        return builder.Build();
    }

    private static BancoSqlite ConfigurarBancoDeDados(IServiceCollection services)
    {
        var banco = new BancoSqlite(CaminhosDados.Banco);
        var inicializador = new InicializadorBanco(banco, CaminhosDados.Backups, Log.Logger);
        inicializador.Inicializar();

        var backup = new BackupService(inicializador, CaminhosDados.Backups,
            CaminhosDados.Banco, Log.Logger);

        if (!backup.TentarBackupAutomatico())
            Log.Warning("Nao foi possivel criar o backup automatico de hoje");

        services.AddSingleton(banco);
        services.AddSingleton(inicializador);
        services.AddSingleton(backup);
        services.AddSingleton(_instancia!);

        return banco;
    }

    private static void ConfigurarTratamentoDeErros()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Falha inesperada nao tratada");
            else
                Log.Fatal("Falha inesperada nao tratada");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Excecao nao observada em Task");
            args.SetObserved();
        };
    }

    public static void Encerrar()
    {
        _instancia?.Dispose();
        Log.Information("Aplicacao encerrada");
        Log.CloseAndFlush();
    }
}
