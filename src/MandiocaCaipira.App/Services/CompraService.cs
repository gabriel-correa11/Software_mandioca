using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using Serilog;

namespace MandiocaCaipira.App.Services;

public sealed class CompraService(
    BancoSqlite banco,
    CompraRepository compras,
    FornecedorRepository fornecedores,
    ConfiguracaoRendimentoRepository configuracoes)
{
    public long Registrar(
        long fornecedorId, DateOnly dataCompra, long pesoBrutoGramas,
        long pesoCascadoGramas, long? custoCentavos, string? caminhoFoto, string? observacao)
    {
        if (fornecedorId <= 0)
            throw new ArgumentException("Selecione um fornecedor.");
        if (pesoBrutoGramas <= 0)
            throw new ArgumentException("Peso bruto deve ser maior que zero.");
        if (pesoCascadoGramas <= 0)
            throw new ArgumentException("Peso cascado deve ser maior que zero.");
        if (pesoCascadoGramas > pesoBrutoGramas)
            throw new ArgumentException("Peso cascado não pode ser maior que o bruto.");
        if (dataCompra > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Data da compra não pode ser futura.");
        if (custoCentavos.HasValue && custoCentavos.Value < 0)
            throw new ArgumentException("Custo total não pode ser negativo.");
        if (observacao is not null && observacao.Trim().Length > 500)
            throw new ArgumentException("Observação muito longa (máximo 500 caracteres).");

        var fornecedor = fornecedores.Obter(fornecedorId)
            ?? throw new ArgumentException("Fornecedor não encontrado.");
        if (!fornecedor.Ativo)
            throw new ArgumentException("Fornecedor está inativo.");

        var config = configuracoes.ObterVigente(dataCompra);

        using var connection = banco.AbrirConexao();
        using var transaction = connection.BeginTransaction();
        var agora = DateTimeOffset.UtcNow;
        try
        {
            var id = compras.Inserir(connection, transaction,
                fornecedorId, dataCompra, pesoBrutoGramas, pesoCascadoGramas,
                custoCentavos, caminhoFoto, observacao?.Trim(),
                config.PercentualPadraoPerdaBp, config.GramasPorSacoPadrao,
                config.TamanhoSacoGramas, agora);
            transaction.Commit();

            Log.Information("Compra {CompraId} registrada: fornecedor={Fornecedor}, bruto={Bruto}g, cascado={Cascado}g",
                id, fornecedor.Nome, pesoBrutoGramas, pesoCascadoGramas);

            var rendimento = RendimentoService.Calcular(pesoBrutoGramas, pesoCascadoGramas,
                config.PercentualPadraoPerdaBp, config.GramasPorSacoPadrao, config.TamanhoSacoGramas);
            if (rendimento.PerdaAcimaDoPadrao)
                Log.Warning("Compra {CompraId}: perda acima do padrão ({Perda:P2})", id, rendimento.PerdaPercentual);

            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<CompraResumo> Listar(
        long? fornecedorId = null, DateOnly? inicio = null, DateOnly? fim = null) =>
        compras.Listar(fornecedorId, inicio, fim);

    public void Cancelar(long compraId, string motivo)
    {
        motivo = motivo.Trim();
        if (motivo.Length < 3)
            throw new ArgumentException("Informe o motivo do cancelamento.");
        if (motivo.Length > 500)
            throw new ArgumentException("Motivo muito longo (máximo 500 caracteres).");
        compras.Cancelar(compraId, motivo, DateTimeOffset.UtcNow);
        Log.Information("Compra {CompraId} cancelada: {Motivo}", compraId, motivo);
    }

    public void MarcarPaga(long compraId) =>
        compras.MarcarPaga(compraId);
}
