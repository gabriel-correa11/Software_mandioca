using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.Tests;

public sealed class FundacaoTests
{
    [Theory]
    [InlineData(500, 101, 51)]
    [InlineData(1500, 101, 152)]
    [InlineData(1000, 100, 100)]
    public void TotalArredondaUmaVezAoFinal(long gramas, long preco, long esperado)
        => Assert.Equal(esperado, CalculoFinanceiro.TotalCentavos(gramas, preco));

    [Fact]
    public void TotalMenorQueUmCentavoNaoEValido()
        => Assert.Throws<ArgumentOutOfRangeException>(() => CalculoFinanceiro.TotalCentavos(1, 1));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1000, 0)]
    [InlineData(-1, 100)]
    [InlineData(1000, -1)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public void TotalRejeitaEntradasInvalidasEOversized(long gramas, long preco)
        => Assert.Throws<ArgumentOutOfRangeException>(() => CalculoFinanceiro.TotalCentavos(gramas, preco));

    [Fact]
    public void MigrationV1CriaSchemaEReaberturaNaoAlteraVersao()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        ambiente.Inicializador.Inicializar();
        using var connection = ambiente.Banco.AbrirConexao();
        Assert.Equal(1L, Escalar(connection, "PRAGMA user_version;"));
        Assert.Equal(4L, Escalar(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Cliente','Venda','Pagamento','AlocacaoPagamento');"));
        Assert.Equal(1L, Escalar(connection, "PRAGMA foreign_keys;"));
    }

    [Fact]
    public void BackupSqlitePreservaSchemaEDados()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using (var db = ambiente.Banco.AbrirConexao())
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");

        var path = ambiente.Inicializador.CriarBackupAntesDeMigrar();
        var backup = new BancoSqlite(path);
        using var copy = backup.AbrirConexao();
        Assert.Equal(1L, Escalar(copy, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal(1L, Escalar(copy, "PRAGMA user_version;"));
    }

    [Fact]
    public void SchemaStrictRejeitaRealEmCamposInteiros()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,0.5,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,10.5,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
    }

    [Fact]
    public void ConstraintsNomeadasRejeitamValoresLocaisInvalidos()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        var nome = Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('  ','Manual','2026-09-20T10:00:00.0000000Z');"));
        Assert.Contains("CK_Cliente_Nome", nome.Message);
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");
        var valor = Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,0,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
        Assert.Contains("CK_Pagamento_Valor", valor.Message);
    }

    [Fact]
    public void ClientePodeSerInativadoEReativadoSemPerderHistorico()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");
        Assert.Equal(1L, Escalar(db, "SELECT Ativo FROM Cliente WHERE Id=1;"));

        Executar(db, "UPDATE Cliente SET Ativo=0 WHERE Id=1;");
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Cliente WHERE Id=1;"));
        var venda = Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
        Assert.Contains("cliente inativo", venda.Message);
        var pagamento = Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
        Assert.Contains("cliente inativo", pagamento.Message);

        Executar(db, "UPDATE Cliente SET Ativo=1 WHERE Id=1;");
        Executar(db, "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Venda WHERE ClienteId=1;"));
    }

    [Fact]
    public void FormatoTemporalNormalizaUtcEValidaPersistencia()
    {
        var instante = new DateTimeOffset(2026, 9, 20, 6, 30, 0, TimeSpan.FromHours(-4));
        Assert.Equal("2026-09-20", FormatoTemporal.FormatarDataOperacao(new DateOnly(2026, 9, 20)));
        Assert.Equal("2026-09-20T10:30:00.0000000Z", FormatoTemporal.FormatarInstanteUtc(instante));
        Assert.True(FormatoTemporal.DataOperacaoValida("2026-09-20"));
        Assert.True(FormatoTemporal.InstanteUtcValido("2026-09-20T10:30:00.0000000Z"));
        Assert.False(FormatoTemporal.DataOperacaoValida("20/09/2026"));
        Assert.False(FormatoTemporal.InstanteUtcValido("2026-09-20T06:30:00.0000000-04:00"));
    }

    [Fact]
    public void SchemaRejeitaRepresentacoesTemporaisNaoNormalizadas()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00Z');"));
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'20/09/2026','2026-09-20T10:00:00.0000000Z','Manual');"));
    }

    [Fact]
    public void BancoVersaoFuturaDesconhecidoOuSchemaIncompletoERecusado()
    {
        using var futuro = new BancoTeste();
        using (var db = futuro.Banco.AbrirConexao())
            Executar(db, "PRAGMA user_version=2;");
        Assert.Throws<InvalidOperationException>(() => futuro.Inicializador.Inicializar());

        using var desconhecido = new BancoTeste();
        using (var db = desconhecido.Banco.AbrirConexao())
            Executar(db, "CREATE TABLE OutraTabela(Id INTEGER PRIMARY KEY) STRICT;");
        Assert.Throws<InvalidOperationException>(() => desconhecido.Inicializador.Inicializar());

        using var incompleto = new BancoTeste();
        incompleto.Inicializador.Inicializar();
        using (var db = incompleto.Banco.AbrirConexao())
            Executar(db, "DROP TRIGGER TR_Alocacao_Validar;");
        Assert.Throws<InvalidDataException>(() => incompleto.Inicializador.Inicializar());
    }

    [Fact]
    public void BancoCorrompidoOuComForeignKeyInvalidaERecusado()
    {
        using var corrompido = new BancoTeste();
        File.WriteAllText(corrompido.Banco.CaminhoBanco, "isto nao e sqlite");
        Assert.Throws<SqliteException>(() => corrompido.Inicializador.Inicializar());

        using var inconsistente = new BancoTeste();
        inconsistente.Inicializador.Inicializar();
        using (var db = inconsistente.Banco.AbrirConexao())
        {
            Executar(db, "PRAGMA foreign_keys=OFF;");
            Executar(db, "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (999,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        }
        Assert.Throws<InvalidDataException>(() => inconsistente.Inicializador.Inicializar());
    }

    [Fact]
    public void AlocacaoExigeMesmoClienteELimitesDoPagamentoEDaVenda()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        InserirFatos(db);
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,60,'2026-09-20T10:00:00.0000000Z');");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,2,1,'2026-09-20T10:00:00.0000000Z');"));
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,41,'2026-09-20T10:00:00.0000000Z');"));
        Executar(db, "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (2,1,41,'2026-09-20T10:00:00.0000000Z');"));
        Assert.Equal(40, CalculoFinanceiro.SaldoCentavos(100, 60));
        Assert.Equal(40L, Escalar(db,
            "SELECT p.ValorCentavos - COALESCE(SUM(a.ValorCentavos),0) FROM Pagamento p LEFT JOIN AlocacaoPagamento a ON a.PagamentoId=p.Id WHERE p.Id=1;"));
    }

    [Fact]
    public void DoisPagamentosQuitamVendaETerceiraAlocacaoFalha()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        InserirFatos(db);
        Executar(db, "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,40,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual'),(1,1,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,60,'2026-09-20T10:00:00.0000000Z'),(2,1,40,'2026-09-20T10:00:00.0000000Z');");
        Assert.Equal(100L, RecebidoValido(db, 1));
        Assert.Equal(0, CalculoFinanceiro.SaldoCentavos(100, 100));
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (3,1,1,'2026-09-20T10:00:00.0000000Z');"));
    }

    [Fact]
    public void EstornoMantemHistoricoEReabreSaldo()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        InserirFatos(db);
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,60,'2026-09-20T10:00:00.0000000Z');");
        Executar(db, "UPDATE Pagamento SET EstornadoEm='2026-09-20T11:00:00.0000000Z', MotivoEstorno='Registro incorreto' WHERE Id=1;");
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM AlocacaoPagamento;"));
        Assert.Equal(0L, Escalar(db,
            "SELECT COALESCE(SUM(a.ValorCentavos),0) FROM AlocacaoPagamento a JOIN Pagamento p ON p.Id=a.PagamentoId WHERE a.VendaId=1 AND p.EstornadoEm IS NULL;"));
        Assert.Equal(0L, Escalar(db,
            "SELECT COALESCE(SUM(p.ValorCentavos-a.Alocado),0) FROM Pagamento p JOIN (SELECT PagamentoId,SUM(ValorCentavos) Alocado FROM AlocacaoPagamento GROUP BY PagamentoId) a ON a.PagamentoId=p.Id WHERE p.ClienteId=1 AND p.EstornadoEm IS NULL;"));
        var estornado = Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,1,'2026-09-20T12:00:00.0000000Z');"));
        Assert.Contains("pagamento estornado", estornado.Message);
        Assert.Throws<SqliteException>(() => Executar(db, "DELETE FROM Pagamento WHERE Id=1;"));
    }

    [Fact]
    public void CancelamentoExigeAusenciaDeAlocacaoValida()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        InserirFatos(db);
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,60,'2026-09-20T10:00:00.0000000Z');");
        Assert.Throws<SqliteException>(() => Executar(db,
            "UPDATE Venda SET CanceladaEm='2026-09-20T11:00:00.0000000Z', MotivoCancelamento='Erro' WHERE Id=1;"));
        Executar(db, "UPDATE Pagamento SET EstornadoEm='2026-09-20T11:00:00.0000000Z', MotivoEstorno='Erro' WHERE Id=1;");
        Executar(db, "UPDATE Venda SET CanceladaEm='2026-09-20T12:00:00.0000000Z', MotivoCancelamento='Erro' WHERE Id=1;");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,1,'2026-09-20T13:00:00.0000000Z');"));
        Assert.Throws<SqliteException>(() => Executar(db,
            "UPDATE Venda SET QuantidadeGramas=2000 WHERE Id=1;"));
    }

    [Fact]
    public void TransacaoPodeDesfazerVendaEPagamentoInicial()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Ficticio','Manual','2026-09-20T10:00:00.0000000Z');");
        using (var transaction = db.BeginTransaction())
        {
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');";
            cmd.ExecuteNonQuery();
            transaction.Rollback();
        }
        Assert.Equal(0L, Escalar(db, "SELECT COUNT(*) FROM Venda;"));
        Assert.Equal(0L, Escalar(db, "SELECT COUNT(*) FROM Pagamento;"));
    }

    [Fact]
    public void ChavesEstrangeirasEExclusoesFisicasSaoProtegidas()
    {
        using var ambiente = new BancoTeste();
        ambiente.Inicializador.Inicializar();
        using var db = ambiente.Banco.AbrirConexao();
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (999,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
        InserirFatos(db);
        Assert.Throws<SqliteException>(() => Executar(db, "DELETE FROM Cliente WHERE Id=1;"));
        Assert.Throws<SqliteException>(() => Executar(db, "DELETE FROM Venda WHERE Id=1;"));
    }

    private static void InserirFatos(SqliteConnection db)
    {
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Cliente Um','Manual','2026-09-20T10:00:00.0000000Z'),('Cliente Dois','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual'),(2,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
    }

    private static void Executar(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long Escalar(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private static long RecebidoValido(SqliteConnection db, long vendaId)
    {
        using var command = db.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(a.ValorCentavos),0) FROM AlocacaoPagamento a JOIN Pagamento p ON p.Id=a.PagamentoId WHERE a.VendaId=$vendaId AND p.EstornadoEm IS NULL;";
        command.Parameters.AddWithValue("$vendaId", vendaId);
        return (long)command.ExecuteScalar()!;
    }

    private sealed class BancoTeste : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));
        public BancoSqlite Banco { get; }
        public InicializadorBanco Inicializador { get; }

        public BancoTeste()
        {
            Directory.CreateDirectory(_directory);
            Banco = new BancoSqlite(Path.Combine(_directory, "test.sqlite"));
            Inicializador = new InicializadorBanco(Banco, Path.Combine(_directory, "backups"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
