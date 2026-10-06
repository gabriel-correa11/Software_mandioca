using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Diagnostics;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using Serilog;

namespace MandiocaCaipira.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        ConfigurarLogging();
        ConfigurarTratamentoDeFalhasInesperadas();

        try
        {
            Log.Information("Aplicação iniciada");
            var banco = new BancoSqlite(CaminhosDados.Banco);
            var inicializador = new InicializadorBanco(banco, CaminhosDados.Backups, Log.Logger);
            inicializador.Inicializar();
            var clientesRepo = new ClienteRepository(banco);
            var vendasRepo = new VendaRepository(banco);
            var pagamentosRepo = new PagamentoRepository(banco);
            var clientes = new ClienteService(clientesRepo);
            var importacaoClientes = new ImportacaoClientesService(new LeitorArquivoClientes(), clientesRepo, Log.Logger);
            var vendas = new VendaService(banco, vendasRepo, pagamentosRepo);
            var pagamentos = new PagamentoService(banco, pagamentosRepo, vendasRepo);
            var recibos = new ReciboService(vendasRepo, pagamentosRepo, CaminhosDados.Recibos);
            Application.Run(new MainForm(clientes, vendas, pagamentos,
                new ResumoRepository(banco), recibos, new BackupService(inicializador), importacaoClientes));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Falha inesperada durante a inicialização ou execução da aplicação");
            MessageBox.Show(
                "Não foi possível iniciar o Mandioca Caipira. Seus dados não foram alterados. Consulte os logs para diagnóstico.",
                "Mandioca Caipira",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("Aplicação encerrada");
            Log.CloseAndFlush();
        }
    }

    private static void ConfigurarLogging()
    {
        try
        {
            Log.Logger = ConfiguracaoLogging.Criar(CaminhosDados.Logs);
        }
        catch
        {
            Log.Logger = new LoggerConfiguration().CreateLogger();
            MessageBox.Show(
                "Não foi possível inicializar o arquivo de diagnóstico. A aplicação continuará sem gravar logs nesta execução.",
                "Mandioca Caipira",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void ConfigurarTratamentoDeFalhasInesperadas()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Falha inesperada na interface da aplicação");
            MessageBox.Show(
                "Ocorreu um erro inesperado. Consulte os logs para diagnóstico. A aplicação será encerrada.",
                "Mandioca Caipira",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
                Log.Fatal(exception, "Falha inesperada fora da interface da aplicação");
            else
                Log.Fatal("Falha inesperada fora da interface da aplicação");
            Log.CloseAndFlush();
        };
    }
}
