using Serilog;
using Serilog.Events;

namespace MandiocaCaipira.App.Diagnostics;

public static class ConfiguracaoLogging
{
    private const int DiasDeRetencao = 14;
    private const long LimitePorArquivoEmBytes = 10 * 1024 * 1024;

    public static ILogger Criar(string diretorioLogs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diretorioLogs);
        Directory.CreateDirectory(diretorioLogs);

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(diretorioLogs, "mandioca-.log"),
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: DiasDeRetencao,
                fileSizeLimitBytes: LimitePorArquivoEmBytes,
                rollOnFileSizeLimit: true,
                shared: false)
            .CreateLogger();
    }
}
