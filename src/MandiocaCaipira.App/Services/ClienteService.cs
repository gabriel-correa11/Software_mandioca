using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;

namespace MandiocaCaipira.App.Services;

public sealed class ClienteService(ClienteRepository repository)
{
    public IReadOnlyList<Cliente> Listar(string? pesquisa = null, bool incluirInativos = false) =>
        repository.Listar(pesquisa, incluirInativos);

    public long Cadastrar(string nome)
    {
        nome = nome.Trim();
        if (nome.Length < 2)
            throw new ArgumentException("Informe um nome com pelo menos 2 caracteres.", nameof(nome));
        return repository.Inserir(nome, OrigemRegistro.Manual, DateTimeOffset.UtcNow);
    }

    public void DefinirAtivo(long clienteId, bool ativo) => repository.DefinirAtivo(clienteId, ativo);
}
