namespace MandiocaCaipira.App.Models;

public sealed record Cliente(
    long Id,
    string Nome,
    bool Ativo,
    OrigemRegistro Origem,
    string? ReferenciaExterna,
    DateTimeOffset CriadoEm);
