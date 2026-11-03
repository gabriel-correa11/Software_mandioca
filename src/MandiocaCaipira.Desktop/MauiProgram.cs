using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Diagnostics;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using MandiocaCaipira.Desktop.Services;
using Serilog;
using Velopack;

namespace MandiocaCaipira.Desktop;

public static class MauiProgram
{
    private static InstanciaUnica? _instancia;

    public static readonly string UpdateUrl = CaminhosDados.Updates;

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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        var banco = new BancoSqlite(CaminhosDados.Banco);
        var inicializador = new InicializadorBanco(banco, CaminhosDados.Backups, Log.Logger);
        inicializador.Inicializar();

        var backup = new BackupService(inicializador, CaminhosDados.Backups,
            CaminhosDados.Banco, Log.Logger);

        if (!backup.TentarBackupAutomatico())
            Log.Warning("Nao foi possivel criar o backup automatico de hoje");

        builder.Services.AddSingleton(banco);
        builder.Services.AddSingleton(inicializador);
        builder.Services.AddSingleton(backup);
        builder.Services.AddSingleton(_instancia);

        builder.Services.AddSingleton(new ClienteRepository(banco));
        builder.Services.AddSingleton(new VendaRepository(banco));
        builder.Services.AddSingleton(new PagamentoRepository(banco));
        builder.Services.AddSingleton(new ResumoRepository(banco));
        builder.Services.AddSingleton(new FornecedorRepository(banco));
        builder.Services.AddSingleton(new CompraRepository(banco));
        builder.Services.AddSingleton(new ConfiguracaoRendimentoRepository(banco));

        builder.Services.AddSingleton<ClienteService>();
        builder.Services.AddSingleton<VendaService>();
        builder.Services.AddSingleton<PagamentoService>();
        builder.Services.AddSingleton<CompraService>();
        builder.Services.AddSingleton(new FotoService(CaminhosDados.Fotos));
        builder.Services.AddSingleton(new ReciboService(
            new VendaRepository(banco), new PagamentoRepository(banco), CaminhosDados.Recibos));
        builder.Services.AddSingleton(new ImportacaoClientesService(
            new LeitorArquivoClientes(), new ClienteRepository(banco), Log.Logger));
        builder.Services.AddSingleton(new UpdateService(UpdateUrl));

        return builder.Build();
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
