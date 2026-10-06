using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Repositories;

public sealed class ResumoRepository(BancoSqlite banco)
{
    public ResumoDiario Obter(DateOnly data)
    {
        using var connection = banco.AbrirConexao();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT COUNT(*) FROM Venda WHERE DataOperacao=$data AND CanceladaEm IS NULL),
              (SELECT COALESCE(SUM(QuantidadeGramas),0) FROM Venda WHERE DataOperacao=$data AND CanceladaEm IS NULL),
              (SELECT COALESCE(SUM(sale_total_cents(QuantidadeGramas,PrecoCentavosPorKg)),0) FROM Venda WHERE DataOperacao=$data AND CanceladaEm IS NULL),
              (SELECT COALESCE(SUM(ValorCentavos),0) FROM Pagamento WHERE DataOperacao=$data AND EstornadoEm IS NULL),
              (SELECT COUNT(*) FROM Venda WHERE DataOperacao=$data AND CanceladaEm IS NOT NULL);
            """;
        command.Parameters.AddWithValue("$data", FormatoTemporal.FormatarDataOperacao(data));
        using var reader = command.ExecuteReader();
        reader.Read();
        return new ResumoDiario(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt32(4));
    }
}
