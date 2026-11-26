namespace MandiocaCaipira.App.Models;

public sealed record MovimentoCaixa(
    long Id,
    TipoMovimento Tipo,
    DateOnly DataMovimento,
    DateTimeOffset DataRegistro,
    long ContaId,
    long CategoriaId,
    long ValorCentavos,
    string Descricao,
    string? Beneficiario,
    OrigemMovimento OrigemTipo,
    long? OrigemId,
    StatusMovimento Status,
    DateTimeOffset? CanceladoEm,
    string? MotivoCancelamento);

public sealed record MovimentoCaixaResumo(
    long Id,
    TipoMovimento Tipo,
    DateOnly DataMovimento,
    string Conta,
    string Categoria,
    NaturezaCategoria Natureza,
    long ValorCentavos,
    string Descricao,
    string? Beneficiario,
    OrigemMovimento OrigemTipo,
    StatusMovimento Status);

public enum OrigemMovimento { Manual, PagamentoCliente, CompraMandioca }

public enum StatusMovimento { Ativo, Cancelado }
