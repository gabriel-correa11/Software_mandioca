using System.Globalization;
using Krypton.Toolkit;
using MandiocaCaipira.App.Services;

namespace MandiocaCaipira.App;

internal sealed class RestaurarBackupForm : KryptonForm
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly IReadOnlyList<InfoBackup> _backups;
    private readonly DataGridView _grid;

    public InfoBackup? BackupSelecionado { get; private set; }

    public RestaurarBackupForm(IReadOnlyList<InfoBackup> backups)
    {
        _backups = backups;
        Text = "Restaurar backup";
        Size = new Size(700, 440);
        MinimumSize = new Size(560, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = Ui.Font;
        PaletteMode = PaletteMode.ProfessionalSystem;

        var label = new Label
        {
            Text = "Selecione o backup que deseja restaurar. Backups manuais e pré-migração nunca são removidos automaticamente.",
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8, 8, 8, 4),
            ForeColor = Ui.Muted
        };

        _grid = Ui.Grid();
        _grid.Dock = DockStyle.Fill;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        Ui.Columns(_grid,
            ("Data", "Data", 20, DataGridViewContentAlignment.MiddleCenter),
            ("Categoria", "Tipo", 18, DataGridViewContentAlignment.MiddleCenter),
            ("Tamanho", "Tamanho", 18, DataGridViewContentAlignment.MiddleRight),
            ("NomeArquivo", "Arquivo", 44, DataGridViewContentAlignment.MiddleLeft));

        _grid.DataSource = backups.Select(b => new BackupRow(
            b.Data.ToString("dd/MM/yyyy", PtBr),
            b.CategoriaTexto,
            FormatSize(b.TamanhoBytes),
            b.NomeArquivo)).ToList();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 6, 8, 6)
        };

        var cancel = Ui.SecondaryButton("Cancelar", () => { DialogResult = DialogResult.Cancel; Close(); });
        var restore = Ui.PrimaryButton("Restaurar selecionado", () =>
        {
            if (_grid.CurrentRow?.Index is int idx && idx >= 0 && idx < _backups.Count)
            {
                BackupSelecionado = _backups[idx];
                DialogResult = DialogResult.OK;
                Close();
            }
        });

        buttons.Controls.AddRange([cancel, restore]);
        Controls.Add(_grid);
        Controls.Add(label);
        Controls.Add(buttons);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    private sealed record BackupRow(string Data, string Categoria, string Tamanho, string NomeArquivo);
}
