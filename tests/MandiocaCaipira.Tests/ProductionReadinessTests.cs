using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;

namespace MandiocaCaipira.Tests;

public sealed class ProductionReadinessTests
{
    [Fact]
    public void FluxoDiarioCompletoGeraDocumentosEBackupValidos()
    {
        using var app = new AppTeste();
        var cliente = app.Clientes.Cadastrar("Família Exemplo");
        var venda = app.Vendas.Registrar(cliente, 12_500, 350, new DateOnly(2026, 10, 5));

        var parcial = app.Pagamentos.Registrar(cliente, 2_000, new DateOnly(2026, 10, 5),
            [new AlocacaoSolicitada(venda, 2_000)]);
        Assert.Equal(2_375, Assert.Single(app.Vendas.Listar(cliente)).SaldoCentavos);

        var final = app.Pagamentos.Registrar(cliente, 2_500, new DateOnly(2026, 10, 5),
            [new AlocacaoSolicitada(venda, 2_375)]);
        Assert.Equal(0, Assert.Single(app.Vendas.Listar(cliente)).SaldoCentavos);
        Assert.Equal(125, app.Pagamentos.Listar(cliente).Single(p => p.Id == final).CreditoCentavos);

        var recibo = app.Recibos.GerarPagamento(parcial);
        var comprovante = app.Recibos.GerarVenda(venda);
        AssertPdfValido(recibo);
        AssertPdfValido(comprovante);

        var cancelavel = app.Vendas.Registrar(cliente, 1_000, 200, new DateOnly(2026, 10, 5));
        app.Vendas.Cancelar(cancelavel, "Teste de cancelamento");
        Assert.True(app.Vendas.Listar(cliente).Single(v => v.Id == cancelavel).Cancelada);

        app.Pagamentos.Estornar(final, "Teste de estorno");
        Assert.Equal(2_375, app.Vendas.Listar(cliente).Single(v => v.Id == venda).SaldoCentavos);

        var backup = app.Backup.Criar();
        Assert.True(File.Exists(backup));
        using var bancoBackup = new BancoSqlite(backup).AbrirConexao();
        using var command = bancoBackup.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Cliente;";
        Assert.Equal(1L, command.ExecuteScalar());
    }

    [Fact]
    public void ReciboDePagamentoMostraCreditoSemCriarFatoFinanceiroNovo()
    {
        using var app = new AppTeste();
        var cliente = app.Clientes.Cadastrar("Cliente Fictício");
        var pagamento = app.Pagamentos.Registrar(cliente, 10_000, new DateOnly(2026, 10, 5), []);

        var path = app.Recibos.GerarPagamento(pagamento);

        AssertPdfValido(path);
        Assert.Equal(10_000, Assert.Single(app.Pagamentos.Listar(cliente)).CreditoCentavos);
    }

    private static void AssertPdfValido(string path)
    {
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 1_000);
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        var header = new char[5];
        Assert.Equal(5, reader.ReadBlock(header, 0, header.Length));
        Assert.Equal("%PDF-", new string(header));
    }

    private sealed class AppTeste : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));
        public ClienteService Clientes { get; }
        public VendaService Vendas { get; }
        public PagamentoService Pagamentos { get; }
        public ReciboService Recibos { get; }
        public BackupService Backup { get; }

        public AppTeste()
        {
            Directory.CreateDirectory(_root);
            var banco = new BancoSqlite(Path.Combine(_root, "data", "test.sqlite"));
            var inicializador = new InicializadorBanco(banco, Path.Combine(_root, "backups"));
            inicializador.Inicializar();
            var clientes = new ClienteRepository(banco); var vendas = new VendaRepository(banco); var pagamentos = new PagamentoRepository(banco);
            Clientes = new ClienteService(clientes); Vendas = new VendaService(banco, vendas, pagamentos); Pagamentos = new PagamentoService(banco, pagamentos, vendas);
            Recibos = new ReciboService(vendas, pagamentos, Path.Combine(_root, "recibos")); Backup = new BackupService(inicializador, Path.Combine(_root, "backups"), banco.CaminhoBanco);
        }
        public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    }
}
