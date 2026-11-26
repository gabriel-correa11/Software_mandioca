using MandiocaCaipira.App.Models;

namespace MandiocaCaipira.App.Services;

public static class ResumoCaixaService
{
    public static long SaldoConta(IEnumerable<MovimentoCaixaResumo> movimentos, string conta, DateOnly? dataAte = null)
    {
        return movimentos
            .Where(m => m.Status == StatusMovimento.Ativo
                && m.Conta == conta
                && (!dataAte.HasValue || m.DataMovimento <= dataAte.Value))
            .Sum(m => m.Tipo == TipoMovimento.Entrada ? m.ValorCentavos : -m.ValorCentavos);
    }

    public static long SaldoTotal(IEnumerable<MovimentoCaixaResumo> movimentos, DateOnly? dataAte = null)
    {
        return movimentos
            .Where(m => m.Status == StatusMovimento.Ativo
                && (!dataAte.HasValue || m.DataMovimento <= dataAte.Value))
            .Sum(m => m.Tipo == TipoMovimento.Entrada ? m.ValorCentavos : -m.ValorCentavos);
    }

    public static (long Entradas, long Saidas, long Saldo) TotalPeriodo(
        IEnumerable<MovimentoCaixaResumo> movimentos, DateOnly inicio, DateOnly fim)
    {
        var ativos = movimentos
            .Where(m => m.Status == StatusMovimento.Ativo
                && m.DataMovimento >= inicio && m.DataMovimento <= fim)
            .ToList();

        var entradas = ativos.Where(m => m.Tipo == TipoMovimento.Entrada).Sum(m => m.ValorCentavos);
        var saidas = ativos.Where(m => m.Tipo == TipoMovimento.Saida).Sum(m => m.ValorCentavos);
        return (entradas, saidas, entradas - saidas);
    }

    public static IReadOnlyList<(string Categoria, long Total)> TotalPorCategoria(
        IEnumerable<MovimentoCaixaResumo> movimentos, DateOnly inicio, DateOnly fim)
    {
        return movimentos
            .Where(m => m.Status == StatusMovimento.Ativo
                && m.DataMovimento >= inicio && m.DataMovimento <= fim)
            .GroupBy(m => m.Categoria)
            .Select(g => (g.Key, g.Sum(m => m.Tipo == TipoMovimento.Entrada ? m.ValorCentavos : -m.ValorCentavos)))
            .OrderByDescending(x => Math.Abs(x.Item2))
            .ToList();
    }

    public static long ResultadoOperacional(
        IEnumerable<MovimentoCaixaResumo> movimentos, DateOnly inicio, DateOnly fim)
    {
        return movimentos
            .Where(m => m.Status == StatusMovimento.Ativo
                && m.Natureza == NaturezaCategoria.Operacional
                && m.DataMovimento >= inicio && m.DataMovimento <= fim)
            .Sum(m => m.Tipo == TipoMovimento.Entrada ? m.ValorCentavos : -m.ValorCentavos);
    }

    public static long DiferencaConferencia(long saldoInformadoCentavos, long saldoSistemaCentavos)
    {
        return saldoInformadoCentavos - saldoSistemaCentavos;
    }
}
