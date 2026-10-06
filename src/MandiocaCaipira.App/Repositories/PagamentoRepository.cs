using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class PagamentoRepository(BancoSqlite banco)
{
    public long Inserir(
        SqliteConnection connection, SqliteTransaction transaction, long clienteId,
        long valorCentavos, DateOnly dataOperacao, DateTimeOffset criadoEm)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Pagamento(ClienteId, ValorCentavos, DataOperacao, CriadoEm, Origem)
            VALUES ($cliente, $valor, $data, $criado, 'Manual');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$cliente", clienteId);
        command.Parameters.AddWithValue("$valor", valorCentavos);
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(dataOperacao));
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        return (long)command.ExecuteScalar()!;
    }

    public void InserirAlocacao(
        SqliteConnection connection, SqliteTransaction transaction, long pagamentoId,
        long vendaId, long valorCentavos, DateTimeOffset criadoEm)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO AlocacaoPagamento(PagamentoId, VendaId, ValorCentavos, CriadoEm)
            VALUES ($pagamento, $venda, $valor, $criado);
            """;
        command.Parameters.AddWithValue("$pagamento", pagamentoId);
        command.Parameters.AddWithValue("$venda", vendaId);
        command.Parameters.AddWithValue("$valor", valorCentavos);
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PagamentoResumo> Listar(long? clienteId = null, DateOnly? inicio = null, DateOnly? fim = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id, p.ClienteId, c.Nome, p.DataOperacao, p.ValorCentavos,
                   COALESCE(SUM(a.ValorCentavos), 0), p.EstornadoEm, p.MotivoEstorno
            FROM Pagamento p
            JOIN Cliente c ON c.Id = p.ClienteId
            LEFT JOIN AlocacaoPagamento a ON a.PagamentoId = p.Id
            WHERE ($cliente IS NULL OR p.ClienteId = $cliente)
              AND ($inicio IS NULL OR p.DataOperacao >= $inicio)
              AND ($fim IS NULL OR p.DataOperacao <= $fim)
            GROUP BY p.Id
            ORDER BY p.DataOperacao DESC, p.Id DESC;
            """;
        command.Parameters.AddWithValue("$cliente", clienteId is null ? DBNull.Value : clienteId.Value);
        command.Parameters.AddWithValue("$inicio", inicio is null ? DBNull.Value : FormatoTemporal.FormatarDataOperacao(inicio.Value));
        command.Parameters.AddWithValue("$fim", fim is null ? DBNull.Value : FormatoTemporal.FormatarDataOperacao(fim.Value));
        using var reader = command.ExecuteReader();
        var result = new List<PagamentoResumo>();
        while (reader.Read())
            result.Add(new PagamentoResumo(
                reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
                FormatoTemporal.LerDataOperacao(reader.GetString(3)), reader.GetInt64(4), reader.GetInt64(5),
                reader.IsDBNull(6) ? null : FormatoTemporal.LerInstanteUtc(reader.GetString(6)),
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        return result;
    }

    public void Estornar(long pagamentoId, string motivo, DateTimeOffset estornadoEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Pagamento SET EstornadoEm = $data, MotivoEstorno = $motivo
            WHERE Id = $id AND EstornadoEm IS NULL;
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarInstanteUtc(estornadoEm));
        command.Parameters.AddWithValue("$motivo", motivo);
        command.Parameters.AddWithValue("$id", pagamentoId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Pagamento não encontrado ou já estornado.");
    }
}
