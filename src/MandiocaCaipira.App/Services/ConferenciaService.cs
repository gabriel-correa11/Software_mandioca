using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using Serilog;

namespace MandiocaCaipira.App.Services;

public sealed class ConferenciaService(
    MovimentoCaixaRepository movimentos,
    ConferenciaSaldoRepository conferencias,
    ContaCaixaRepository contas)
{
    public (long SaldoSistema, long Diferenca) Calcular(long contaId, DateOnly data, long saldoInformadoCentavos)
    {
        var saldoSistema = movimentos.SaldoConta(contaId, data);
        var diferenca = ResumoCaixaService.DiferencaConferencia(saldoInformadoCentavos, saldoSistema);
        return (saldoSistema, diferenca);
    }

    public long Salvar(long contaId, DateOnly data, long saldoInformadoCentavos, string? observacao)
    {
        var conta = contas.Obter(contaId)
            ?? throw new ArgumentException("Conta não encontrada.");

        var saldoSistema = movimentos.SaldoConta(contaId, data);
        var diferenca = saldoInformadoCentavos - saldoSistema;

        var id = conferencias.Inserir(contaId, data, saldoInformadoCentavos,
            saldoSistema, observacao?.Trim(), DateTimeOffset.UtcNow);

        if (diferenca != 0)
            Log.Warning("Conferência {Id}: diferença de {Diferenca} centavos na conta {Conta}",
                id, diferenca, conta.Nome);
        else
            Log.Information("Conferência {Id}: saldos conferem na conta {Conta}", id, conta.Nome);

        return id;
    }

    public IReadOnlyList<ConferenciaSaldo> Listar(long? contaId = null) =>
        conferencias.Listar(contaId);
}
