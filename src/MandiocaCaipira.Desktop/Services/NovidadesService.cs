using MandiocaCaipira.App.Data;

namespace MandiocaCaipira.Desktop.Services;

public sealed class NovidadesService
{
    private static readonly Dictionary<string, (string Titulo, string Descricao)[]> Changelog = new()
    {
        ["1.3.0"] =
        [
            ("Reportar problema", "Envie um relatório de problema com imagens direto pelo programa."),
            ("Tela reorganizada", "Menus e páginas organizados por área: Vendas, Caixa, Compras e Sistema."),
            ("Modo escuro", "Alterne entre claro e escuro pelo ícone no rodapé do menu."),
            ("Visual renovado", "Tela inicial com resumo do dia e lista de vendas.")
        ],
        ["1.2.0"] =
        [
            ("Fluxo de caixa", "Controle entradas e saídas da empresa com categorias e contas separadas."),
            ("Conferência de saldo", "Compare o saldo do sistema com o app do banco e encontre diferenças."),
            ("Total em pendências", "Veja o total em aberto direto no rodapé da tabela."),
            ("Atualização automática", "O programa se atualiza sozinho ao abrir.")
        ]
    };

    private readonly string _versionFile =
        Path.Combine(CaminhosDados.DiretorioBase, "lastversion.txt");

    public string VersaoAtual { get; }

    public NovidadesService(string versao) => VersaoAtual = versao;

    public bool PrimeiraVez => !File.Exists(_versionFile);

    public (string Versao, (string Titulo, string Descricao)[] Itens)? Verificar()
    {
        var anterior = LerVersaoAnterior();
        if (VersaoAtual == anterior) return null;

        return Changelog.TryGetValue(VersaoAtual, out var itens)
            ? (VersaoAtual, itens)
            : null;
    }

    public void MarcarComoVisto()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_versionFile)!);
        File.WriteAllText(_versionFile, VersaoAtual);
    }

    private string? LerVersaoAnterior()
    {
        try { return File.Exists(_versionFile) ? File.ReadAllText(_versionFile).Trim() : null; }
        catch { return null; }
    }
}
