using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using MandiocaCaipira.Desktop.Services;
using Serilog;

namespace MandiocaCaipira.Desktop;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, BancoSqlite banco)
    {
        services.AddSingleton(new ClienteRepository(banco));
        services.AddSingleton(new VendaRepository(banco));
        services.AddSingleton(new PagamentoRepository(banco));
        services.AddSingleton(new ResumoRepository(banco));
        services.AddSingleton(new FornecedorRepository(banco));
        services.AddSingleton(new CompraRepository(banco));
        services.AddSingleton(new ConfiguracaoRendimentoRepository(banco));
        services.AddSingleton(new ContaCaixaRepository(banco));
        services.AddSingleton(new CategoriaMovimentoRepository(banco));
        services.AddSingleton(new MovimentoCaixaRepository(banco));
        services.AddSingleton(new ConferenciaSaldoRepository(banco));
        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services, BancoSqlite banco)
    {
        services.AddSingleton<ClienteService>();
        services.AddSingleton<VendaService>();
        services.AddSingleton<PagamentoService>();
        services.AddSingleton<CompraService>();
        services.AddSingleton(new FotoService(CaminhosDados.Fotos));
        services.AddSingleton(new ReciboService(
            new VendaRepository(banco), new PagamentoRepository(banco), CaminhosDados.Recibos));
        services.AddSingleton(new ImportacaoClientesService(
            new LeitorArquivoClientes(), new ClienteRepository(banco), Log.Logger));
        services.AddSingleton<MovimentoCaixaService>();
        services.AddSingleton<ConferenciaService>();
        return services;
    }

    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton(new UpdateService(MauiProgram.RepoUrl));
        services.AddSingleton(new NovidadesService(MauiProgram.Versao));
        services.AddSingleton(new BugReportService(MauiProgram.Versao));
        return services;
    }
}
