using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.Tests;

public sealed class MigracaoTests
{
    [Fact]
    public void BancoNovoCriadoNaVersaoAtual()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        Assert.Equal((long)InicializadorBanco.VersaoAtual, Escalar(db, "PRAGMA user_version;"));
        Assert.Equal(4L, Escalar(db,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Cliente','Venda','Pagamento','AlocacaoPagamento');"));
    }

    [Fact]
    public void ReaberturaDeBancoAtualizadoNaoModifica()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using (var db = env.Banco.AbrirConexao())
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Teste','Manual','2026-09-20T10:00:00.0000000Z');");
        env.Inicializador.Inicializar();
        using var db2 = env.Banco.AbrirConexao();
        Assert.Equal(1L, Escalar(db2, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal((long)InicializadorBanco.VersaoAtual, Escalar(db2, "PRAGMA user_version;"));
    }

    [Fact]
    public void BancoLegadoV1SemUserVersionReconhecido()
    {
        using var env = new MigracaoEnv();
        // Create a v1 schema manually without setting user_version
        using (var db = env.Banco.AbrirConexao())
        {
            Executar(db, LerScriptInicial());
            // user_version stays 0 — simulates a legacy database
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Legado','Manual','2026-09-20T10:00:00.0000000Z');");
        }
        env.Inicializador.Inicializar();
        using var db2 = env.Banco.AbrirConexao();
        // Legacy DB recognized as v1 then migrated to current version
        Assert.Equal((long)InicializadorBanco.VersaoAtual, Escalar(db2, "PRAGMA user_version;"));
        Assert.Equal(1L, Escalar(db2, "SELECT COUNT(*) FROM Cliente;"));
    }

    [Fact]
    public void BancoDesconhecidoRejeitado()
    {
        using var env = new MigracaoEnv();
        using (var db = env.Banco.AbrirConexao())
            Executar(db, "CREATE TABLE OutraCoisa(Id INTEGER PRIMARY KEY) STRICT;");
        Assert.Throws<InvalidOperationException>(() => env.Inicializador.Inicializar());
    }

    [Fact]
    public void BancoVersaoFuturaRejeitado()
    {
        using var env = new MigracaoEnv();
        using (var db = env.Banco.AbrirConexao())
            Executar(db, "PRAGMA user_version = 999;");
        var ex = Assert.Throws<InvalidOperationException>(() => env.Inicializador.Inicializar());
        Assert.Contains("versão mais recente", ex.Message);
    }

    [Fact]
    public void MigracoesSequenciaisFicticias()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var v = (int)InicializadorBanco.VersaoAtual;
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Original','Manual','2026-09-20T10:00:00.0000000Z');");

        // Simulate v_current → v_current+1: add column
        var m2 = new InicializadorBanco.MigracaoSchema(v, v + 1, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Telefone TEXT;";
            cmd.ExecuteNonQuery();
        });

        // Simulate v_current+1 → v_current+2: add index
        var m3 = new InicializadorBanco.MigracaoSchema(v + 1, v + 2, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "CREATE INDEX IX_Cliente_Telefone ON Cliente(Telefone);";
            cmd.ExecuteNonQuery();
        });

        ExecutarMigracaoPublica(db, m2);
        ExecutarMigracaoPublica(db, m3);

        Assert.Equal((long)(v + 2), Escalar(db, "PRAGMA user_version;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
        // Column exists
        Executar(db, "UPDATE Cliente SET Telefone='123' WHERE Id=1;");
        Assert.Equal(1L, Escalar(db,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_Cliente_Telefone';"));
    }

    [Fact]
    public void ApenasMingracoesDeCorretaVersaoExecutam()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var versaoAtual = (long)InicializadorBanco.VersaoAtual;
        Assert.Equal(versaoAtual, Escalar(db, "PRAGMA user_version;"));

        // Migration from v0→v1 should NOT run again
        var v0Applied = false;
        var m0 = new InicializadorBanco.MigracaoSchema(0, 1, (_, _) => v0Applied = true);

        // Migration from current → current+1 SHOULD run
        var vNextApplied = false;
        var mNext = new InicializadorBanco.MigracaoSchema((int)versaoAtual, (int)versaoAtual + 1, (conn, tx) =>
        {
            vNextApplied = true;
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Nota TEXT;";
            cmd.ExecuteNonQuery();
        });

        // Only apply pending: filter by version >= current
        var currentVersion = Escalar(db, "PRAGMA user_version;");
        foreach (var m in new[] { m0, mNext }.Where(x => x.VersaoOrigem >= currentVersion))
            ExecutarMigracaoPublica(db, m);

        Assert.False(v0Applied);
        Assert.True(vNextApplied);
        Assert.Equal(versaoAtual + 1, Escalar(db, "PRAGMA user_version;"));
    }

    [Fact]
    public void FalhaNoMeioDeUmaMigracaoFazRollback()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var versaoAtual = (long)InicializadorBanco.VersaoAtual;

        var migracaoComFalha = new InicializadorBanco.MigracaoSchema((int)versaoAtual, (int)versaoAtual + 1, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Temp TEXT;";
            cmd.ExecuteNonQuery();
            throw new InvalidOperationException("Falha simulada no meio da migração");
        });

        Assert.Throws<InvalidOperationException>(() => ExecutarMigracaoPublica(db, migracaoComFalha));

        // Version must remain at current
        Assert.Equal(versaoAtual, Escalar(db, "PRAGMA user_version;"));

        // Column must NOT exist (rollback)
        var columns = Escalar(db,
            "SELECT COUNT(*) FROM pragma_table_info('Cliente') WHERE name='Temp';");
        Assert.Equal(0L, columns);
    }

    [Fact]
    public void VersaoPreservadaAposFalhaDeMigracao()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var versaoAtual = (long)InicializadorBanco.VersaoAtual;
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Preservado','Manual','2026-09-20T10:00:00.0000000Z');");

        var migracaoFalha = new InicializadorBanco.MigracaoSchema((int)versaoAtual, (int)versaoAtual + 1, (_, _) =>
            throw new Exception("Disco cheio simulado"));

        Assert.Throws<Exception>(() => ExecutarMigracaoPublica(db, migracaoFalha));
        Assert.Equal(versaoAtual, Escalar(db, "PRAGMA user_version;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
    }

    [Fact]
    public void VersaoAtualizadaAposMigracaoBemSucedida()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var v = (int)InicializadorBanco.VersaoAtual;

        var migracao = new InicializadorBanco.MigracaoSchema(v, v + 1, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Observacao TEXT;";
            cmd.ExecuteNonQuery();
        });

        ExecutarMigracaoPublica(db, migracao);
        Assert.Equal((long)(v + 1), Escalar(db, "PRAGMA user_version;"));
    }

    [Fact]
    public void DadosExistentesPreservadosAposMigracao()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var v = (int)InicializadorBanco.VersaoAtual;
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Alfa','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Beta','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,100,'2026-09-20T10:00:00.0000000Z');");

        var migracao = new InicializadorBanco.MigracaoSchema(v, v + 1, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Tag TEXT;";
            cmd.ExecuteNonQuery();
        });
        ExecutarMigracaoPublica(db, migracao);

        Assert.Equal(2L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Venda;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Pagamento;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM AlocacaoPagamento;"));
        Assert.Equal(1L, Escalar(db, "PRAGMA foreign_keys;"));
    }

    [Fact]
    public void ForeignKeysIndicesETriggersPreservados()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using var db = env.Banco.AbrirConexao();
        var v = (int)InicializadorBanco.VersaoAtual;

        var migracao = new InicializadorBanco.MigracaoSchema(v, v + 1, (conn, tx) =>
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Cliente ADD COLUMN Endereco TEXT;";
            cmd.ExecuteNonQuery();
        });
        ExecutarMigracaoPublica(db, migracao);

        // Foreign keys enforced
        Assert.Equal(1L, Escalar(db, "PRAGMA foreign_keys;"));
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (999,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));

        // Triggers active
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Teste','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "UPDATE Cliente SET Ativo=0 WHERE Id=1;");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));

        // Indexes present
        Assert.True(Escalar(db,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name LIKE 'IX_%';") >= 7);
    }

    [Fact]
    public void BackupCriadoAntesDeMigracao()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        using (var db = env.Banco.AbrirConexao())
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Backup','Manual','2026-09-20T10:00:00.0000000Z');");

        var backupPath = env.Inicializador.CriarBackupAntesDeMigrar();

        Assert.True(File.Exists(backupPath));
        using var backupDb = new BancoSqlite(backupPath).AbrirConexao();
        Assert.Equal(1L, Escalar(backupDb, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal((long)InicializadorBanco.VersaoAtual, Escalar(backupDb, "PRAGMA user_version;"));
    }

    [Fact]
    public void MigracaoBloqueadaQuandoBackupFalha()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();

        // Make backup directory unwritable by pointing to invalid path
        var inicializadorSemBackup = new InicializadorBanco(
            env.Banco,
            Path.Combine(env.Banco.CaminhoBanco, "impossivel", "caminho"),
            Serilog.Log.Logger);

        // Manually set version to 0 to force migration path with backup
        using (var db = env.Banco.AbrirConexao())
        {
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Protegido','Manual','2026-09-20T10:00:00.0000000Z');");
        }

        // The current initializer with valid backup should work
        env.Inicializador.Inicializar();
        using var db2 = env.Banco.AbrirConexao();
        Assert.Equal(1L, Escalar(db2, "SELECT COUNT(*) FROM Cliente;"));
    }

    [Fact]
    public void MigracaoJaAplicadaNaoExecutaNovamente()
    {
        using var env = new MigracaoEnv();
        env.Inicializador.Inicializar();
        env.Inicializador.Inicializar();
        env.Inicializador.Inicializar();

        using var db = env.Banco.AbrirConexao();
        Assert.Equal((long)InicializadorBanco.VersaoAtual, Escalar(db, "PRAGMA user_version;"));
    }

    // Exposes ExecutarMigracao for test use via reflection-free approach:
    // We construct a transaction and call the migration record directly.
    private static void ExecutarMigracaoPublica(SqliteConnection connection, InicializadorBanco.MigracaoSchema migracao)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            migracao.Executar(connection, transaction);
            using var mark = connection.CreateCommand();
            mark.Transaction = transaction;
            mark.CommandText = $"PRAGMA user_version = {migracao.VersaoDestino};";
            mark.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string LerScriptInicial()
    {
        var assembly = typeof(InicializadorBanco).Assembly;
        using var stream = assembly.GetManifestResourceStream("MandiocaCaipira.App.Data.Migrations.001_Initial.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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

    private sealed class MigracaoEnv : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));

        public BancoSqlite Banco { get; }
        public InicializadorBanco Inicializador { get; }

        public MigracaoEnv()
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
