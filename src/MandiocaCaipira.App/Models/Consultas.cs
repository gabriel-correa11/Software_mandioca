namespace MandiocaCaipira.App.Models;

public sealed record VendaResumo(
    long Id, long ClienteId, string Cliente, DateOnly DataOperacao,
    long QuantidadeGramas, long PrecoCentavosPorKg, long TotalCentavos,
    long RecebidoCentavos, bool Cancelada, DateTimeOffset? CanceladaEm,
    string? MotivoCancelamento)
{
    public long SaldoCentavos => Cancelada ? 0 : TotalCentavos - RecebidoCentavos;
}

public sealed record PagamentoResumo(
    long Id, long ClienteId, string Cliente, DateOnly DataOperacao,
    long ValorCentavos, long AlocadoCentavos, DateTimeOffset? EstornadoEm,
    string? MotivoEstorno)
{
    public long CreditoCentavos => EstornadoEm is null ? ValorCentavos - AlocadoCentavos : 0;
}

public sealed record AlocacaoSolicitada(long VendaId, long ValorCentavos);

public sealed record ResumoDiario(
    int VendasRealizadas, long QuantidadeGramas, long ValorVendidoCentavos,
    long RecebimentosCentavos, int VendasCanceladas);

public sealed record DestinacaoPagamento(
    long VendaId, DateOnly DataVenda, long ValorVendaCentavos, long ValorAplicadoCentavos);

public sealed record PagamentoDocumento(
    PagamentoResumo Pagamento, IReadOnlyList<DestinacaoPagamento> Destinacoes);
