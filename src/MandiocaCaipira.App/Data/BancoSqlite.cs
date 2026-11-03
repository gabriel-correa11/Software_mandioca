using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.App.Data;

public sealed class BancoSqlite(string caminhoBanco)
{
    public string CaminhoBanco { get; } = Path.GetFullPath(caminhoBanco);

    public SqliteConnection AbrirConexao()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = CaminhoBanco,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.CreateFunction<long, long, long>(
            "sale_total_cents", CalculoFinanceiro.TotalCentavos, isDeterministic: true);
        connection.CreateFunction<string?, bool>(
            "is_operation_date", FormatoTemporal.DataOperacaoValida, isDeterministic: true);
        connection.CreateFunction<string?, bool>(
            "is_utc_instant", FormatoTemporal.InstanteUtcValido, isDeterministic: true);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();
        return connection;
    }

    public static void LiberarConexoes(string caminhoBanco)
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        RemoverArquivoAuxiliar(caminhoBanco + "-wal");
        RemoverArquivoAuxiliar(caminhoBanco + "-shm");
    }

    private static void RemoverArquivoAuxiliar(string caminho)
    {
        try { if (File.Exists(caminho)) File.Delete(caminho); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
