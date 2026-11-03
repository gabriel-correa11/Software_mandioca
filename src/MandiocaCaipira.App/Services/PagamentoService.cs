using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;

namespace MandiocaCaipira.App.Services;

public sealed class PagamentoService(
    BancoSqlite banco,
    PagamentoRepository pagamentos,
    VendaRepository vendas)
{
    public long Registrar(long clienteId, long valorCentavos, DateOnly dataOperacao,
        IReadOnlyCollection<AlocacaoSolicitada> alocacoes)
    {
        if (clienteId <= 0) throw new ArgumentException("Selecione um cliente.");
        if (valorCentavos <= 0) throw new ArgumentException("Informe um valor recebido maior que zero.");
        if (alocacoes.Any(a => a.ValorCentavos <= 0))
            throw new ArgumentException("As alocações devem ser maiores que zero.");
        if (alocacoes.GroupBy(a => a.VendaId).Any(g => g.Count() > 1))
            throw new ArgumentException("Cada venda deve aparecer uma única vez na distribuição.");
        if (alocacoes.Sum(a => checked(a.ValorCentavos)) > valorCentavos)
            throw new ArgumentException("A distribuição não pode ultrapassar o valor recebido.");

        var abertas = vendas.Listar(clienteId, somentePendentes: true).ToDictionary(v => v.Id);
        foreach (var alocacao in alocacoes)
        {
            if (!abertas.TryGetValue(alocacao.VendaId, out var venda))
                throw new ArgumentException("Uma venda selecionada não está aberta para este cliente.");
            if (alocacao.ValorCentavos > venda.SaldoCentavos)
                throw new ArgumentException("Uma alocação ultrapassa o saldo da venda.");
        }

        using var connection = banco.AbrirConexao();
        using var transaction = connection.BeginTransaction();
        var agora = DateTimeOffset.UtcNow;
        try
        {
            var id = pagamentos.Inserir(connection, transaction, clienteId, valorCentavos, dataOperacao, agora);
            foreach (var alocacao in alocacoes)
                pagamentos.InserirAlocacao(connection, transaction, id, alocacao.VendaId,
                    alocacao.ValorCentavos, agora);
            transaction.Commit();
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<PagamentoResumo> Listar(long? clienteId = null, DateOnly? inicio = null, DateOnly? fim = null) =>
        pagamentos.Listar(clienteId, inicio, fim);

    public void Estornar(long pagamentoId, string motivo)
    {
        motivo = motivo.Trim();
        if (motivo.Length < 3) throw new ArgumentException("Informe o motivo do estorno.");
        if (motivo.Length > 500) throw new ArgumentException("Motivo muito longo (máximo 500 caracteres).");
        pagamentos.Estornar(pagamentoId, motivo, DateTimeOffset.UtcNow);
    }
}
