namespace MandiocaCaipira.App.Models;

public sealed record AlocacaoPagamento(
    long Id,
    long PagamentoId,
    long VendaId,
    long ValorCentavos,
    DateTimeOffset CriadoEm);
