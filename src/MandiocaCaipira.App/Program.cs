namespace MandiocaCaipira.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var banco = new Data.BancoSqlite(Data.CaminhosDados.Banco);
            new Data.InicializadorBanco(banco, Data.CaminhosDados.Backups).Inicializar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível abrir o banco de dados: {ex.Message}",
                "Mandioca Caipira",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        Application.Run(new Form1());
    }
}
