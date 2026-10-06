namespace MandiocaCaipira.App;

internal sealed class TextPrompt : Form
{
    private readonly TextBox _value = new() { Dock = DockStyle.Top, Height = 30 };
    private TextPrompt(string title, string label, string help)
    {
        Text = title; Width = 500; Height = 235; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; Font = Ui.Font; BackColor = Color.White; Padding = new Padding(20);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
        var ok = Ui.PrimaryButton("Confirmar", () => { if (string.IsNullOrWhiteSpace(_value.Text)) { MessageBox.Show(this, "Preencha o campo antes de continuar.", title); return; } DialogResult = DialogResult.OK; });
        var cancel = Ui.SecondaryButton("Cancelar", () => DialogResult = DialogResult.Cancel); buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        Controls.Add(_value); Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI Semibold", 10) }); Controls.Add(new Label { Text = help, Dock = DockStyle.Top, Height = 48, ForeColor = Ui.Muted }); Controls.Add(buttons); AcceptButton = ok; CancelButton = cancel;
    }
    public static string? Show(IWin32Window owner, string title, string label, string help) { using var dialog = new TextPrompt(title, label, help); return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._value.Text.Trim() : null; }
}
