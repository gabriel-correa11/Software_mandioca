using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.Tests;

public sealed class CompraServiceTests
{
    [Fact]
    public void RegistroGravaSnapshotDosPadroes()
    {
        using var app = new CompraApp();

        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            340_000, 226_130, null, null, null);

        var compra = Assert.Single(app.Compras.Listar());
        Assert.Equal(id, compra.Id);
        Assert.Equal(3000, compra.SnapshotPerdaBp);
        Assert.Equal(21_000, compra.SnapshotGramasPorSaco);
        Assert.Equal(30_000, compra.SnapshotTamanhoSacoGramas);
    }

    [Fact]
    public void CancelamentoExigeMotivo()
    {
        using var app = new CompraApp();
        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            100_000, 70_000, null, null, null);

        Assert.Throws<ArgumentException>(() => app.Compras.Cancelar(id, "ab"));
    }

    [Fact]
    public void CancelamentoComMotivoFunciona()
    {
        using var app = new CompraApp();
        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            100_000, 70_000, null, null, null);

        app.Compras.Cancelar(id, "Lote errado");

        var compra = Assert.Single(app.Compras.Listar());
        Assert.Equal(StatusCompra.Cancelada, compra.Status);
        Assert.NotNull(compra.CanceladaEm);
        Assert.Equal("Lote errado", compra.MotivoCancelamento);
    }

    [Fact]
    public void CompraCanceladaNaoAceitaSegundoCancelamento()
    {
        using var app = new CompraApp();
        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            100_000, 70_000, null, null, null);
        app.Compras.Cancelar(id, "Primeiro cancelamento");

        Assert.ThrowsAny<Exception>(() => app.Compras.Cancelar(id, "Segundo cancelamento"));
    }

    [Fact]
    public void NadaEApagadoDoBanco()
    {
        using var app = new CompraApp();
        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            100_000, 70_000, null, null, null);
        app.Compras.Cancelar(id, "Motivo qualquer");

        using var conn = app.Banco.AbrirConexao();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CompraMandioca;";
        Assert.Equal(1L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void MudancaDePadraoNaoAlteraCompraAntiga()
    {
        using var app = new CompraApp();

        var id = app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
            100_000, 70_000, null, null, null);

        // Insere nova configuracao com percentual diferente
        app.Configuracoes.Inserir(new DateOnly(2026, 10, 2), 4000, 22_000, 25_000);

        // Compra antiga mantém snapshot original
        var compra = Assert.Single(app.Compras.Listar());
        Assert.Equal(3000, compra.SnapshotPerdaBp);
        Assert.Equal(21_000, compra.SnapshotGramasPorSaco);
        Assert.Equal(30_000, compra.SnapshotTamanhoSacoGramas);
    }

    [Fact]
    public void PesoCascadoMaiorQueBrutoRejeita()
    {
        using var app = new CompraApp();
        Assert.Throws<ArgumentException>(() =>
            app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
                100_000, 100_001, null, null, null));
    }

    [Fact]
    public void DataFuturaRejeita()
    {
        using var app = new CompraApp();
        Assert.Throws<ArgumentException>(() =>
            app.Compras.Registrar(app.FornecedorId,
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                100_000, 70_000, null, null, null));
    }

    [Fact]
    public void FornecedorInativoRejeita()
    {
        using var app = new CompraApp();
        app.FornecedorRepo.AlternarAtivo(app.FornecedorId);

        Assert.Throws<ArgumentException>(() =>
            app.Compras.Registrar(app.FornecedorId, new DateOnly(2026, 10, 1),
                100_000, 70_000, null, null, null));
    }

    internal sealed class CompraApp : IDisposable
    {
        private readonly string _diretorio = Path.Combine(
            Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));

        public BancoSqlite Banco { get; }
        public CompraService Compras { get; }
        public FornecedorRepository FornecedorRepo { get; }
        public ConfiguracaoRendimentoRepository Configuracoes { get; }
        public long FornecedorId { get; }

        public CompraApp()
        {
            Directory.CreateDirectory(_diretorio);
            Banco = new BancoSqlite(Path.Combine(_diretorio, "test.sqlite"));
            new InicializadorBanco(Banco, Path.Combine(_diretorio, "backups")).Inicializar();
            FornecedorRepo = new FornecedorRepository(Banco);
            Configuracoes = new ConfiguracaoRendimentoRepository(Banco);
            var compraRepo = new CompraRepository(Banco);
            Compras = new CompraService(Banco, compraRepo, FornecedorRepo, Configuracoes);
            FornecedorId = FornecedorRepo.Inserir("Fornecedor Teste", null, null);
        }

        public void Dispose()
        {
            if (Directory.Exists(_diretorio))
                Directory.Delete(_diretorio, recursive: true);
        }
    }
}
