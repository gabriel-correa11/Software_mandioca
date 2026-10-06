namespace MandiocaCaipira.App.Services;

public enum AcaoImportacaoCliente
{
    CriarNovo,
    UsarExistente,
    Ignorar,
    Revisar
}

public sealed class CandidatoImportacaoCliente
{
    public required string NomeEncontrado { get; init; }
    public required string NomeNormalizado { get; set; }
    public required string NomeParaCadastro { get; set; }
    public int Ocorrencias { get; init; }
    public long? ClienteCorrespondenteId { get; set; }
    public string? ClienteCorrespondenteNome { get; set; }
    public required string Situacao { get; set; }
    public AcaoImportacaoCliente Acao { get; set; }
}

public sealed record ResultadoImportacaoClientes(
    int Encontrados,
    int Criados,
    int Associados,
    int Ignorados);
