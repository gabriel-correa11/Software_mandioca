using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Serilog;

namespace MandiocaCaipira.App.Data;

public sealed class InicializadorBanco
{
    private readonly BancoSqlite _banco;
    private readonly string _diretorioBackups;
    private readonly ILogger _logger;

    private static readonly string[] ObjetosV1 =
    [
        "Cliente", "Venda", "Pagamento", "AlocacaoPagamento",
        "TR_Alocacao_Validar", "TR_Alocacao_Imutavel", "TR_Alocacao_SemExclusao",
        "TR_Pagamento_Imutavel", "TR_Pagamento_SemExclusao",
        "TR_Venda_Imutavel", "TR_Venda_Cancelar", "TR_Venda_SemExclusao",
        "TR_Cliente_SemExclusao", "TR_Venda_ClienteAtivo", "TR_Pagamento_ClienteAtivo"
    ];

    private static readonly string[] ObjetosObrigatorios =
    [
        .. ObjetosV1,
        "Fornecedor", "ConfiguracaoRendimento", "CompraMandioca",
        "TR_Compra_FornecedorAtivo", "TR_Compra_Imutavel", "TR_Compra_SemExclusao",
        "TR_Fornecedor_SemExclusao", "TR_ConfigRendimento_SemExclusao"
    ];

    private static readonly IReadOnlyList<MigracaoSchema> Migracoes =
    [
        new(0, 1, AplicarV1),
        new(1, 2, AplicarV2),
    ];

    public static int VersaoAtual => Migracoes[^1].VersaoDestino;

    public InicializadorBanco(BancoSqlite banco, string diretorioBackups, ILogger? logger = null)
    {
        _banco = banco;
        _diretorioBackups = diretorioBackups;
        _logger = logger ?? Log.Logger;
    }

    public void Inicializar()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_banco.CaminhoBanco)!);
            using var connection = _banco.AbrirConexao();
            var version = Versao(connection);
            _logger.Information("Inicializando banco SQLite na versão de schema {SchemaVersion}", version);

            if (version > VersaoAtual)
                throw new InvalidOperationException(
                    $"O banco foi criado por uma versão mais recente da aplicação (schema v{version}, suportado até v{VersaoAtual}).");

            if (version == VersaoAtual)
            {
                ValidarBanco(connection);
                _logger.Information("Banco SQLite validado com sucesso");
                return;
            }

            if (version == 0)
            {
                using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
                var tableCount = (long)check.ExecuteScalar()!;
                if (tableCount != 0)
                {
                    if (PossuiSchemaV1(connection))
                    {
                        _logger.Information("Banco legado detectado com schema v1 sem user_version; reconhecendo como v1");
                        MarcarVersao(connection, 1);
                        version = 1;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Banco sem versão com tabelas existentes; migração automática recusada.");
                    }
                }
            }

            if (version > 0)
            {
                _logger.Information("Criando backup antes de migrar de v{From} para v{To}", version, VersaoAtual);
                CriarBackupAntesDeMigrar();
            }

            var pendentes = Migracoes.Where(m => m.VersaoOrigem >= version).ToList();
            foreach (var migracao in pendentes)
            {
                _logger.Information("Aplicando migração v{From} → v{To}",
                    migracao.VersaoOrigem, migracao.VersaoDestino);
                ExecutarMigracao(connection, migracao);
            }

            ValidarBanco(connection);
            _logger.Information("Banco SQLite atualizado para v{Version} com sucesso", VersaoAtual);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Falha ao inicializar ou validar o banco SQLite");
            throw;
        }
    }

    public string CriarBackupAntesDeMigrar() =>
        CriarBackupInterno("migracao", "Backup SQLite criado antes da migração", validarSchema: false);

    public string CriarBackupAgora() =>
        CriarBackupInterno("manual", "Backup SQLite manual criado e validado");

    public string CriarBackupAutomatico() =>
        CriarBackupInterno("auto", "Backup SQLite automático criado e validado");

    internal string CriarBackupInterno(string categoria, string mensagemLog, bool validarSchema = true)
    {
        try
        {
            Directory.CreateDirectory(_diretorioBackups);
            var name = $"mandioca-{categoria}-{DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture)}.sqlite";
            var path = Path.Combine(_diretorioBackups, name);
            using (var source = _banco.AbrirConexao())
            using (var target = new BancoSqlite(path).AbrirConexao())
            {
                source.BackupDatabase(target);
                if (validarSchema)
                    ValidarBanco(target);
                else
                    ValidarIntegridade(target);
            }
            GravarHmacBackup(path, _diretorioBackups);
            _logger.Information("{BackupResult}", mensagemLog);
            return path;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Falha ao criar ou validar backup SQLite");
            throw;
        }
    }

    internal static void ValidarArquivoBackup(string caminho)
    {
        var banco = new BancoSqlite(caminho);
        using var connection = banco.AbrirConexao();
        ValidarBanco(connection);
    }

    internal static int VersaoDoArquivo(string caminho)
    {
        var banco = new BancoSqlite(caminho);
        using var connection = banco.AbrirConexao();
        return (int)Versao(connection);
    }

    internal static void GravarHmacBackup(string caminhoBackup, string diretorioBackups)
    {
        try
        {
            var chave = ObterOuCriarChaveIntegridade(diretorioBackups);
            using var hmac = new HMACSHA256(chave);
            using var stream = File.OpenRead(caminhoBackup);
            var hash = hmac.ComputeHash(stream);
            File.WriteAllText(caminhoBackup + ".hmac", Convert.ToHexString(hash));
        }
        catch { /* melhor esforço — não falhar a criação do backup */ }
    }

    internal static bool VerificarHmacBackup(string caminhoBackup, string diretorioBackups)
    {
        var caminhoHmac = caminhoBackup + ".hmac";
        if (!File.Exists(caminhoHmac))
            return true; // Backup legado sem HMAC

        try
        {
            var chave = ObterOuCriarChaveIntegridade(diretorioBackups);
            using var hmac = new HMACSHA256(chave);
            using var stream = File.OpenRead(caminhoBackup);
            var hash = hmac.ComputeHash(stream);
            var esperado = File.ReadAllText(caminhoHmac).Trim();
            return string.Equals(Convert.ToHexString(hash), esperado, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static byte[] ObterOuCriarChaveIntegridade(string diretorioBackups)
    {
        var caminho = Path.Combine(diretorioBackups, ".chave-integridade");
        if (File.Exists(caminho))
            return File.ReadAllBytes(caminho);

        var chave = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(diretorioBackups);
        File.WriteAllBytes(caminho, chave);
        try { File.SetAttributes(caminho, File.GetAttributes(caminho) | FileAttributes.Hidden); }
        catch { /* melhor esforço */ }
        return chave;
    }

    private static void ExecutarMigracao(SqliteConnection connection, MigracaoSchema migracao)
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

    private static void AplicarV1(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var sql = connection.CreateCommand();
        sql.Transaction = transaction;
        sql.CommandText = LerScriptEmbutido("MandiocaCaipira.App.Data.Migrations.001_Initial.sql");
        sql.ExecuteNonQuery();
    }

    private static void AplicarV2(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var sql = connection.CreateCommand();
        sql.Transaction = transaction;
        sql.CommandText = LerScriptEmbutido("MandiocaCaipira.App.Data.Migrations.002_CompraMandioca.sql");
        sql.ExecuteNonQuery();
    }

    private static long Versao(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return (long)command.ExecuteScalar()!;
    }

    private static void MarcarVersao(SqliteConnection connection, int versao)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {versao};";
        command.ExecuteNonQuery();
    }

    private static bool PossuiSchemaV1(SqliteConnection connection)
    {
        using var schema = connection.CreateCommand();
        schema.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE name IN (" +
            string.Join(",", ObjetosV1.Select((_, index) => $"$name{index}")) + ");";
        for (var index = 0; index < ObjetosV1.Length; index++)
            schema.Parameters.AddWithValue($"$name{index}", ObjetosV1[index]);
        return (long)schema.ExecuteScalar()! == ObjetosV1.Length;
    }

    private static void ValidarIntegridade(SqliteConnection connection)
    {
        using (var integrity = connection.CreateCommand())
        {
            integrity.CommandText = "PRAGMA quick_check;";
            if (!string.Equals((string?)integrity.ExecuteScalar(), "ok", StringComparison.Ordinal))
                throw new InvalidDataException("O banco de dados falhou na verificação de integridade.");
        }

        using (var foreignKeys = connection.CreateCommand())
        {
            foreignKeys.CommandText = "SELECT COUNT(*) FROM pragma_foreign_key_check;";
            if ((long)foreignKeys.ExecuteScalar()! != 0)
                throw new InvalidDataException("O banco de dados contém referências inválidas.");
        }
    }

    internal static void ValidarBanco(SqliteConnection connection)
    {
        ValidarIntegridade(connection);

        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE name IN (" +
            string.Join(",", ObjetosObrigatorios.Select((_, index) => $"$name{index}")) + ");";
        for (var index = 0; index < ObjetosObrigatorios.Length; index++)
            check.Parameters.AddWithValue($"$name{index}", ObjetosObrigatorios[index]);
        if ((long)check.ExecuteScalar()! != ObjetosObrigatorios.Length)
            throw new InvalidDataException("O schema do banco está incompleto ou incompatível.");
    }

    private static string LerScriptEmbutido(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Script de migração '{resourceName}' não incorporado ao aplicativo.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal sealed record MigracaoSchema(int VersaoOrigem, int VersaoDestino,
        Action<SqliteConnection, SqliteTransaction> Executar);
}
