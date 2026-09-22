namespace MandiocaCaipira.App.Models;

public sealed record Pagamento(
    long Id,
    long ClienteId,
    long ValorCentavos,
    DateOnly DataOperacao,
    DateTimeOffset CriadoEm,
    OrigemRegistro Origem,
    string? ReferenciaExterna,
    DateTimeOffset? EstornadoEm,
    string? MotivoEstorno);
