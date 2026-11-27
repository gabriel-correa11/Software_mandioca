using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MandiocaCaipira.App.Data;
using Serilog;

namespace MandiocaCaipira.Desktop.Services;

public sealed class BugReportService
{
    private const string Owner = "gabriel-correa11";
    private const string Repo = "Software_mandioca";
    private const string ApiBase = "https://api.github.com";

    private readonly string _versao;
    private readonly string _tokenPath;

    public BugReportService(string versao)
    {
        _versao = versao;
        _tokenPath = Path.Combine(CaminhosDados.DiretorioBase, "github-token.txt");
    }

    public bool TokenConfigurado
    {
        get
        {
            try { return File.Exists(_tokenPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(_tokenPath)); }
            catch { return false; }
        }
    }

    public async Task EnviarAsync(string descricao, IReadOnlyList<string> caminhoImagens)
    {
        var token = File.ReadAllText(_tokenPath).Trim();
        using var http = CriarClient(token);

        var imageUrls = await UploadImagens(http, caminhoImagens);
        var body = MontarCorpo(descricao, imageUrls);
        var titulo = descricao.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Bug report";
        if (titulo.Length > 80) titulo = titulo[..77] + "...";

        var payload = new { title = $"[Bug] {titulo}", body, labels = new[] { "bug-report" } };
        var resp = await http.PostAsJsonAsync($"{ApiBase}/repos/{Owner}/{Repo}/issues", payload);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            Log.Error("Falha ao criar issue: {Status} {Body}", resp.StatusCode, err);
            throw new HttpRequestException($"Erro ao enviar: {resp.StatusCode}");
        }
    }

    private async Task<List<string>> UploadImagens(HttpClient http, IReadOnlyList<string> caminhos)
    {
        var urls = new List<string>();
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        for (var i = 0; i < caminhos.Count; i++)
        {
            try
            {
                var ext = Path.GetExtension(caminhos[i]).ToLowerInvariant();
                var fileName = $"bug-reports/{timestamp}-{i}{ext}";
                var base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(caminhos[i]));

                var payload = new { message = $"bug-report: imagem {timestamp}-{i}", content = base64 };
                var resp = await http.PutAsJsonAsync($"{ApiBase}/repos/{Owner}/{Repo}/contents/{fileName}", payload);

                if (resp.IsSuccessStatusCode)
                    urls.Add($"https://raw.githubusercontent.com/{Owner}/{Repo}/master/{fileName}");
                else
                    Log.Warning("Falha ao enviar imagem {Index}: {Status}", i, resp.StatusCode);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erro ao processar imagem {Index}", i);
            }
        }

        return urls;
    }

    private string MontarCorpo(string descricao, List<string> imageUrls)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Descrição do problema");
        sb.AppendLine(descricao);
        sb.AppendLine();

        if (imageUrls.Count > 0)
        {
            sb.AppendLine("## Imagens");
            foreach (var url in imageUrls)
                sb.AppendLine($"![screenshot]({url})");
            sb.AppendLine();
        }

        sb.AppendLine("## Informações do sistema");
        sb.AppendLine($"- **Versão:** {_versao}");
        sb.AppendLine($"- **Windows:** {Environment.OSVersion.VersionString}");
        sb.AppendLine($"- **Data:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"- **Máquina:** {Environment.MachineName}");

        var logRecente = LerUltimasLinhasLog();
        if (logRecente is not null)
        {
            sb.AppendLine();
            sb.AppendLine("<details><summary>Últimas linhas do log</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(logRecente);
            sb.AppendLine("```");
            sb.AppendLine("</details>");
        }

        return sb.ToString();
    }

    private static string? LerUltimasLinhasLog()
    {
        try
        {
            if (!Directory.Exists(CaminhosDados.Logs)) return null;

            var logFile = Directory.GetFiles(CaminhosDados.Logs, "*.txt")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (logFile is null) return null;

            using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);

            return string.Join('\n', lines.TakeLast(30));
        }
        catch
        {
            return null;
        }
    }

    private static HttpClient CriarClient(string token)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MandiocaCaipira-BugReport");
        return http;
    }
}
