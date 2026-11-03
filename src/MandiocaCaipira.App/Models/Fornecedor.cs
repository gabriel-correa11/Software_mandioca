namespace MandiocaCaipira.App.Models;

public sealed record Fornecedor(
    long Id,
    string Nome,
    string? Apelido,
    string? Telefone,
    bool Ativo,
    DateTimeOffset CriadoEm);
