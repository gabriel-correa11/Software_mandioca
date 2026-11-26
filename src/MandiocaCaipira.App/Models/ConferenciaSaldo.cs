namespace MandiocaCaipira.App.Models;

public sealed record ConferenciaSaldo(
    long Id,
    long ContaId,
    DateOnly DataConferencia,
    long SaldoInformadoCentavos,
    long SaldoSistemaCentavos,
    long DiferencaCentavos,
    string? Observacao,
    DateTimeOffset CriadoEm);
