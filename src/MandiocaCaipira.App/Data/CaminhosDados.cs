namespace MandiocaCaipira.App.Data;

public static class CaminhosDados
{
    public static string DiretorioBase => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MandiocaCaipira");

    public static string Banco => Path.Combine(DiretorioBase, "Data", "mandioca.sqlite");
    public static string Backups => Path.Combine(DiretorioBase, "Backups");
    public static string Logs => Path.Combine(DiretorioBase, "Logs");
    public static string Recibos => Path.Combine(DiretorioBase, "Recibos");
}
