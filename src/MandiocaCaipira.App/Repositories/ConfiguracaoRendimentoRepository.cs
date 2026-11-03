using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Repositories;

public sealed class ConfiguracaoRendimentoRepository(BancoSqlite banco)
{
    public ConfiguracaoRendimento ObterVigente(DateOnly data)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, VigenteDesde, PercentualPadraoPerdaBp, GramasPorSacoPadrao, TamanhoSacoGramas
            FROM ConfiguracaoRendimento
            WHERE VigenteDesde <= $data
            ORDER BY VigenteDesde DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(data));
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            throw new InvalidOperationException("Nenhuma configuração de rendimento cadastrada.");
        return new ConfiguracaoRendimento(
            reader.GetInt64(0),
            FormatoTemporal.LerDataOperacao(reader.GetString(1)),
            reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    public IReadOnlyList<ConfiguracaoRendimento> Listar()
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, VigenteDesde, PercentualPadraoPerdaBp, GramasPorSacoPadrao, TamanhoSacoGramas
            FROM ConfiguracaoRendimento
            ORDER BY VigenteDesde DESC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<ConfiguracaoRendimento>();
        while (reader.Read())
            result.Add(new ConfiguracaoRendimento(
                reader.GetInt64(0),
                FormatoTemporal.LerDataOperacao(reader.GetString(1)),
                reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4)));
        return result;
    }

    public long Inserir(DateOnly vigenteDesde, long percentualPerdaBp, long gramasPorSaco, long tamanhoSacoGramas)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ConfiguracaoRendimento(VigenteDesde, PercentualPadraoPerdaBp, GramasPorSacoPadrao, TamanhoSacoGramas)
            VALUES ($vigente, $perda, $gramasSaco, $tamanhoSaco);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$vigente", FormatoTemporal.FormatarDataOperacao(vigenteDesde));
        command.Parameters.AddWithValue("$perda", percentualPerdaBp);
        command.Parameters.AddWithValue("$gramasSaco", gramasPorSaco);
        command.Parameters.AddWithValue("$tamanhoSaco", tamanhoSacoGramas);
        return (long)command.ExecuteScalar()!;
    }
}
