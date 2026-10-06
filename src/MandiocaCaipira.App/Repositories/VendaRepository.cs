using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class VendaRepository(BancoSqlite banco)
{
    public long Inserir(
        SqliteConnection connection, SqliteTransaction transaction, long clienteId,
        long quantidadeGramas, long precoCentavosPorKg, DateOnly dataOperacao,
        DateTimeOffset criadoEm)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Venda(ClienteId, QuantidadeGramas, PrecoCentavosPorKg,
                              DataOperacao, CriadoEm, Origem)
            VALUES ($cliente, $quantidade, $preco, $data, $criado, 'Manual');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$cliente", clienteId);
        command.Parameters.AddWithValue("$quantidade", quantidadeGramas);
        command.Parameters.AddWithValue("$preco", precoCentavosPorKg);
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(dataOperacao));
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        return (long)command.ExecuteScalar()!;
    }

    public IReadOnlyList<VendaResumo> Listar(
        long? clienteId = null, DateOnly? inicio = null, DateOnly? fim = null,
        bool somentePendentes = false)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT v.Id, v.ClienteId, c.Nome, v.DataOperacao, v.QuantidadeGramas,
                   v.PrecoCentavosPorKg,
                   sale_total_cents(v.QuantidadeGramas, v.PrecoCentavosPorKg) Total,
                   COALESCE(SUM(CASE WHEN p.EstornadoEm IS NULL THEN a.ValorCentavos ELSE 0 END), 0) Recebido,
                   v.CanceladaEm, v.MotivoCancelamento
            FROM Venda v
            JOIN Cliente c ON c.Id = v.ClienteId
            LEFT JOIN AlocacaoPagamento a ON a.VendaId = v.Id
            LEFT JOIN Pagamento p ON p.Id = a.PagamentoId
            WHERE ($cliente IS NULL OR v.ClienteId = $cliente)
              AND ($inicio IS NULL OR v.DataOperacao >= $inicio)
              AND ($fim IS NULL OR v.DataOperacao <= $fim)
            GROUP BY v.Id
            HAVING ($pendentes = 0 OR (v.CanceladaEm IS NULL AND Recebido < Total))
            ORDER BY v.DataOperacao DESC, v.Id DESC;
            """;
        command.Parameters.AddWithValue("$cliente", clienteId is null ? DBNull.Value : clienteId.Value);
        command.Parameters.AddWithValue("$inicio", inicio is null ? DBNull.Value : FormatoTemporal.FormatarDataOperacao(inicio.Value));
        command.Parameters.AddWithValue("$fim", fim is null ? DBNull.Value : FormatoTemporal.FormatarDataOperacao(fim.Value));
        command.Parameters.AddWithValue("$pendentes", somentePendentes ? 1 : 0);
        using var reader = command.ExecuteReader();
        var result = new List<VendaResumo>();
        while (reader.Read())
            result.Add(new VendaResumo(
                reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
                FormatoTemporal.LerDataOperacao(reader.GetString(3)), reader.GetInt64(4),
                reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7),
                !reader.IsDBNull(8), reader.IsDBNull(8) ? null : FormatoTemporal.LerInstanteUtc(reader.GetString(8)),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        return result;
    }

    public void Cancelar(long vendaId, string motivo, DateTimeOffset canceladaEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Venda SET CanceladaEm = $data, MotivoCancelamento = $motivo
            WHERE Id = $id AND CanceladaEm IS NULL;
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarInstanteUtc(canceladaEm));
        command.Parameters.AddWithValue("$motivo", motivo);
        command.Parameters.AddWithValue("$id", vendaId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Venda não encontrada ou já cancelada.");
    }
}
