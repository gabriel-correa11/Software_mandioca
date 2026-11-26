using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Repositories;

public sealed class ConferenciaSaldoRepository(BancoSqlite banco)
{
    public long Inserir(
        long contaId, DateOnly dataConferencia,
        long saldoInformadoCentavos, long saldoSistemaCentavos,
        string? observacao, DateTimeOffset criadoEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ConferenciaSaldo(ContaId, DataConferencia, SaldoInformadoCentavos,
                SaldoSistemaCentavos, DiferencaCentavos, Observacao, CriadoEm)
            VALUES ($conta, $data, $informado, $sistema, $diferenca, $obs, $criado);
            SELECT last_insert_rowid();
            """;
        var diferenca = saldoInformadoCentavos - saldoSistemaCentavos;
        command.Parameters.AddWithValue("$conta", contaId);
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(dataConferencia));
        command.Parameters.AddWithValue("$informado", saldoInformadoCentavos);
        command.Parameters.AddWithValue("$sistema", saldoSistemaCentavos);
        command.Parameters.AddWithValue("$diferenca", diferenca);
        command.Parameters.AddWithValue("$obs", (object?)observacao ?? DBNull.Value);
        command.Parameters.AddWithValue("$criado", FormatoTemporal.FormatarInstanteUtc(criadoEm));
        return (long)command.ExecuteScalar()!;
    }

    public IReadOnlyList<ConferenciaSaldo> Listar(long? contaId = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ContaId, DataConferencia, SaldoInformadoCentavos,
                   SaldoSistemaCentavos, DiferencaCentavos, Observacao, CriadoEm
            FROM ConferenciaSaldo
            WHERE ($conta IS NULL OR ContaId = $conta)
            ORDER BY DataConferencia DESC, Id DESC;
            """;
        command.Parameters.AddWithValue("$conta", contaId.HasValue ? contaId.Value : DBNull.Value);
        using var reader = command.ExecuteReader();
        var result = new List<ConferenciaSaldo>();
        while (reader.Read())
            result.Add(new ConferenciaSaldo(
                reader.GetInt64(0), reader.GetInt64(1),
                FormatoTemporal.LerDataOperacao(reader.GetString(2)),
                reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                FormatoTemporal.LerInstanteUtc(reader.GetString(7))));
        return result;
    }
}
