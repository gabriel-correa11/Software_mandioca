using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Services;

/// <summary>
/// Função pura: recebe dados, devolve RendimentoLote. Sem banco, sem UI.
/// </summary>
public static class RendimentoService
{
    public static RendimentoLote Calcular(
        long pesoBrutoGramas, long pesoCascadoGramas,
        long percentualPadraoPerdaBp, long gramasPorSacoPadrao, long tamanhoSacoGramas)
    {
        if (pesoBrutoGramas <= 0)
            throw new ArgumentException("Peso bruto deve ser maior que zero.");
        if (pesoCascadoGramas <= 0)
            throw new ArgumentException("Peso cascado deve ser maior que zero.");
        if (pesoCascadoGramas > pesoBrutoGramas)
            throw new ArgumentException("Peso cascado não pode ser maior que o bruto.");
        if (tamanhoSacoGramas <= 0)
            throw new ArgumentException("Tamanho do saco deve ser maior que zero.");

        var perdaGramas = pesoBrutoGramas - pesoCascadoGramas;
        var perdaPercentual = perdaGramas / (decimal)pesoBrutoGramas;
        var padrao = percentualPadraoPerdaBp / 10_000m;

        var pesoEsperadoGramas = (long)(pesoBrutoGramas * (10_000 - percentualPadraoPerdaBp) / 10_000);
        var deficitGramas = pesoEsperadoGramas - pesoCascadoGramas;
        var desvioPercentualPerda = perdaPercentual - padrao;

        var sacosEquivalentes = CalcularSacos(pesoBrutoGramas, tamanhoSacoGramas);
        var gramasPorSacoObtido = sacosEquivalentes > 0
            ? pesoCascadoGramas / sacosEquivalentes
            : 0m;
        var desvioGramasPorSaco = gramasPorSacoObtido - gramasPorSacoPadrao;

        return new RendimentoLote(
            pesoBrutoGramas, pesoCascadoGramas, perdaGramas, perdaPercentual,
            pesoEsperadoGramas, deficitGramas, desvioPercentualPerda,
            sacosEquivalentes, gramasPorSacoObtido, desvioGramasPorSaco,
            perdaPercentual > padrao);
    }

    /// <summary>
    /// Método isolado para trocar a regra de sacos sem mexer no resto.
    /// PENDENTE CONFIRMAÇÃO: usando PesoBruto / TamanhoSaco (número de sacos brutos).
    /// </summary>
    public static decimal CalcularSacos(long pesoBrutoGramas, long tamanhoSacoGramas) =>
        pesoBrutoGramas / (decimal)tamanhoSacoGramas;
}
