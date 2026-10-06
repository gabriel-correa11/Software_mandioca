using MandiocaCaipira.App.Data;

namespace MandiocaCaipira.App.Services;

public sealed class BackupService(InicializadorBanco inicializador)
{
    public string Criar() => inicializador.CriarBackupAgora();
}
