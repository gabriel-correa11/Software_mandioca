using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class FornecedorRepository(BancoSqlite banco)
{
    public long Inserir(string nome, string? apelido, string? telefone)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Fornecedor(Nome, Apelido, Telefone, CriadoEm)
            VALUES ($nome, $apelido, $telefone, $criado);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nome", nome);
        command.Parameters.AddWithValue("$apelido", (object?)apelido ?? DBNull.Value);
        command.Parameters.AddWithValue("$telefone", (object?)telefone ?? DBNull.Value);
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(DateTimeOffset.UtcNow));
        return (long)command.ExecuteScalar()!;
    }

    public IReadOnlyList<Fornecedor> Listar(bool somenteAtivos = true)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Nome, Apelido, Telefone, Ativo, CriadoEm
            FROM Fornecedor
            WHERE ($ativos = 0 OR Ativo = 1)
            ORDER BY Nome COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$ativos", somenteAtivos ? 1 : 0);
        using var reader = command.ExecuteReader();
        var result = new List<Fornecedor>();
        while (reader.Read())
            result.Add(new Fornecedor(
                reader.GetInt64(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt64(4) == 1,
                FormatoTemporal.LerInstanteUtc(reader.GetString(5))));
        return result;
    }

    public Fornecedor? Obter(long id)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Nome, Apelido, Telefone, Ativo, CriadoEm
            FROM Fornecedor WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new Fornecedor(
            reader.GetInt64(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt64(4) == 1,
            FormatoTemporal.LerInstanteUtc(reader.GetString(5)));
    }

    public void AlternarAtivo(long id)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Fornecedor SET Ativo = CASE WHEN Ativo = 1 THEN 0 ELSE 1 END
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Fornecedor não encontrado.");
    }
}
