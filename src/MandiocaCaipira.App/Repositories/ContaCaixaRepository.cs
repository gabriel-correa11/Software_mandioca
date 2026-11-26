using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Repositories;

public sealed class ContaCaixaRepository(BancoSqlite banco)
{
    public IReadOnlyList<ContaCaixa> Listar(bool apenasAtivas = true)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = apenasAtivas
            ? "SELECT Id, Nome, Ativa FROM ContaCaixa WHERE Ativa = 1 ORDER BY Nome;"
            : "SELECT Id, Nome, Ativa FROM ContaCaixa ORDER BY Nome;";
        using var reader = command.ExecuteReader();
        var result = new List<ContaCaixa>();
        while (reader.Read())
            result.Add(new ContaCaixa(reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2) == 1));
        return result;
    }

    public ContaCaixa? Obter(long id)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Nome, Ativa FROM ContaCaixa WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new ContaCaixa(reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2) == 1)
            : null;
    }
}
