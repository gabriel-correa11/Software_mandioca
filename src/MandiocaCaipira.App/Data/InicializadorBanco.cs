using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Data;

public sealed class InicializadorBanco(BancoSqlite banco, string diretorioBackups)
{
    private static readonly string[] ObjetosObrigatorios =
    [
        "Cliente", "Venda", "Pagamento", "AlocacaoPagamento",
        "TR_Alocacao_Validar", "TR_Alocacao_Imutavel", "TR_Alocacao_SemExclusao",
        "TR_Pagamento_Imutavel", "TR_Pagamento_SemExclusao",
        "TR_Venda_Imutavel", "TR_Venda_Cancelar", "TR_Venda_SemExclusao",
        "TR_Cliente_SemExclusao", "TR_Venda_ClienteAtivo", "TR_Pagamento_ClienteAtivo"
    ];

    public void Inicializar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(banco.CaminhoBanco)!);
        using var connection = banco.AbrirConexao();
        var version = Versao(connection);
        if (version > 1)
            throw new InvalidOperationException("O banco foi criado por uma versão mais recente da aplicação.");
        if (version == 1)
        {
            ValidarBanco(connection);
            return;
        }

        using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
            if ((long)check.ExecuteScalar()! != 0)
                throw new InvalidOperationException("Banco sem versão com tabelas existentes; migração automática recusada.");
        }

        // V1 is only created on an empty database. Future migrations must back up
        // the existing database before changing it.
        using var transaction = connection.BeginTransaction();
        try
        {
            using var sql = connection.CreateCommand();
            sql.Transaction = transaction;
            sql.CommandText = LerScriptInicial();
            sql.ExecuteNonQuery();
            using var mark = connection.CreateCommand();
            mark.Transaction = transaction;
            mark.CommandText = "PRAGMA user_version = 1;";
            mark.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        ValidarBanco(connection);
    }

    public string CriarBackupAntesDeMigrar()
    {
        Directory.CreateDirectory(diretorioBackups);
        var name = $"mandioca-{DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture)}.sqlite";
        var path = Path.Combine(diretorioBackups, name);
        using var source = banco.AbrirConexao();
        using var target = new BancoSqlite(path).AbrirConexao();
        source.BackupDatabase(target);
        ValidarBanco(target);
        return path;
    }

    private static long Versao(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return (long)command.ExecuteScalar()!;
    }

    private static void ValidarBanco(SqliteConnection connection)
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

        using var schema = connection.CreateCommand();
        schema.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE name IN (" +
            string.Join(",", ObjetosObrigatorios.Select((_, index) => $"$name{index}")) + ");";
        for (var index = 0; index < ObjetosObrigatorios.Length; index++)
            schema.Parameters.AddWithValue($"$name{index}", ObjetosObrigatorios[index]);
        if ((long)schema.ExecuteScalar()! != ObjetosObrigatorios.Length)
            throw new InvalidDataException("O schema do banco está incompleto ou incompatível.");
    }

    private static string LerScriptInicial()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            "MandiocaCaipira.App.Data.Migrations.001_Initial.sql")
            ?? throw new InvalidOperationException("Migration v1 não incorporada ao aplicativo.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
