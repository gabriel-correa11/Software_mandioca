using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;

namespace MandiocaCaipira.Tests;

public sealed class CasosDeUsoTests
{
    [Fact]
    public void VendaComPagamentoInicialMantemCreditoEAtualizaResumo()
    {
        using var app = new AplicacaoTeste();
        var clienteId = app.Clientes.Cadastrar("Cliente Fictício");

        app.Vendas.Registrar(clienteId, 1_000, 10_000, new DateOnly(2026, 10, 5), 12_000);

        var venda = Assert.Single(app.Vendas.Listar(clienteId));
        Assert.Equal(10_000, venda.TotalCentavos);
        Assert.Equal(10_000, venda.RecebidoCentavos);
        Assert.Equal(0, venda.SaldoCentavos);
        var pagamento = Assert.Single(app.Pagamentos.Listar(clienteId));
        Assert.Equal(2_000, pagamento.CreditoCentavos);
        var resumo = app.Resumos.Obter(new DateOnly(2026, 10, 5));
        Assert.Equal(1, resumo.VendasRealizadas);
        Assert.Equal(1_000, resumo.QuantidadeGramas);
        Assert.Equal(10_000, resumo.ValorVendidoCentavos);
        Assert.Equal(12_000, resumo.RecebimentosCentavos);
    }

    [Fact]
    public void PagamentoPodeSerDistribuidoEntreVendasEPreservarCredito()
    {
        using var app = new AplicacaoTeste();
        var clienteId = app.Clientes.Cadastrar("Cliente Fictício");
        var primeira = app.Vendas.Registrar(clienteId, 1_000, 6_000, new DateOnly(2026, 10, 1));
        var segunda = app.Vendas.Registrar(clienteId, 1_000, 4_000, new DateOnly(2026, 10, 2));

        app.Pagamentos.Registrar(clienteId, 12_000, new DateOnly(2026, 10, 5),
        [
            new AlocacaoSolicitada(primeira, 6_000),
            new AlocacaoSolicitada(segunda, 4_000)
        ]);

        Assert.All(app.Vendas.Listar(clienteId), venda => Assert.Equal(0, venda.SaldoCentavos));
        Assert.Equal(2_000, Assert.Single(app.Pagamentos.Listar(clienteId)).CreditoCentavos);
        Assert.Empty(app.Vendas.Listar(clienteId, somentePendentes: true));
    }

    [Fact]
    public void EstornoReabreVendasSemApagarHistorico()
    {
        using var app = new AplicacaoTeste();
        var clienteId = app.Clientes.Cadastrar("Cliente Fictício");
        var vendaId = app.Vendas.Registrar(clienteId, 1_000, 10_000, new DateOnly(2026, 10, 5));
        var pagamentoId = app.Pagamentos.Registrar(clienteId, 10_000, new DateOnly(2026, 10, 5),
            [new AlocacaoSolicitada(vendaId, 10_000)]);

        app.Pagamentos.Estornar(pagamentoId, "Lançamento incorreto");

        var venda = Assert.Single(app.Vendas.Listar(clienteId));
        Assert.Equal(0, venda.RecebidoCentavos);
        Assert.Equal(10_000, venda.SaldoCentavos);
        var pagamento = Assert.Single(app.Pagamentos.Listar(clienteId));
        Assert.NotNull(pagamento.EstornadoEm);
        Assert.Equal(10_000, pagamento.AlocadoCentavos);
    }

    [Fact]
    public void CancelamentoEInativacaoRespeitamRegrasOperacionais()
    {
        using var app = new AplicacaoTeste();
        var clienteId = app.Clientes.Cadastrar("Cliente Fictício");
        var vendaId = app.Vendas.Registrar(clienteId, 1_000, 10_000, new DateOnly(2026, 10, 5));

        app.Vendas.Cancelar(vendaId, "Venda lançada errada");
        Assert.True(Assert.Single(app.Vendas.Listar(clienteId)).Cancelada);

        app.Clientes.DefinirAtivo(clienteId, false);
        Assert.Empty(app.Clientes.Listar());
        Assert.Single(app.Clientes.Listar(incluirInativos: true));
        Assert.ThrowsAny<Exception>(() =>
            app.Vendas.Registrar(clienteId, 1_000, 10_000, new DateOnly(2026, 10, 5)));
        Assert.ThrowsAny<Exception>(() =>
            app.Pagamentos.Registrar(clienteId, 1_000, new DateOnly(2026, 10, 5), []));
    }

    [Fact]
    public void DistribuicaoInvalidaNaoCriaPagamentoParcial()
    {
        using var app = new AplicacaoTeste();
        var clienteId = app.Clientes.Cadastrar("Cliente Fictício");
        var vendaId = app.Vendas.Registrar(clienteId, 1_000, 10_000, new DateOnly(2026, 10, 5));

        Assert.Throws<ArgumentException>(() => app.Pagamentos.Registrar(
            clienteId, 5_000, new DateOnly(2026, 10, 5),
            [new AlocacaoSolicitada(vendaId, 5_001)]));

        Assert.Empty(app.Pagamentos.Listar(clienteId));
        Assert.Equal(10_000, Assert.Single(app.Vendas.Listar(clienteId)).SaldoCentavos);
    }

    private sealed class AplicacaoTeste : IDisposable
    {
        private readonly string _diretorio = Path.Combine(
            Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));

        public ClienteService Clientes { get; }
        public VendaService Vendas { get; }
        public PagamentoService Pagamentos { get; }
        public ResumoRepository Resumos { get; }

        public AplicacaoTeste()
        {
            Directory.CreateDirectory(_diretorio);
            var banco = new BancoSqlite(Path.Combine(_diretorio, "test.sqlite"));
            new InicializadorBanco(banco, Path.Combine(_diretorio, "backups")).Inicializar();
            var clientes = new ClienteRepository(banco);
            var vendas = new VendaRepository(banco);
            var pagamentos = new PagamentoRepository(banco);
            Clientes = new ClienteService(clientes);
            Vendas = new VendaService(banco, vendas, pagamentos);
            Pagamentos = new PagamentoService(banco, pagamentos, vendas);
            Resumos = new ResumoRepository(banco);
        }

        public void Dispose()
        {
            if (Directory.Exists(_diretorio))
                Directory.Delete(_diretorio, recursive: true);
        }
    }
}
