using System.Globalization;

namespace MandiocaCaipira.App.Data;

public static class FormatoTemporal
{
    private const string FormatoData = "yyyy-MM-dd";
    private const string FormatoInstanteUtc = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    public static string FormatarDataOperacao(DateOnly data) =>
        data.ToString(FormatoData, CultureInfo.InvariantCulture);

    public static string FormatarInstanteUtc(DateTimeOffset instante) =>
        instante.ToUniversalTime().ToString(FormatoInstanteUtc, CultureInfo.InvariantCulture);

    public static bool DataOperacaoValida(string? valor) =>
        valor is not null && DateOnly.TryParseExact(
            valor,
            FormatoData,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);

    public static bool InstanteUtcValido(string? valor) =>
        valor is not null && DateTimeOffset.TryParseExact(
            valor,
            FormatoInstanteUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _);

    public static DateOnly LerDataOperacao(string valor) =>
        DateOnly.ParseExact(valor, FormatoData, CultureInfo.InvariantCulture);

    public static DateTimeOffset LerInstanteUtc(string valor) =>
        DateTimeOffset.ParseExact(
            valor,
            FormatoInstanteUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
