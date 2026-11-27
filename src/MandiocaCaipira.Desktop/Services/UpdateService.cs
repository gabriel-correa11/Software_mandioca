using Serilog;
using Velopack;
using Velopack.Sources;

namespace MandiocaCaipira.Desktop.Services;

public sealed class UpdateService
{
    private readonly GithubSource _source;

    public UpdateService(string repoUrl)
    {
        _source = new GithubSource(repoUrl, accessToken: null, prerelease: false);
    }

    public bool UpdateAvailable { get; private set; }
    public string? NewVersion { get; private set; }
    public bool IsInstalled => new UpdateManager(_source).IsInstalled;

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (!IsInstalled)
        {
            Log.Warning("Velopack: app nao foi instalado pelo Setup, atualizacao automatica indisponivel");
            throw new InvalidOperationException(
                "O programa não foi instalado pelo instalador oficial. " +
                "Baixe o instalador (MandiocaCaipira-win-Setup.exe) e instale novamente para receber atualizações automáticas.");
        }

        var mgr = new UpdateManager(_source);
        var info = await mgr.CheckForUpdatesAsync();
        if (info is null)
        {
            UpdateAvailable = false;
            NewVersion = null;
            return false;
        }

        UpdateAvailable = true;
        NewVersion = info.TargetFullRelease.Version.ToString();
        return true;
    }

    public async Task ApplyUpdateAsync()
    {
        var mgr = new UpdateManager(_source);
        var info = await mgr.CheckForUpdatesAsync();
        if (info is null) return;

        await mgr.DownloadUpdatesAsync(info);
        mgr.ApplyUpdatesAndRestart(info);
    }
}
