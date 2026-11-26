using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;

namespace MandiocaCaipira.Tests;

public sealed class FluxoCaixaTests : IDisposable
{
    private readonly string _dbPath;
    private readonly BancoSqlite _banco;
    private readonly MovimentoCaixaRepository _movRepo;
    private readonly ContaCaixaRepository _contaRepo;
    private readonly CategoriaMovimentoRepository _catRepo;
    private readonly ConferenciaSaldoRepository _confRepo;
    private readonly MovimentoCaixaService _movService;
    private readonly ConferenciaService _confService;

    public FluxoCaixaTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fluxo-test-{Guid.NewGuid()}.sqlite");
        _banco = new BancoSqlite(_dbPath);
        var backupDir = Path.Combine(Path.GetTempPath(), $"backup-test-{Guid.NewGuid()}");
        new InicializadorBanco(_banco, backupDir).Inicializar();

        _movRepo = new MovimentoCaixaRepository(_banco);
        _contaRepo = new ContaCaixaRepository(_banco);
        _catRepo = new CategoriaMovimentoRepository(_banco);
        _confRepo = new ConferenciaSaldoRepository(_banco);
        _movService = new MovimentoCaixaService(_banco, _movRepo, _contaRepo, _catRepo);
        _confService = new ConferenciaService(_movRepo, _confRepo, _contaRepo);
    }

    public void Dispose()
    {
        BancoSqlite.LiberarConexoes(_dbPath);
        try { File.Delete(_dbPath); } catch { }
    }

    private long ContaBanco => _contaRepo.Listar().First(c => c.Nome == "Banco").Id;
    private long ContaDinheiro => _contaRepo.Listar().First(c => c.Nome == "Dinheiro").Id;

    private long CatEntrada(string nome) =>
        _catRepo.ObterPorNomeETipo(nome, TipoMovimento.Entrada)!.Id;

    private long CatSaida(string nome) =>
        _catRepo.ObterPorNomeETipo(nome, TipoMovimento.Saida)!.Id;

    [Fact]
    public void Caso_real_1000_menos_600_igual_400()
    {
        var banco = ContaBanco;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 100000, "Venda a vista cliente X", null);
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Funcionario"), 30000, "Diaria Joao", "Joao");
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Combustivel"), 20000, "Diesel", null);
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Energia e agua"), 10000, "Conta de luz", null);

        Assert.Equal(40000, _movService.SaldoConta(banco));
    }

    [Fact]
    public void Movimento_cancelado_nao_entra_no_total()
    {
        var banco = ContaBanco;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 50000, "Pagamento cliente", null);
        var id = _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Funcionario"), 20000, "Diaria", null);

        _movService.Cancelar(id, "Lancamento errado");

        Assert.Equal(50000, _movService.SaldoConta(banco));
    }

    [Fact]
    public void Saldo_por_conta_e_total_batem()
    {
        var banco = ContaBanco;
        var dinheiro = ContaDinheiro;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 80000, "PIX recebido", null);
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), dinheiro,
            CatEntrada("Recebimento de cliente"), 20000, "Dinheiro recebido", null);
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Combustivel"), 15000, "Diesel", null);

        Assert.Equal(65000, _movService.SaldoConta(banco));
        Assert.Equal(20000, _movService.SaldoConta(dinheiro));
        Assert.Equal(85000, _movService.SaldoTotal());
    }

    [Fact]
    public void Aporte_nao_entra_no_resultado_operacional()
    {
        var banco = ContaBanco;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 50000, "Venda", null);
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Aporte do dono"), 100000, "Capital inicial", null);
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Funcionario"), 20000, "Diaria", null);

        var movimentos = _movService.Listar(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 1));
        var resultado = ResumoCaixaService.ResultadoOperacional(movimentos,
            new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 1));

        Assert.Equal(30000, resultado); // 50000 - 20000, sem o aporte
    }

    [Fact]
    public void Periodo_sem_movimento_retorna_zero()
    {
        var movimentos = _movService.Listar(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
        var (entradas, saidas, saldo) = ResumoCaixaService.TotalPeriodo(movimentos,
            new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        Assert.Equal(0, entradas);
        Assert.Equal(0, saidas);
        Assert.Equal(0, saldo);
    }

    [Fact]
    public void Valor_zero_rejeitado()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
                ContaBanco, CatEntrada("Recebimento de cliente"), 0, "Teste", null));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void Valor_negativo_rejeitado()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
                ContaBanco, CatEntrada("Recebimento de cliente"), -100, "Teste", null));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void Descricao_vazia_rejeitada()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
                ContaBanco, CatEntrada("Recebimento de cliente"), 1000, "", null));
        Assert.Contains("Descreva", ex.Message);
    }

    [Fact]
    public void Entrada_com_categoria_de_saida_rejeitada()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
                ContaBanco, CatSaida("Funcionario"), 1000, "Teste", null));
        Assert.Contains("incompatível", ex.Message);
    }

    [Fact]
    public void Saida_com_categoria_de_entrada_rejeitada()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1),
                ContaBanco, CatEntrada("Recebimento de cliente"), 1000, "Teste", null));
        Assert.Contains("incompatível", ex.Message);
    }

    [Fact]
    public void Data_futura_rejeitada()
    {
        var amanha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var ex = Assert.Throws<ArgumentException>(() =>
            _movService.Registrar(TipoMovimento.Entrada, amanha,
                ContaBanco, CatEntrada("Recebimento de cliente"), 1000, "Teste", null));
        Assert.Contains("futura", ex.Message);
    }

    [Fact]
    public void Cancelamento_exige_motivo()
    {
        var id = _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
            ContaBanco, CatEntrada("Recebimento de cliente"), 5000, "Teste", null);

        var ex = Assert.Throws<ArgumentException>(() => _movService.Cancelar(id, "ab"));
        Assert.Contains("motivo", ex.Message);
    }

    [Fact]
    public void Conferencia_diferenca_calculada_corretamente()
    {
        var banco = ContaBanco;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 100000, "Venda", null);
        _movService.Registrar(TipoMovimento.Saida, new DateOnly(2024, 6, 1), banco,
            CatSaida("Combustivel"), 30000, "Diesel", null);

        // Sistema: 100000 - 30000 = 70000
        // App banco mostra 65000 (faltam 50 reais)
        var (saldoSistema, diferenca) = _confService.Calcular(banco, new DateOnly(2024, 6, 1), 65000);

        Assert.Equal(70000, saldoSistema);
        Assert.Equal(-5000, diferenca); // 65000 - 70000
    }

    [Fact]
    public void Conferencia_gravada_nao_altera_movimentos()
    {
        var banco = ContaBanco;
        _movService.Registrar(TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco,
            CatEntrada("Recebimento de cliente"), 50000, "Pagamento", null);

        var saldoAntes = _movService.SaldoConta(banco);
        _confService.Salvar(banco, new DateOnly(2024, 6, 1), 45000, "Teste");
        var saldoDepois = _movService.SaldoConta(banco);

        Assert.Equal(saldoAntes, saldoDepois);
    }

    [Fact]
    public void Contas_e_categorias_seed_existem()
    {
        var contas = _contaRepo.Listar();
        Assert.Contains(contas, c => c.Nome == "Dinheiro");
        Assert.Contains(contas, c => c.Nome == "Banco");

        var catEntrada = _catRepo.Listar(TipoMovimento.Entrada);
        Assert.Contains(catEntrada, c => c.Nome == "Recebimento de cliente");
        Assert.Contains(catEntrada, c => c.Nome == "Aporte do dono");

        var catSaida = _catRepo.Listar(TipoMovimento.Saida);
        Assert.Contains(catSaida, c => c.Nome == "Funcionario");
        Assert.Contains(catSaida, c => c.Nome == "Combustivel");
        Assert.Contains(catSaida, c => c.Nome == "Compra de mandioca");
    }

    [Fact]
    public void Nao_cria_movimento_automatico_duplicado()
    {
        var banco = ContaBanco;
        var cat = CatEntrada("Recebimento de cliente");

        using var conn = _banco.AbrirConexao();
        using var tx = conn.BeginTransaction();
        _movRepo.Inserir(conn, tx, TipoMovimento.Entrada, new DateOnly(2024, 6, 1),
            banco, cat, 5000, "Pagamento 1", null, OrigemMovimento.PagamentoCliente, 42,
            DateTimeOffset.UtcNow);
        tx.Commit();

        Assert.True(_movRepo.ExisteMovimentoOrigem(OrigemMovimento.PagamentoCliente, 42));

        using var conn2 = _banco.AbrirConexao();
        using var tx2 = conn2.BeginTransaction();
        var id = _movService.RegistrarAutomatico(conn2, tx2,
            TipoMovimento.Entrada, new DateOnly(2024, 6, 1), banco, cat,
            5000, "Pagamento 1 duplicado", null, OrigemMovimento.PagamentoCliente, 42);
        tx2.Commit();

        Assert.Equal(-1, id);
    }
}
