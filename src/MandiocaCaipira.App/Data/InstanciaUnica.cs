namespace MandiocaCaipira.App.Data;

public sealed class InstanciaUnica : IDisposable
{
    private const string MutexName = "MandiocaCaipira_SingleInstance_F7A3B2E1";
    private readonly Mutex _mutex;
    private readonly bool _acquired;

    public bool IsOwner => _acquired;

    public InstanciaUnica()
    {
        _mutex = new Mutex(initiallyOwned: false, MutexName, out _);
        try
        {
            _acquired = _mutex.WaitOne(0, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            _acquired = true;
        }
    }

    public void Dispose()
    {
        if (_acquired)
        {
            try { _mutex.ReleaseMutex(); }
            catch (ObjectDisposedException) { }
        }
        _mutex.Dispose();
    }
}
