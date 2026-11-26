using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Repositories;

public sealed class MovimentoCaixaRepository(BancoSqlite banco)
{
    public long Inserir(
        SqliteConnection connection, SqliteTransaction transaction,
        TipoMovimento tipo, DateOnly dataMovimento, long contaId, long categoriaId,
        long valorCentavos, string descricao, string? beneficiario,
        OrigemMovimento origemTipo, long? origemId, DateTimeOffset dataRegistro)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO MovimentoCaixa(Tipo, DataMovimento, DataRegistro, ContaId, CategoriaId,
                ValorCentavos, Descricao, Beneficiario, OrigemTipo, OrigemId)
            VALUES ($tipo, $data, $registro, $conta, $categoria,
                $valor, $descricao, $beneficiario, $origem, $origemId);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$tipo", tipo.ToString());
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(dataMovimento));
        command.Parameters.AddWithValue("$registro", FormatoTemporal.FormatarInstanteUtc(dataRegistro));
        command.Parameters.AddWithValue("$conta", contaId);
        command.Parameters.AddWithValue("$categoria", categoriaId);
        command.Parameters.AddWithValue("$valor", valorCentavos);
        command.Parameters.AddWithValue("$descricao", descricao);
        command.Parameters.AddWithValue("$beneficiario", (object?)beneficiario ?? DBNull.Value);
        command.Parameters.AddWithValue("$origem", origemTipo.ToString());
        command.Parameters.AddWithValue("$origemId", origemId.HasValue ? origemId.Value : DBNull.Value);
        return (long)command.ExecuteScalar()!;
    }

    public bool ExisteMovimentoOrigem(OrigemMovimento origemTipo, long origemId)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM MovimentoCaixa
            WHERE OrigemTipo = $tipo AND OrigemId = $id AND Status = 'Ativo';
            """;
        command.Parameters.AddWithValue("$tipo", origemTipo.ToString());
        command.Parameters.AddWithValue("$id", origemId);
        return (long)command.ExecuteScalar()! > 0;
    }

    public IReadOnlyList<MovimentoCaixaResumo> Listar(
        DateOnly? inicio = null, DateOnly? fim = null,
        long? contaId = null, TipoMovimento? tipo = null, long? categoriaId = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.Id, m.Tipo, m.DataMovimento, c.Nome, cat.Nome, cat.Natureza,
                   m.ValorCentavos, m.Descricao, m.Beneficiario, m.OrigemTipo, m.Status
            FROM MovimentoCaixa m
            JOIN ContaCaixa c ON c.Id = m.ContaId
            JOIN CategoriaMovimento cat ON cat.Id = m.CategoriaId
            WHERE ($inicio IS NULL OR m.DataMovimento >= $inicio)
              AND ($fim IS NULL OR m.DataMovimento <= $fim)
              AND ($conta IS NULL OR m.ContaId = $conta)
              AND ($tipo IS NULL OR m.Tipo = $tipo)
              AND ($categoria IS NULL OR m.CategoriaId = $categoria)
            ORDER BY m.DataMovimento DESC, m.Id DESC;
            """;
        command.Parameters.AddWithValue("$inicio", inicio.HasValue ? FormatoTemporal.FormatarDataOperacao(inicio.Value) : DBNull.Value);
        command.Parameters.AddWithValue("$fim", fim.HasValue ? FormatoTemporal.FormatarDataOperacao(fim.Value) : DBNull.Value);
        command.Parameters.AddWithValue("$conta", contaId.HasValue ? contaId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$tipo", tipo.HasValue ? tipo.Value.ToString() : DBNull.Value);
        command.Parameters.AddWithValue("$categoria", categoriaId.HasValue ? categoriaId.Value : DBNull.Value);
        using var reader = command.ExecuteReader();
        var result = new List<MovimentoCaixaResumo>();
        while (reader.Read())
            result.Add(new MovimentoCaixaResumo(
                reader.GetInt64(0),
                Enum.Parse<TipoMovimento>(reader.GetString(1)),
                FormatoTemporal.LerDataOperacao(reader.GetString(2)),
                reader.GetString(3), reader.GetString(4),
                Enum.Parse<NaturezaCategoria>(reader.GetString(5)),
                reader.GetInt64(6), reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                Enum.Parse<OrigemMovimento>(reader.GetString(9)),
                Enum.Parse<StatusMovimento>(reader.GetString(10))));
        return result;
    }

    public void Cancelar(long movimentoId, string motivo, DateTimeOffset canceladoEm)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE MovimentoCaixa
            SET Status = 'Cancelado', CanceladoEm = $data, MotivoCancelamento = $motivo
            WHERE Id = $id AND Status = 'Ativo';
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarInstanteUtc(canceladoEm));
        command.Parameters.AddWithValue("$motivo", motivo);
        command.Parameters.AddWithValue("$id", movimentoId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Movimento não encontrado ou já cancelado.");
    }

    public long SaldoConta(long contaId, DateOnly? dataAte = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(CASE WHEN Tipo = 'Entrada' THEN ValorCentavos ELSE 0 END), 0)
              - COALESCE(SUM(CASE WHEN Tipo = 'Saida' THEN ValorCentavos ELSE 0 END), 0)
            FROM MovimentoCaixa
            WHERE ContaId = $conta AND Status = 'Ativo'
              AND ($ate IS NULL OR DataMovimento <= $ate);
            """;
        command.Parameters.AddWithValue("$conta", contaId);
        command.Parameters.AddWithValue("$ate", dataAte.HasValue ? FormatoTemporal.FormatarDataOperacao(dataAte.Value) : DBNull.Value);
        return (long)command.ExecuteScalar()!;
    }

    public long SaldoTotal(DateOnly? dataAte = null)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(CASE WHEN Tipo = 'Entrada' THEN ValorCentavos ELSE 0 END), 0)
              - COALESCE(SUM(CASE WHEN Tipo = 'Saida' THEN ValorCentavos ELSE 0 END), 0)
            FROM MovimentoCaixa
            WHERE Status = 'Ativo'
              AND ($ate IS NULL OR DataMovimento <= $ate);
            """;
        command.Parameters.AddWithValue("$ate", dataAte.HasValue ? FormatoTemporal.FormatarDataOperacao(dataAte.Value) : DBNull.Value);
        return (long)command.ExecuteScalar()!;
    }
}
