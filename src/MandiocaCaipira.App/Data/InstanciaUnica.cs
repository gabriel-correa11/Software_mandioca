namespace MandiocaCaipira.App.Data;

public sealed class InstanciaUnica : IDisposable
{
    private const string MutexName = "MandiocaCaipira_SingleInstance_F7A3B2E1";
    private readonly Mutex _mutex;
    private readonly bool _acquired;

    public bool IsOwner => _acquired;
    private bool _released;

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

    public void Liberar()
    {
        if (_acquired && !_released)
        {
            _released = true;
            try { _mutex.ReleaseMutex(); }
            catch (ObjectDisposedException) { }
        }
    }

    public void Dispose()
    {
        Liberar();
        _mutex.Dispose();
    }
}
