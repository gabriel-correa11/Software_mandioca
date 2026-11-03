using Velopack;
using Velopack.Sources;

namespace MandiocaCaipira.Desktop.Services;

public sealed class UpdateService
{
    private readonly string _updateUrl;

    public UpdateService(string updateUrl)
    {
        _updateUrl = updateUrl;
    }

    public bool UpdateAvailable { get; private set; }
    public string? NewVersion { get; private set; }

    public async Task<bool> CheckForUpdatesAsync()
    {
        var mgr = new UpdateManager(new SimpleFileSource(new DirectoryInfo(_updateUrl)));
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
        var mgr = new UpdateManager(new SimpleFileSource(new DirectoryInfo(_updateUrl)));
        var info = await mgr.CheckForUpdatesAsync();
        if (info is null) return;

        await mgr.DownloadUpdatesAsync(info);
        mgr.ApplyUpdatesAndRestart(info);
    }
}
