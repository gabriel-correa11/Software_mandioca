using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class CompraRepository(BancoSqlite banco)
{
    public long Inserir(
        SqliteConnection connection, SqliteTransaction transaction,
        long fornecedorId, DateOnly dataCompra, long pesoBrutoGramas, long pesoCascadoGramas,
        long? custoCentavos, string? caminhoFoto, string? observacao,
        long snapshotPerdaBp, long snapshotGramasPorSaco, long snapshotTamanhoSacoGramas,
        DateTimeOffset criadoEm)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO CompraMandioca(FornecedorId, DataCompra, PesoBrutoGramas, PesoCascadoGramas,
                CustoCentavos, CaminhoFoto, Observacao,
                SnapshotPerdaBp, SnapshotGramasPorSaco, SnapshotTamanhoSacoGramas, CriadoEm)
            VALUES ($fornecedor, $data, $bruto, $cascado, $custo, $foto, $obs,
                $perdaBp, $gramasSaco, $tamanhoSaco, $criado);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$fornecedor", fornecedorId);
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(dataCompra));
        command.Parameters.AddWithValue("$bruto", pesoBrutoGramas);
        command.Parameters.AddWithValue("$cascado", pesoCascadoGramas);
        command.Parameters.AddWithValue("$custo", custoCentavos.HasValue ? custoCentavos.Value : DBNull.Value);
        command.Parameters.AddWithValue("$foto", (object?)caminhoFoto ?? DBNull.Value);
        command.Parameters.AddWithValue("$obs", (object?)observacao ?? DBNull.Value);
        command.Parameters.AddWithValue("$perdaBp", snapshotPerdaBp);
        command.Parameters.AddWithValue("$gramasSaco", snapshotGramasPorSaco);
        command.Parameters.AddWithValue("$tamanhoSaco", snapshotTamanhoSacoGramas);
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        return (long)command.ExecuteScalar()!;
    }

    public IReadOnlyList<CompraResumo> Listar(
        long? fornecedorId = null, DateOnly? inicio = null, DateOnly? fim = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.Id, c.FornecedorId, f.Nome, c.DataCompra, c.PesoBrutoGramas,
                   c.PesoCascadoGramas, c.CustoCentavos, c.Status, c.CaminhoFoto, c.Observacao,
                   c.SnapshotPerdaBp, c.SnapshotGramasPorSaco, c.SnapshotTamanhoSacoGramas,
                   c.CanceladaEm, c.MotivoCancelamento
            FROM CompraMandioca c
            JOIN Fornecedor f ON f.Id = c.FornecedorId
            WHERE ($fornecedor IS NULL OR c.FornecedorId = $fornecedor)
              AND ($inicio IS NULL OR c.DataCompra >= $inicio)
              AND ($fim IS NULL OR c.DataCompra <= $fim)
            ORDER BY c.DataCompra DESC, c.Id DESC;
            """;
        command.Parameters.AddWithValue("$fornecedor", fornecedorId.HasValue ? fornecedorId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$inicio", inicio.HasValue ? FormatoTemporal.FormatarDataOperacao(inicio.Value) : DBNull.Value);
        command.Parameters.AddWithValue("$fim", fim.HasValue ? FormatoTemporal.FormatarDataOperacao(fim.Value) : DBNull.Value);
        using var reader = command.ExecuteReader();
        var result = new List<CompraResumo>();
        while (reader.Read())
            result.Add(new CompraResumo(
                reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
                FormatoTemporal.LerDataOperacao(reader.GetString(3)),
                reader.GetInt64(4), reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetInt64(6),
                Enum.Parse<StatusCompra>(reader.GetString(7)),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt64(10), reader.GetInt64(11), reader.GetInt64(12),
                reader.IsDBNull(13) ? null : FormatoTemporal.LerInstanteUtc(reader.GetString(13)),
                reader.IsDBNull(14) ? null : reader.GetString(14)));
        return result;
    }

    public void Cancelar(long compraId, string motivo, DateTimeOffset canceladaEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CompraMandioca
            SET Status = 'Cancelada', CanceladaEm = $data, MotivoCancelamento = $motivo
            WHERE Id = $id AND Status <> 'Cancelada';
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarInstanteUtc(canceladaEm));
        command.Parameters.AddWithValue("$motivo", motivo);
        command.Parameters.AddWithValue("$id", compraId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Compra não encontrada ou já cancelada.");
    }

    public void MarcarPaga(long compraId)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CompraMandioca SET Status = 'Paga'
            WHERE Id = $id AND Status = 'Pendente';
            """;
        command.Parameters.AddWithValue("$id", compraId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Compra não encontrada ou não está pendente.");
    }
}
