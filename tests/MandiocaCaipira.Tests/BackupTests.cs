using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.Tests;

public sealed class BackupTests
{
    [Fact]
    public void BackupAutomaticoNaPrimeiraAberturaDoDia()
    {
        using var env = new BackupEnv();
        Assert.True(env.Backup.TentarBackupAutomatico());
        var autos = Directory.GetFiles(env.DiretorioBackups, "mandioca-auto-*.sqlite");
        Assert.Single(autos);
    }

    [Fact]
    public void BackupAutomaticoNaoDuplicaNoMesmoDia()
    {
        using var env = new BackupEnv();
        Assert.True(env.Backup.TentarBackupAutomatico());
        Assert.True(env.Backup.TentarBackupAutomatico());
        Assert.True(env.Backup.TentarBackupAutomatico());
        var autos = Directory.GetFiles(env.DiretorioBackups, "mandioca-auto-*.sqlite");
        Assert.Single(autos);
    }

    [Fact]
    public void BackupConsisteComDadosReais()
    {
        using var env = new BackupEnv();
        InserirDadosCompletos(env);

        env.Backup.TentarBackupAutomatico();

        var arquivo = Assert.Single(Directory.GetFiles(env.DiretorioBackups, "mandioca-auto-*.sqlite"));
        using var db = new BancoSqlite(arquivo).AbrirConexao();
        Assert.Equal(2L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Venda;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Pagamento;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM AlocacaoPagamento;"));
    }

    [Fact]
    public void RetencaoRemoveAutomaticosAntigosPreservandoMinimo()
    {
        using var env = new BackupEnv();

        // Create fake old auto backups
        Directory.CreateDirectory(env.DiretorioBackups);
        for (var i = 0; i < 6; i++)
        {
            var nome = $"mandioca-auto-2025{(i + 1):D2}01-120000-0000000.sqlite";
            var path = Path.Combine(env.DiretorioBackups, nome);
            File.WriteAllBytes(path, []);
            File.SetCreationTimeUtc(path, new DateTime(2025, i + 1, 1));
        }

        env.Backup.AplicarRetencao();

        var restantes = Directory.GetFiles(env.DiretorioBackups, "mandioca-auto-*.sqlite");
        Assert.True(restantes.Length >= 3, "Deve preservar ao menos 3 backups automáticos");
    }

    [Fact]
    public void RetencaoPreservaManuaisEPreMigracao()
    {
        using var env = new BackupEnv();
        Directory.CreateDirectory(env.DiretorioBackups);

        // Create old manual and pre-migration backups
        var manual = Path.Combine(env.DiretorioBackups, "mandioca-manual-20250101-120000-0000000.sqlite");
        var migracao = Path.Combine(env.DiretorioBackups, "mandioca-migracao-20250101-120000-0000000.sqlite");
        File.WriteAllBytes(manual, []);
        File.WriteAllBytes(migracao, []);

        // Create many old auto backups
        for (var i = 0; i < 10; i++)
        {
            var nome = $"mandioca-auto-2024{(i + 1):D2}01-120000-0000000.sqlite";
            var path = Path.Combine(env.DiretorioBackups, nome);
            File.WriteAllBytes(path, []);
            File.SetCreationTimeUtc(path, new DateTime(2024, 1, 1));
        }

        env.Backup.AplicarRetencao();

        Assert.True(File.Exists(manual), "Backup manual não deve ser removido pela retenção");
        Assert.True(File.Exists(migracao), "Backup pré-migração não deve ser removido pela retenção");
    }

    [Fact]
    public void FalhaDeBackupNaoImpedeContinuacao()
    {
        using var env = new BackupEnv();
        // Create an InicializadorBanco with impossible backup path so the actual backup fails
        var inicializadorQuebrado = new InicializadorBanco(
            env.Banco,
            Path.Combine(env.Banco.CaminhoBanco, "path", "impossivel"));
        var servicoQuebrado = new BackupService(
            inicializadorQuebrado,
            Path.Combine(env.Banco.CaminhoBanco, "path", "impossivel"),
            env.Banco.CaminhoBanco);

        // Should return false but not throw
        Assert.False(servicoQuebrado.TentarBackupAutomatico());
    }

    [Fact]
    public void BackupCorrompidoRejeitadoNaRestauracao()
    {
        using var env = new BackupEnv();
        var corrompido = Path.Combine(env.DiretorioBackups, "mandioca-manual-20260101-120000-0000000.sqlite");
        Directory.CreateDirectory(env.DiretorioBackups);
        File.WriteAllText(corrompido, "isto nao e sqlite");

        var resultado = env.Backup.Restaurar(corrompido);

        Assert.False(resultado.Ok);
        Assert.Contains("corrompido", resultado.Mensagem!);
    }

    [Fact]
    public void VersaoIncompativelRejeitadaNaRestauracao()
    {
        using var env = new BackupEnv();
        // Create a backup with future version
        var futuro = Path.Combine(env.DiretorioBackups, "futuro.sqlite");
        Directory.CreateDirectory(env.DiretorioBackups);
        var bancoFuturo = new BancoSqlite(futuro);
        using (var db = bancoFuturo.AbrirConexao())
            Executar(db, "PRAGMA user_version = 999;");

        var resultado = env.Backup.Restaurar(futuro);

        Assert.False(resultado.Ok);
        Assert.Contains("versão mais recente", resultado.Mensagem!);
    }

    [Fact]
    public void RestauracaoBemSucedida()
    {
        using var env = new BackupEnv();
        InserirDadosCompletos(env);

        // Create manual backup
        var backupPath = env.Backup.Criar();

        // Modify current DB (add another client)
        using (var db = env.Banco.AbrirConexao())
            Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Novo','Manual','2026-10-15T10:00:00.0000000Z');");

        using (var db = env.Banco.AbrirConexao())
            Assert.Equal(3L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));

        // Restore the backup (which has 2 clients)
        var resultado = env.Backup.Restaurar(backupPath);

        Assert.True(resultado.Ok);
        Assert.NotNull(resultado.BackupSeguranca);
        Assert.True(File.Exists(resultado.BackupSeguranca));

        // The DB file should now have 2 clients (from backup)
        using var restored = env.Banco.AbrirConexao();
        Assert.Equal(2L, Escalar(restored, "SELECT COUNT(*) FROM Cliente;"));
    }

    [Fact]
    public void FalhaDuranteRestauracaoPreservaBancoAnterior()
    {
        using var env = new BackupEnv();
        InserirDadosCompletos(env);

        // Try to restore a non-existent file
        var resultado = env.Backup.Restaurar(Path.Combine(env.DiretorioBackups, "nao-existe.sqlite"));

        Assert.False(resultado.Ok);
        // Original database untouched
        using var db = env.Banco.AbrirConexao();
        Assert.Equal(2L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
    }

    [Fact]
    public void PreservacaoDeClientesVendasPagamentosEAlocacoes()
    {
        using var env = new BackupEnv();
        InserirDadosCompletos(env);

        var backupPath = env.Backup.Criar();

        var resultado = env.Backup.Restaurar(backupPath);
        Assert.True(resultado.Ok);

        using var db = env.Banco.AbrirConexao();
        Assert.Equal(2L, Escalar(db, "SELECT COUNT(*) FROM Cliente;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Venda;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM Pagamento;"));
        Assert.Equal(1L, Escalar(db, "SELECT COUNT(*) FROM AlocacaoPagamento;"));
        Assert.Equal(1L, Escalar(db, "PRAGMA foreign_keys;"));
        // Triggers still active
        Executar(db, "UPDATE Cliente SET Ativo=0 WHERE Id=1;");
        Assert.Throws<SqliteException>(() => Executar(db,
            "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');"));
    }

    [Fact]
    public void ListagemDeBackupsRetornaInformacoesCorretas()
    {
        using var env = new BackupEnv();
        env.Backup.Criar();
        env.Backup.TentarBackupAutomatico();
        env.Inicializador.CriarBackupAntesDeMigrar();

        var lista = env.Backup.Listar();

        Assert.Equal(3, lista.Count);
        Assert.Contains(lista, b => b.Categoria == CategoriaBackup.Manual);
        Assert.Contains(lista, b => b.Categoria == CategoriaBackup.Automatico);
        Assert.Contains(lista, b => b.Categoria == CategoriaBackup.PreMigracao);
        Assert.All(lista, b =>
        {
            Assert.True(b.TamanhoBytes > 0);
            Assert.False(string.IsNullOrEmpty(b.NomeArquivo));
        });
    }

    [Fact]
    public void BackupManualUsaPrefixoCorreto()
    {
        using var env = new BackupEnv();
        var path = env.Backup.Criar();
        Assert.Contains("mandioca-manual-", Path.GetFileName(path));
    }

    [Fact]
    public void InstanciaUnicaBloqueiaDuplicata()
    {
        var nome = $"MandiocaCaipiraTest_{Guid.NewGuid():N}";
        using var primeira = new InstanciaUnicaTeste(nome);
        Assert.True(primeira.IsOwner);

        // Mutex is reentrant on the same thread; test from another thread
        bool? secondResult = null;
        var thread = new Thread(() =>
        {
            using var segunda = new InstanciaUnicaTeste(nome);
            secondResult = segunda.IsOwner;
        });
        thread.Start();
        thread.Join();
        Assert.False(secondResult);
    }

    [Fact]
    public void InstanciaUnicaLiberaAposDispose()
    {
        string mutexName;
        using (var primeira = new InstanciaUnicaTeste())
        {
            Assert.True(primeira.IsOwner);
            mutexName = primeira.MutexName;
        }

        using var nova = new InstanciaUnicaTeste(mutexName);
        Assert.True(nova.IsOwner);
    }

    [Fact]
    public void BackupSemVersaoRejeitadoNaRestauracao()
    {
        using var env = new BackupEnv();
        var semVersao = Path.Combine(env.DiretorioBackups, "sem-versao.sqlite");
        Directory.CreateDirectory(env.DiretorioBackups);
        var bancoVazio = new BancoSqlite(semVersao);
        using (var db = bancoVazio.AbrirConexao())
            Executar(db, "CREATE TABLE Teste(Id INTEGER PRIMARY KEY) STRICT;");

        var resultado = env.Backup.Restaurar(semVersao);

        Assert.False(resultado.Ok);
    }

    private static void InserirDadosCompletos(BackupEnv env)
    {
        using var db = env.Banco.AbrirConexao();
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Alfa','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "INSERT INTO Cliente(Nome,Origem,CriadoEm) VALUES ('Beta','Manual','2026-09-20T10:00:00.0000000Z');");
        Executar(db, "INSERT INTO Venda(ClienteId,QuantidadeGramas,PrecoCentavosPorKg,DataOperacao,CriadoEm,Origem) VALUES (1,1000,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO Pagamento(ClienteId,ValorCentavos,DataOperacao,CriadoEm,Origem) VALUES (1,100,'2026-09-20','2026-09-20T10:00:00.0000000Z','Manual');");
        Executar(db, "INSERT INTO AlocacaoPagamento(PagamentoId,VendaId,ValorCentavos,CriadoEm) VALUES (1,1,100,'2026-09-20T10:00:00.0000000Z');");
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

    internal sealed class BackupEnv : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));

        public BancoSqlite Banco { get; }
        public InicializadorBanco Inicializador { get; }
        public BackupService Backup { get; }
        public string DiretorioBackups { get; }

        public BackupEnv()
        {
            Directory.CreateDirectory(_directory);
            DiretorioBackups = Path.Combine(_directory, "backups");
            Banco = new BancoSqlite(Path.Combine(_directory, "data", "test.sqlite"));
            Inicializador = new InicializadorBanco(Banco, DiretorioBackups);
            Inicializador.Inicializar();
            Backup = new BackupService(Inicializador, DiretorioBackups, Banco.CaminhoBanco);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class InstanciaUnicaTeste : IDisposable
    {
        private readonly Mutex _mutex;
        private readonly bool _acquired;

        public string MutexName { get; }
        public bool IsOwner => _acquired;

        public InstanciaUnicaTeste(string? name = null)
        {
            MutexName = name ?? $"MandiocaCaipiraTest_{Guid.NewGuid():N}";
            _mutex = new Mutex(initiallyOwned: false, MutexName, out _);
            try
            {
                _acquired = _mutex.WaitOne(0, exitContext: false);
            }
            catch (AbandonedMutexException)
            {
                _acquired = true;
            }
        }

        public void Dispose()
        {
            if (_acquired)
            {
                try { _mutex.ReleaseMutex(); }
                catch (ObjectDisposedException) { }
            }
            _mutex.Dispose();
        }
    }
}
