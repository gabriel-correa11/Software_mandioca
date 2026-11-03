namespace MandiocaCaipira.App.Models;

public sealed record CompraMandioca(
    long Id,
    long FornecedorId,
    DateOnly DataCompra,
    long PesoBrutoGramas,
    long PesoCascadoGramas,
    long? CustoCentavos,
    StatusCompra Status,
    string? CaminhoFoto,
    string? Observacao,
    long SnapshotPerdaBp,
    long SnapshotGramasPorSaco,
    long SnapshotTamanhoSacoGramas,
    DateTimeOffset CriadoEm,
    DateTimeOffset? CanceladaEm,
    string? MotivoCancelamento);
