using MandiocaCaipira.App.Services;

namespace MandiocaCaipira.Tests;

public sealed class RendimentoServiceTests
{
    // Caso do relatorio de referencia
    // Bruto=340kg, Cascado=226,13kg, Padrao=30%, Saco=30kg, KgPorSacoPadrao=21kg
    [Fact]
    public void CasoRelatorioDeReferencia()
    {
        var r = RendimentoService.Calcular(
            pesoBrutoGramas: 340_000,
            pesoCascadoGramas: 226_130,
            percentualPadraoPerdaBp: 3000,
            gramasPorSacoPadrao: 21_000,
            tamanhoSacoGramas: 30_000);

        Assert.Equal(113_870, r.PerdaGramas);
        // 113870 / 340000 = 0.33491...
        Assert.Equal(0.3349m, Math.Round(r.PerdaPercentual, 4));
        Assert.Equal(238_000, r.PesoEsperadoGramas);
        Assert.Equal(11_870, r.DeficitGramas);
        // 0.3349 - 0.3000 = 0.0349
        Assert.Equal(0.0349m, Math.Round(r.DesvioPercentualPerda, 4));
        // 340000 / 30000 = 11.3333...
        Assert.Equal(11.33m, Math.Round(r.SacosEquivalentes, 2));
        Assert.True(r.PerdaAcimaDoPadrao);
    }

    [Fact]
    public void PesoCascadoIgualAoBrutoSemPerdaSemDeficit()
    {
        var r = RendimentoService.Calcular(100_000, 100_000, 3000, 21_000, 30_000);

        Assert.Equal(0, r.PerdaGramas);
        Assert.Equal(0m, r.PerdaPercentual);
        Assert.Equal(70_000, r.PesoEsperadoGramas);
        Assert.Equal(-30_000, r.DeficitGramas);
        Assert.False(r.PerdaAcimaDoPadrao);
    }

    [Fact]
    public void PesoCascadoMaiorQueBrutoRejeita()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            RendimentoService.Calcular(100_000, 100_001, 3000, 21_000, 30_000));
        Assert.Contains("maior que o bruto", ex.Message);
    }

    [Fact]
    public void PesoBrutoZeroRejeita()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            RendimentoService.Calcular(0, 0, 3000, 21_000, 30_000));
        Assert.Contains("Peso bruto", ex.Message);
    }

    [Fact]
    public void PerdaExatamenteNoPadraoNaoEAcima()
    {
        // 30% de perda exata: bruto=100kg, cascado=70kg, padrao=30%
        var r = RendimentoService.Calcular(100_000, 70_000, 3000, 21_000, 30_000);

        Assert.Equal(30_000, r.PerdaGramas);
        Assert.Equal(0.30m, r.PerdaPercentual);
        Assert.Equal(70_000, r.PesoEsperadoGramas);
        Assert.Equal(0, r.DeficitGramas);
        Assert.False(r.PerdaAcimaDoPadrao);
    }

    [Fact]
    public void SacosEquivalentesUsaPesoBruto()
    {
        // 340kg bruto / 30kg saco = 11.33 sacos
        var sacos = RendimentoService.CalcularSacos(340_000, 30_000);
        Assert.Equal(11.33m, Math.Round(sacos, 2));
    }
}
