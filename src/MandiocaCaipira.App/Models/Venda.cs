namespace MandiocaCaipira.App.Models;

public sealed record Venda(
    long Id,
    long ClienteId,
    long QuantidadeGramas,
    long PrecoCentavosPorKg,
    DateOnly DataOperacao,
    DateTimeOffset CriadoEm,
    OrigemRegistro Origem,
    string? ReferenciaExterna,
    DateTimeOffset? CanceladaEm,
    string? MotivoCancelamento);
