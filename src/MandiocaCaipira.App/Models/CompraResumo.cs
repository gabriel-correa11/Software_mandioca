namespace MandiocaCaipira.App.Models;

public sealed record CompraResumo(
    long Id,
    long FornecedorId,
    string Fornecedor,
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
    DateTimeOffset? CanceladaEm,
    string? MotivoCancelamento);
