namespace MandiocaCaipira.App.Models;

/// <summary>
/// Objeto calculado (nao persiste). Todos os pesos em gramas,
/// percentuais em fracao decimal (0.30 = 30%).
/// </summary>
public sealed record RendimentoLote(
    long PesoBrutoGramas,
    long PesoCascadoGramas,
    long PerdaGramas,
    decimal PerdaPercentual,
    long PesoEsperadoGramas,
    long DeficitGramas,
    decimal DesvioPercentualPerda,
    decimal SacosEquivalentes,
    decimal GramasPorSacoObtido,
    decimal DesvioGramasPorSaco,
    bool PerdaAcimaDoPadrao);
