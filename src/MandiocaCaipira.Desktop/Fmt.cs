using System.Globalization;

namespace MandiocaCaipira.Desktop;

public static class Fmt
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Money(long cents) => (cents / 100m).ToString("C", PtBr);
    public static string Kg(long grams) => $"{grams / 1000m:N3} kg";
}
