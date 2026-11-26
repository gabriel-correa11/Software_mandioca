namespace MandiocaCaipira.App.Models;

public sealed record CategoriaMovimento(
    long Id, string Nome, TipoMovimento Tipo,
    NaturezaCategoria Natureza, bool Ativa);

public enum TipoMovimento { Entrada, Saida }

public enum NaturezaCategoria { Operacional, NaoOperacional }
