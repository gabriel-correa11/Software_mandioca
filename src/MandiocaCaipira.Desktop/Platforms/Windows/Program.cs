using Velopack;

namespace MandiocaCaipira.Desktop.WinUI;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
