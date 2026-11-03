using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;

namespace MandiocaCaipira.App.Services;

public sealed class VendaService(
    BancoSqlite banco,
    VendaRepository vendas,
    PagamentoRepository pagamentos)
{
    public long CalcularTotal(long quantidadeGramas, long precoCentavosPorKg) =>
        CalculoFinanceiro.TotalCentavos(quantidadeGramas, precoCentavosPorKg);

    public long Registrar(
        long clienteId, long quantidadeGramas, long precoCentavosPorKg,
        DateOnly dataOperacao, long pagamentoInicialCentavos = 0)
    {
        if (clienteId <= 0) throw new ArgumentException("Selecione um cliente ativo.");
        var total = CalcularTotal(quantidadeGramas, precoCentavosPorKg);
        if (pagamentoInicialCentavos < 0)
            throw new ArgumentOutOfRangeException(nameof(pagamentoInicialCentavos));

        using var connection = banco.AbrirConexao();
        using var transaction = connection.BeginTransaction();
        var agora = DateTimeOffset.UtcNow;
        try
        {
            var vendaId = vendas.Inserir(connection, transaction, clienteId, quantidadeGramas,
                precoCentavosPorKg, dataOperacao, agora);
            if (pagamentoInicialCentavos > 0)
            {
                var pagamentoId = pagamentos.Inserir(connection, transaction, clienteId,
                    pagamentoInicialCentavos, dataOperacao, agora);
                pagamentos.InserirAlocacao(connection, transaction, pagamentoId, vendaId,
                    Math.Min(total, pagamentoInicialCentavos), agora);
            }
            transaction.Commit();
            return vendaId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<VendaResumo> Listar(long? clienteId = null, DateOnly? inicio = null,
        DateOnly? fim = null, bool somentePendentes = false) =>
        vendas.Listar(clienteId, inicio, fim, somentePendentes);

    public void Cancelar(long vendaId, string motivo)
    {
        motivo = motivo.Trim();
        if (motivo.Length < 3) throw new ArgumentException("Informe o motivo do cancelamento.");
        if (motivo.Length > 500) throw new ArgumentException("Motivo muito longo (máximo 500 caracteres).");
        vendas.Cancelar(vendaId, motivo, DateTimeOffset.UtcNow);
    }
}
