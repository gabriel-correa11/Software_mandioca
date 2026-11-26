using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using Microsoft.Data.Sqlite;
using Serilog;

namespace MandiocaCaipira.App.Services;

public sealed class MovimentoCaixaService(
    BancoSqlite banco,
    MovimentoCaixaRepository movimentos,
    ContaCaixaRepository contas,
    CategoriaMovimentoRepository categorias)
{
    private static readonly DateOnly DataMinima = new(2023, 11, 1);

    public long Registrar(
        TipoMovimento tipo, DateOnly dataMovimento, long contaId, long categoriaId,
        long valorCentavos, string descricao, string? beneficiario)
    {
        Validar(tipo, dataMovimento, contaId, categoriaId, valorCentavos, descricao);

        using var connection = banco.AbrirConexao();
        using var transaction = connection.BeginTransaction();
        try
        {
            var id = movimentos.Inserir(connection, transaction, tipo, dataMovimento,
                contaId, categoriaId, valorCentavos, descricao.Trim(), beneficiario?.Trim(),
                OrigemMovimento.Manual, null, DateTimeOffset.UtcNow);
            transaction.Commit();

            Log.Information("Movimento {Id} registrado: {Tipo}, categoria={Cat}, valor={Valor}",
                id, tipo, categoriaId, valorCentavos);
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public long RegistrarAutomatico(
        SqliteConnection connection, SqliteTransaction transaction,
        TipoMovimento tipo, DateOnly dataMovimento, long contaId, long categoriaId,
        long valorCentavos, string descricao, string? beneficiario,
        OrigemMovimento origemTipo, long origemId)
    {
        if (movimentos.ExisteMovimentoOrigem(origemTipo, origemId))
            return -1;

        var id = movimentos.Inserir(connection, transaction, tipo, dataMovimento,
            contaId, categoriaId, valorCentavos, descricao, beneficiario,
            origemTipo, origemId, DateTimeOffset.UtcNow);

        Log.Information("Movimento automático {Id}: {Tipo}, origem={Origem}:{OrigemId}, valor={Valor}",
            id, tipo, origemTipo, origemId, valorCentavos);
        return id;
    }

    public void Cancelar(long movimentoId, string motivo)
    {
        motivo = motivo.Trim();
        if (motivo.Length < 3)
            throw new ArgumentException("Informe o motivo do cancelamento.");
        if (motivo.Length > 500)
            throw new ArgumentException("Motivo muito longo (máximo 500 caracteres).");

        movimentos.Cancelar(movimentoId, motivo, DateTimeOffset.UtcNow);
        Log.Information("Movimento {Id} cancelado: {Motivo}", movimentoId, motivo);
    }

    public IReadOnlyList<MovimentoCaixaResumo> Listar(
        DateOnly? inicio = null, DateOnly? fim = null,
        long? contaId = null, TipoMovimento? tipo = null, long? categoriaId = null) =>
        movimentos.Listar(inicio, fim, contaId, tipo, categoriaId);

    public long SaldoConta(long contaId, DateOnly? dataAte = null) =>
        movimentos.SaldoConta(contaId, dataAte);

    public long SaldoTotal(DateOnly? dataAte = null) =>
        movimentos.SaldoTotal(dataAte);

    public IReadOnlyList<(string Conta, long Saldo)> SaldoPorConta()
    {
        var lista = contas.Listar();
        return lista.Select(c => (c.Nome, movimentos.SaldoConta(c.Id))).ToList();
    }

    private void Validar(
        TipoMovimento tipo, DateOnly dataMovimento, long contaId, long categoriaId,
        long valorCentavos, string descricao)
    {
        if (valorCentavos <= 0)
            throw new ArgumentException("Informe um valor maior que zero.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descreva o movimento.");
        if (descricao.Trim().Length > 500)
            throw new ArgumentException("Descrição muito longa (máximo 500 caracteres).");
        if (dataMovimento > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Data do movimento não pode ser futura.");
        if (dataMovimento < DataMinima)
            throw new ArgumentException("Data do movimento não pode ser anterior a 01/11/2023.");

        var conta = contas.Obter(contaId)
            ?? throw new ArgumentException("Conta não encontrada.");
        if (!conta.Ativa)
            throw new ArgumentException("Conta inativa.");

        var categoria = categorias.Obter(categoriaId)
            ?? throw new ArgumentException("Categoria não encontrada.");
        if (!categoria.Ativa)
            throw new ArgumentException("Categoria inativa.");
        if (categoria.Tipo != tipo)
            throw new ArgumentException("Tipo do movimento incompatível com a categoria.");
    }
}
