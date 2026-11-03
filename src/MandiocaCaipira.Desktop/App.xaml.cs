namespace MandiocaCaipira.Desktop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage())
        {
            Title = "Mandioca Caipira",
            MinimumWidth = 1024,
            MinimumHeight = 680,
            Width = 1280,
            Height = 760,
        };

        window.Destroying += (_, _) => MauiProgram.Encerrar();

        return window;
    }
}
