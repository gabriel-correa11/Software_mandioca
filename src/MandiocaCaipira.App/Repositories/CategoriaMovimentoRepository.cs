using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Repositories;

public sealed class CategoriaMovimentoRepository(BancoSqlite banco)
{
    public IReadOnlyList<CategoriaMovimento> Listar(TipoMovimento? tipo = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Nome, Tipo, Natureza, Ativa
            FROM CategoriaMovimento
            WHERE Ativa = 1
              AND ($tipo IS NULL OR Tipo = $tipo)
            ORDER BY Nome;
            """;
        command.Parameters.AddWithValue("$tipo", tipo.HasValue ? tipo.Value.ToString() : DBNull.Value);
        using var reader = command.ExecuteReader();
        var result = new List<CategoriaMovimento>();
        while (reader.Read())
            result.Add(new CategoriaMovimento(
                reader.GetInt64(0), reader.GetString(1),
                Enum.Parse<TipoMovimento>(reader.GetString(2)),
                Enum.Parse<NaturezaCategoria>(reader.GetString(3)),
                reader.GetInt64(4) == 1));
        return result;
    }

    public CategoriaMovimento? Obter(long id)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Nome, Tipo, Natureza, Ativa FROM CategoriaMovimento WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new CategoriaMovimento(reader.GetInt64(0), reader.GetString(1),
                Enum.Parse<TipoMovimento>(reader.GetString(2)),
                Enum.Parse<NaturezaCategoria>(reader.GetString(3)),
                reader.GetInt64(4) == 1)
            : null;
    }

    public CategoriaMovimento? ObterPorNomeETipo(string nome, TipoMovimento tipo)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Nome, Tipo, Natureza, Ativa FROM CategoriaMovimento WHERE Nome = $nome AND Tipo = $tipo;";
        command.Parameters.AddWithValue("$nome", nome);
        command.Parameters.AddWithValue("$tipo", tipo.ToString());
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new CategoriaMovimento(reader.GetInt64(0), reader.GetString(1),
                Enum.Parse<TipoMovimento>(reader.GetString(2)),
                Enum.Parse<NaturezaCategoria>(reader.GetString(3)),
                reader.GetInt64(4) == 1)
            : null;
    }
}
