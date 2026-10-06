using FuzzySharp;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using Serilog;

namespace MandiocaCaipira.App.Services;

public sealed class ImportacaoClientesService(
    LeitorArquivoClientes leitor,
    ClienteRepository repository,
    ILogger? logger = null)
{
    private readonly ILogger _logger = logger ?? Log.Logger;

    public async Task<IReadOnlyList<CandidatoImportacaoCliente>> AnalisarAsync(
        string caminho, CancellationToken cancellationToken = default)
    {
        _logger.Information("Iniciando leitura para importação de clientes; formato {Formato}", Path.GetExtension(caminho));
        try
        {
            var nomes = await leitor.LerAsync(caminho, cancellationToken);
            var clientes = repository.Listar(null, incluirInativos: true);
            var candidatos = nomes
                .GroupBy(NormalizadorNomeCliente.NormalizarParaComparacao)
                .Where(group => group.Key.Length >= 2)
                .Select(group => CriarCandidato(group.First(), group.Key, group.Count(), clientes))
                .OrderBy(candidate => candidate.NomeParaCadastro, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            _logger.Information("Leitura para importação concluída com {QuantidadeCandidatos} candidatos", candidatos.Count);
            return candidatos;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Falha ao analisar arquivo para importação de clientes");
            throw;
        }
    }

    public void Reavaliar(CandidatoImportacaoCliente candidato)
    {
        var nome = NormalizadorNomeCliente.NormalizarParaApresentacao(candidato.NomeParaCadastro);
        var normalizado = NormalizadorNomeCliente.NormalizarParaComparacao(nome);
        if (normalizado.Length < 2)
            throw new ArgumentException("Informe um nome válido com pelo menos 2 caracteres.");
        var atualizado = CriarCandidato(nome, normalizado, candidato.Ocorrencias, repository.Listar(null, true));
        candidato.NomeParaCadastro = atualizado.NomeParaCadastro;
        candidato.NomeNormalizado = atualizado.NomeNormalizado;
        candidato.ClienteCorrespondenteId = atualizado.ClienteCorrespondenteId;
        candidato.ClienteCorrespondenteNome = atualizado.ClienteCorrespondenteNome;
        candidato.Situacao = atualizado.Situacao;
        candidato.Acao = atualizado.Acao;
    }

    public ResultadoImportacaoClientes Confirmar(IReadOnlyCollection<CandidatoImportacaoCliente> candidatos)
    {
        if (candidatos.Count == 0)
            throw new InvalidOperationException("Não há clientes para importar.");
        if (candidatos.Any(candidate => candidate.Acao == AcaoImportacaoCliente.Revisar))
            throw new InvalidOperationException("Revise todos os possíveis duplicados antes de confirmar.");

        var existentes = repository.Listar(null, true).ToDictionary(cliente => cliente.Id);
        foreach (var candidato in candidatos.Where(c => c.Acao == AcaoImportacaoCliente.UsarExistente))
            if (candidato.ClienteCorrespondenteId is not long id || !existentes.ContainsKey(id))
                throw new InvalidOperationException($"Escolha o cliente existente correspondente a “{candidato.NomeParaCadastro}”.");

        var novos = candidatos.Where(c => c.Acao == AcaoImportacaoCliente.CriarNovo).ToList();
        foreach (var candidato in novos)
        {
            candidato.NomeParaCadastro = NormalizadorNomeCliente.NormalizarParaApresentacao(candidato.NomeParaCadastro);
            candidato.NomeNormalizado = NormalizadorNomeCliente.NormalizarParaComparacao(candidato.NomeParaCadastro);
            if (candidato.NomeNormalizado.Length < 2)
                throw new InvalidOperationException("Todos os novos clientes precisam ter um nome válido.");
        }
        if (novos.GroupBy(c => c.NomeNormalizado).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Existem nomes repetidos entre os clientes marcados para criação.");
        var normalizadosExistentes = existentes.Values.Select(c => NormalizadorNomeCliente.NormalizarParaComparacao(c.Nome)).ToHashSet();
        if (novos.Any(c => normalizadosExistentes.Contains(c.NomeNormalizado)))
            throw new InvalidOperationException("Um cliente marcado como novo já está cadastrado. Atualize a revisão e tente novamente.");

        repository.InserirImportadosAtomico(novos.Select(c => c.NomeParaCadastro).ToList(), DateTimeOffset.UtcNow);
        var result = new ResultadoImportacaoClientes(
            candidatos.Sum(c => c.Ocorrencias), novos.Count,
            candidatos.Count(c => c.Acao == AcaoImportacaoCliente.UsarExistente),
            candidatos.Count(c => c.Acao == AcaoImportacaoCliente.Ignorar));
        _logger.Information("Importação de clientes concluída: {Criados} criados, {Associados} associados, {Ignorados} ignorados",
            result.Criados, result.Associados, result.Ignorados);
        return result;
    }

    private static CandidatoImportacaoCliente CriarCandidato(string nome, string normalizado, int ocorrencias, IReadOnlyList<Cliente> clientes)
    {
        var exact = clientes.FirstOrDefault(cliente => NormalizadorNomeCliente.NormalizarParaComparacao(cliente.Nome) == normalizado);
        if (exact is not null)
            return Novo(nome, normalizado, ocorrencias, exact, "Já cadastrado", AcaoImportacaoCliente.UsarExistente);

        var suggestion = clientes
            .Select(cliente => (Cliente: cliente, Score: Fuzz.Ratio(normalizado, NormalizadorNomeCliente.NormalizarParaComparacao(cliente.Nome))))
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();
        if (suggestion.Cliente is not null && suggestion.Score >= 88)
            return Novo(nome, normalizado, ocorrencias, suggestion.Cliente,
                suggestion.Score >= 95 ? "Correspondência forte — revisar" : "Possível duplicado — revisar",
                AcaoImportacaoCliente.Revisar);
        return Novo(nome, normalizado, ocorrencias, null, ocorrencias > 1 ? "Novo (repetido no arquivo)" : "Novo", AcaoImportacaoCliente.CriarNovo);
    }

    private static CandidatoImportacaoCliente Novo(string nome, string normalizado, int ocorrencias,
        Cliente? cliente, string situacao, AcaoImportacaoCliente acao) => new()
    {
        NomeEncontrado = nome,
        NomeParaCadastro = NormalizadorNomeCliente.NormalizarParaApresentacao(nome),
        NomeNormalizado = normalizado,
        Ocorrencias = ocorrencias,
        ClienteCorrespondenteId = cliente?.Id,
        ClienteCorrespondenteNome = cliente?.Nome,
        Situacao = situacao,
        Acao = acao
    };
}
