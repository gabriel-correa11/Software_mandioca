namespace MandiocaCaipira.App.Models;

/// <summary>
/// Parametros padrao de rendimento vigentes a partir de uma data.
/// Percentual armazenado em pontos base (3000 = 30,00%).
/// Pesos armazenados em gramas.
/// </summary>
public sealed record ConfiguracaoRendimento(
    long Id,
    DateOnly VigenteDesde,
    long PercentualPadraoPerdaBp,
    long GramasPorSacoPadrao,
    long TamanhoSacoGramas);
