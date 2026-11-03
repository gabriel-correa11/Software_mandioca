using System.Globalization;
using System.Text.RegularExpressions;
using MandiocaCaipira.App.Data;
using Serilog;

namespace MandiocaCaipira.App.Services;

public sealed class BackupService
{
    private readonly InicializadorBanco _inicializador;
    private readonly string _diretorioBackups;
    private readonly string _caminhoBanco;
    private readonly ILogger _logger;

    private const int RetencaoAutomaticosDias = 30;
    private const int MinimoBackupsPreservados = 3;

    private static readonly Regex NomeBackup = new(
        @"^mandioca-(?<cat>auto|manual|migracao|pre-restauracao)-(?<date>\d{8})-\d{6}-\d{7}\.sqlite$",
        RegexOptions.Compiled);

    public BackupService(InicializadorBanco inicializador, string diretorioBackups,
        string caminhoBanco, ILogger? logger = null)
    {
        _inicializador = inicializador;
        _diretorioBackups = diretorioBackups;
        _caminhoBanco = caminhoBanco;
        _logger = logger ?? Log.Logger;
    }

    public string Criar() => _inicializador.CriarBackupAgora();

    public bool TentarBackupAutomatico()
    {
        try
        {
            if (JaPossuiBackupAutomaticoHoje())
            {
                _logger.Information("Backup automático do dia já existe; ignorando");
                return true;
            }

            _inicializador.CriarBackupAutomatico();
            AplicarRetencao();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Não foi possível criar o backup automático");
            return false;
        }
    }

    public IReadOnlyList<InfoBackup> Listar()
    {
        if (!Directory.Exists(_diretorioBackups))
            return [];

        return Directory.GetFiles(_diretorioBackups, "mandioca-*.sqlite")
            .Select(ParseBackup)
            .Where(b => b is not null)
            .OrderByDescending(b => b!.CriadoEmUtc)
            .ToList()!;
    }

    public ResultadoRestauracao Restaurar(string caminhoBackup)
    {
        var caminhoCompleto = Path.GetFullPath(caminhoBackup);
        var diretorioCompleto = Path.GetFullPath(_diretorioBackups) + Path.DirectorySeparatorChar;
        if (!caminhoCompleto.StartsWith(diretorioCompleto, StringComparison.OrdinalIgnoreCase))
            return ResultadoRestauracao.Falha("Caminho de backup inválido.");

        if (!File.Exists(caminhoBackup))
            return ResultadoRestauracao.Falha("O arquivo de backup não foi encontrado.");

        int versao;
        try
        {
            versao = InicializadorBanco.VersaoDoArquivo(caminhoBackup);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Backup selecionado para restauração não pôde ser lido");
            return ResultadoRestauracao.Falha(
                "O arquivo selecionado não é um banco de dados válido ou está corrompido.");
        }

        if (versao > InicializadorBanco.VersaoAtual)
            return ResultadoRestauracao.Falha(
                $"O backup é de uma versão mais recente do aplicativo (v{versao}) e não pode ser restaurado nesta versão (v{InicializadorBanco.VersaoAtual}).");

        if (versao < 1)
            return ResultadoRestauracao.Falha(
                "O backup não possui um schema válido do Mandioca Caipira.");

        try
        {
            InicializadorBanco.ValidarArquivoBackup(caminhoBackup);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Backup selecionado para restauração falhou na validação");
            return ResultadoRestauracao.Falha(
                "O backup selecionado está corrompido ou com dados inconsistentes e não pode ser restaurado.");
        }

        if (!InicializadorBanco.VerificarHmacBackup(caminhoBackup, _diretorioBackups))
        {
            _logger.Warning("Backup falhou na verificação HMAC: {Path}", caminhoBackup);
            return ResultadoRestauracao.Falha(
                "O arquivo de backup foi alterado ou corrompido. A restauração foi recusada por segurança.");
        }

        string? seguranca = null;
        if (File.Exists(_caminhoBanco))
        {
            try
            {
                seguranca = _inicializador.CriarBackupInterno("pre-restauracao",
                    "Backup de segurança criado antes da restauração");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Não foi possível criar backup de segurança antes da restauração");
                return ResultadoRestauracao.Falha(
                    "Não foi possível criar uma cópia de segurança do banco atual. A restauração foi cancelada para proteger seus dados.");
            }
        }

        BancoSqlite.LiberarConexoes(_caminhoBanco);

        var dir = Path.GetDirectoryName(_caminhoBanco)!;
        var temp = Path.Combine(dir, $"restore-{Guid.NewGuid():N}.sqlite");
        var antigo = _caminhoBanco + ".old";

        try
        {
            File.Copy(caminhoBackup, temp);

            if (File.Exists(antigo))
                File.Delete(antigo);

            if (File.Exists(_caminhoBanco))
                File.Move(_caminhoBanco, antigo);

            File.Move(temp, _caminhoBanco);

            try { if (File.Exists(antigo)) File.Delete(antigo); }
            catch { /* best effort */ }

            _logger.Information("Banco restaurado a partir do backup. Reinicialização necessária");
            return ResultadoRestauracao.Sucesso(seguranca);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Falha ao substituir o banco principal pelo backup");

            if (!File.Exists(_caminhoBanco) && File.Exists(antigo))
            {
                try { File.Move(antigo, _caminhoBanco); }
                catch (Exception rollbackEx)
                {
                    _logger.Error(rollbackEx, "Falha ao restaurar o banco original após erro");
                }
            }

            try { if (File.Exists(temp)) File.Delete(temp); }
            catch { /* best effort */ }

            return ResultadoRestauracao.Falha(
                "Não foi possível substituir o banco. Seus dados originais permanecem intactos"
                + (seguranca is not null ? $" e uma cópia de segurança foi preservada em:\n{seguranca}" : "."));
        }
    }

    internal void AplicarRetencao()
    {
        try
        {
            var autos = Directory.GetFiles(_diretorioBackups, "mandioca-auto-*.sqlite")
                .OrderByDescending(f => f)
                .ToList();

            if (autos.Count <= MinimoBackupsPreservados)
                return;

            var limite = DateTime.UtcNow.AddDays(-RetencaoAutomaticosDias);
            foreach (var arquivo in autos.Skip(MinimoBackupsPreservados))
            {
                var info = new FileInfo(arquivo);
                if (info.CreationTimeUtc < limite)
                {
                    info.Delete();
                    try { var hmac = new FileInfo(arquivo + ".hmac"); if (hmac.Exists) hmac.Delete(); }
                    catch { /* melhor esforço */ }
                    _logger.Information("Backup automático antigo removido: {FileName}", info.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Falha ao aplicar retenção de backups automáticos");
        }
    }

    private bool JaPossuiBackupAutomaticoHoje()
    {
        if (!Directory.Exists(_diretorioBackups))
            return false;

        var hoje = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return Directory.GetFiles(_diretorioBackups, $"mandioca-auto-{hoje}-*.sqlite").Length > 0;
    }

    private static InfoBackup? ParseBackup(string caminho)
    {
        var nome = Path.GetFileName(caminho);
        var match = NomeBackup.Match(nome);
        if (!match.Success)
            return null;

        var categoria = match.Groups["cat"].Value switch
        {
            "auto" => CategoriaBackup.Automatico,
            "manual" => CategoriaBackup.Manual,
            "migracao" => CategoriaBackup.PreMigracao,
            "pre-restauracao" => CategoriaBackup.PreRestauracao,
            _ => CategoriaBackup.Manual
        };

        var dateStr = match.Groups["date"].Value;
        DateOnly.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var data);

        var info = new FileInfo(caminho);
        return new InfoBackup(caminho, nome, data, categoria,
            info.Exists ? info.Length : 0, info.Exists ? info.CreationTimeUtc : default);
    }
}

public enum CategoriaBackup { Automatico, Manual, PreMigracao, PreRestauracao }

public sealed record InfoBackup(
    string Caminho,
    string NomeArquivo,
    DateOnly Data,
    CategoriaBackup Categoria,
    long TamanhoBytes,
    DateTime CriadoEmUtc)
{
    public string CategoriaTexto => Categoria switch
    {
        CategoriaBackup.Automatico => "Automático",
        CategoriaBackup.Manual => "Manual",
        CategoriaBackup.PreMigracao => "Pré-migração",
        CategoriaBackup.PreRestauracao => "Pré-restauração",
        _ => "Desconhecido"
    };
}

public sealed record ResultadoRestauracao(bool Ok, string? Mensagem, string? BackupSeguranca)
{
    public static ResultadoRestauracao Sucesso(string? backupSeguranca) =>
        new(true, null, backupSeguranca);

    public static ResultadoRestauracao Falha(string mensagem) =>
        new(false, mensagem, null);
}
