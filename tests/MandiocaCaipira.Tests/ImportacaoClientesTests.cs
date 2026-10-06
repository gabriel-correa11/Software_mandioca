using ClosedXML.Excel;
using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using Microsoft.Data.Sqlite;

namespace MandiocaCaipira.Tests;

public sealed class ImportacaoClientesTests
{
    [Theory]
    [InlineData("joaozinho", "Joaozinho")]
    [InlineData("maria silva", "Maria Silva")]
    [InlineData("MARIA SILVA", "Maria Silva")]
    [InlineData("  maria   da   silva  ", "Maria da Silva")]
    [InlineData("JOÃO DOS SANTOS", "João dos Santos")]
    [InlineData("josé de souza", "José de Souza")]
    [InlineData("ana-maria d'ávila", "Ana-Maria D'ávila")]
    [InlineData("de souza", "De Souza")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizacaoDeApresentacaoCapitalizaSemRemoverCaracteres(string entrada, string esperado)
        => Assert.Equal(esperado, NormalizadorNomeCliente.NormalizarParaApresentacao(entrada));

    [Theory]
    [InlineData("  João   da Silva  ", "JOAO DA SILVA")]
    [InlineData("Ana-Maria", "ANA MARIA")]
    [InlineData("D'Ávila", "D AVILA")]
    public void NormalizacaoConservadoraIgnoraAcentosCaixaEEspacos(string entrada, string esperado)
        => Assert.Equal(esperado, NormalizadorNomeCliente.NormalizarParaComparacao(entrada));

    [Fact]
    public async Task PreviewAgrupaRepeticoesReconheceExatoENaoAlteraBanco()
    {
        using var env = new ImportEnvironment();
        env.Repository.Inserir("João Silva", OrigemRegistro.Manual, DateTimeOffset.UtcNow);
        var file = env.WriteText("nomes.txt", "JOÃO SILVA\nJoao  Silva\nMaria Souza\n");

        var preview = await env.Service.AnalisarAsync(file);

        Assert.Equal(2, preview.Count);
        var joao = Assert.Single(preview, c => c.NomeNormalizado == "JOAO SILVA");
        Assert.Equal(2, joao.Ocorrencias);
        Assert.Equal(AcaoImportacaoCliente.UsarExistente, joao.Acao);
        Assert.Equal("João Silva", joao.ClienteCorrespondenteNome);
        Assert.Equal(1, env.CountClients());
    }

    [Fact]
    public async Task FuzzyApenasSugereEExigeDecisaoHumana()
    {
        using var env = new ImportEnvironment();
        env.Repository.Inserir("Mariana Souza", OrigemRegistro.Manual, DateTimeOffset.UtcNow);
        var preview = await env.Service.AnalisarAsync(env.WriteText("clientes.txt", "Mariana Souzza"));
        var candidate = Assert.Single(preview);

        Assert.Equal(AcaoImportacaoCliente.Revisar, candidate.Acao);
        Assert.Equal("Mariana Souza", candidate.ClienteCorrespondenteNome);
        Assert.Throws<InvalidOperationException>(() => env.Service.Confirmar(preview));
        Assert.Equal(1, env.CountClients());
    }

    [Fact]
    public async Task ConfirmacaoCriaSomenteNovosComOrigemImportacao()
    {
        using var env = new ImportEnvironment();
        env.Repository.Inserir("Cliente Existente", OrigemRegistro.Manual, DateTimeOffset.UtcNow);
        var preview = await env.Service.AnalisarAsync(env.WriteText("clientes.txt", "Cliente Existente\nPessoa Nova\nIgnorar Depois"));
        preview.Single(c => c.NomeParaCadastro == "Ignorar Depois").Acao = AcaoImportacaoCliente.Ignorar;

        var result = env.Service.Confirmar(preview);

        Assert.Equal(1, result.Criados);
        Assert.Equal(1, result.Associados);
        Assert.Equal(1, result.Ignorados);
        var imported = Assert.Single(env.Repository.Listar(null, true), c => c.Nome == "Pessoa Nova");
        Assert.Equal(OrigemRegistro.Importacao, imported.Origem);
    }

    [Fact]
    public async Task CadastroManualEImportacaoUsamMesmaNormalizacaoDeApresentacao()
    {
        using var env = new ImportEnvironment();
        var clientes = new ClienteService(env.Repository);
        clientes.Cadastrar("  maria   da   silva ");
        var preview = await env.Service.AnalisarAsync(env.WriteText("clientes.txt", "JOSÉ DOS SANTOS"));
        env.Service.Confirmar(preview);

        var names = env.Repository.Listar(null, true).Select(c => c.Nome).ToList();
        Assert.Contains("Maria da Silva", names);
        Assert.Contains("José dos Santos", names);
    }

    [Fact]
    public void InsercaoEmLoteFazRollbackCompletoQuandoUmaLinhaFalha()
    {
        using var env = new ImportEnvironment();
        Assert.Throws<SqliteException>(() => env.Repository.InserirImportadosAtomico(["Pessoa Válida", "  "], DateTimeOffset.UtcNow));
        Assert.Equal(0, env.CountClients());
    }

    [Fact]
    public async Task LeitorCsvUsaColunaNomeERespeitaCamposEntreAspas()
    {
        using var env = new ImportEnvironment();
        var file = env.WriteText("clientes.csv", "Codigo;Nome;Cidade\n1;\"Pessoa, Um\";Teste\n2;Pessoa Dois;Teste\n");
        var names = await new LeitorArquivoClientes().LerAsync(file);
        Assert.Equal(["Pessoa, Um", "Pessoa Dois"], names);
    }

    [Fact]
    public async Task LeitorXlsxUsaCabecalhoNomeEmPlanilhaFicticia()
    {
        using var env = new ImportEnvironment();
        var file = Path.Combine(env.Directory, "clientes.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Clientes");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "Nome do cliente";
            sheet.Cell(2, 1).Value = 1;
            sheet.Cell(2, 2).Value = "Pessoa Planilha";
            workbook.SaveAs(file);
        }
        var names = await new LeitorArquivoClientes().LerAsync(file);
        Assert.Equal(["Pessoa Planilha"], names);
    }

    private sealed class ImportEnvironment : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "MandiocaCaipiraTests", Guid.NewGuid().ToString("N"));
        public ClienteRepository Repository { get; }
        public ImportacaoClientesService Service { get; }
        private readonly BancoSqlite _database;

        public ImportEnvironment()
        {
            System.IO.Directory.CreateDirectory(Directory);
            _database = new BancoSqlite(Path.Combine(Directory, "test.sqlite"));
            new InicializadorBanco(_database, Path.Combine(Directory, "backups")).Inicializar();
            Repository = new ClienteRepository(_database);
            Service = new ImportacaoClientesService(new LeitorArquivoClientes(), Repository);
        }

        public string WriteText(string name, string content)
        {
            var path = Path.Combine(Directory, name);
            File.WriteAllText(path, content);
            return path;
        }

        public long CountClients()
        {
            using var connection = _database.AbrirConexao();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Cliente;";
            return (long)command.ExecuteScalar()!;
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
