namespace MandiocaCaipira.App.Services;

public static class CalculoFinanceiro
{
    public static long TotalCentavos(long quantidadeGramas, long precoCentavosPorKg)
    {
        if (quantidadeGramas <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeGramas), "A quantidade deve ser positiva.");
        if (precoCentavosPorKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(precoCentavosPorKg), "O preço deve ser positivo.");

        decimal centavos;
        try
        {
            centavos = decimal.Round(
                (decimal)quantidadeGramas * precoCentavosPorKg / 1000m,
                0,
                MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidadeGramas), "Total fora da faixa válida.");
        }

        if (centavos < 1 || centavos > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(quantidadeGramas), "Total fora da faixa válida.");

        return checked((long)centavos);
    }

    public static long SaldoCentavos(long totalCentavos, long recebidoCentavos)
    {
        if (totalCentavos < 1 || recebidoCentavos < 0 || recebidoCentavos > totalCentavos)
            throw new ArgumentOutOfRangeException(nameof(recebidoCentavos));
        return totalCentavos - recebidoCentavos;
    }
}
