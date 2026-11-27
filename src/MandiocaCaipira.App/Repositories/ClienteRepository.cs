using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class ClienteRepository(BancoSqlite banco)
{
    public IReadOnlyList<Cliente> Listar(string? pesquisa, bool incluirInativos)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Nome, Ativo, Origem, ReferenciaExterna, CriadoEm
            FROM Cliente
            WHERE ($incluir = 1 OR Ativo = 1)
              AND ($pesquisa = '' OR Nome LIKE '%' || $pesquisa || '%' COLLATE NOCASE)
            ORDER BY Ativo DESC, Nome COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$incluir", incluirInativos ? 1 : 0);
        command.Parameters.AddWithValue("$pesquisa", pesquisa?.Trim() ?? string.Empty);
        using var reader = command.ExecuteReader();
        var result = new List<Cliente>();
        while (reader.Read())
            result.Add(new Cliente(
                reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2) == 1,
                Enum.Parse<OrigemRegistro>(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                FormatoTemporal.LerInstanteUtc(reader.GetString(5))));
        return result;
    }

    public long Inserir(string nome, OrigemRegistro origem, DateTimeOffset criadoEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Cliente(Nome, Origem, CriadoEm)
            VALUES ($nome, $origem, $criadoEm);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nome", nome);
        command.Parameters.AddWithValue("$origem", origem.ToString());
        command.Parameters.AddWithValue("$criadoEm", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        return (long)command.ExecuteScalar()!;
    }

    public void InserirImportadosAtomico(IReadOnlyList<string> nomes, DateTimeOffset criadoEm)
    {
        if (nomes.Count == 0) return;
        using var connection = banco.AbrirConexao();
        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Cliente(Nome, Origem, CriadoEm) VALUES ($nome, 'Importacao', $criadoEm);";
            var nome = command.Parameters.Add("$nome", SqliteType.Text);
            command.Parameters.AddWithValue("$criadoEm", FormatoTemporal.FormatarInstanteUtc(criadoEm));
            foreach (var item in nomes)
            {
                nome.Value = item;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Renomear(long clienteId, string novoNome)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Cliente SET Nome = $nome WHERE Id = $id;";
        command.Parameters.AddWithValue("$nome", novoNome);
        command.Parameters.AddWithValue("$id", clienteId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Cliente não encontrado.");
    }

    public void DefinirAtivo(long clienteId, bool ativo)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Cliente SET Ativo = $ativo WHERE Id = $id;";
        command.Parameters.AddWithValue("$ativo", ativo ? 1 : 0);
        command.Parameters.AddWithValue("$id", clienteId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Cliente não encontrado.");
    }
}
