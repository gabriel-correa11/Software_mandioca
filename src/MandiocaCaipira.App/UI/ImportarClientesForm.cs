using System.ComponentModel;
using Krypton.Toolkit;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Services;
using Serilog;

namespace MandiocaCaipira.App;

public sealed class ImportarClientesForm : KryptonForm
{
    private readonly ImportacaoClientesService _service;
    private readonly IReadOnlyList<Cliente> _clientes;
    private readonly KryptonDataGridView _grid = new();
    private readonly KryptonLabel _summary = new() { Dock = DockStyle.Fill };
    private readonly KryptonButton _confirm = new() { Text = "Confirmar importação", Width = 165, Enabled = false };
    private readonly KryptonButton _cancel = new() { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
    private BindingList<CandidatoImportacaoCliente> _candidates = [];
    private BindingSource _source = new();

    public ResultadoImportacaoClientes? Resultado { get; private set; }

    public ImportarClientesForm(ImportacaoClientesService service, IReadOnlyList<Cliente> clientes)
    {
        _service = service;
        _clientes = clientes;
        Text = "Importar clientes — revisão";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 600);
        Size = new Size(1180, 700);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        PaletteMode = PaletteMode.ProfessionalSystem;
        KeyPreview = true;
        AcceptButton = _confirm;
        CancelButton = _cancel;
        Controls.Add(BuildLayout());
        ConfigureGrid();
        _confirm.Click += (_, _) => ConfirmImport();
        _grid.CellEndEdit += GridCellEndEdit;
        _grid.CellValueChanged += (_, _) => UpdateSummary();
        _grid.DataError += (_, args) => args.ThrowException = false;
        Shown += (_, _) => _grid.Focus();
    }

    public async Task LoadFileAsync(string path)
    {
        UseWaitCursor = true;
        _confirm.Enabled = false;
        try
        {
            var candidates = await _service.AnalisarAsync(path);
            if (candidates.Count == 0)
                throw new InvalidDataException("Nenhum nome de cliente reconhecível foi encontrado no arquivo.");
            _candidates = new BindingList<CandidatoImportacaoCliente>(candidates.ToList());
            _source = new BindingSource { DataSource = _candidates };
            _grid.DataSource = _source;
            UpdateSummary();
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private Control BuildLayout()
    {
        var panel = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        panel.StateCommon.Color1 = SystemColors.Control;
        var heading = new KryptonPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(8, 6, 8, 5) };
        heading.StateCommon.Color1 = SystemColors.Control;
        heading.Controls.Add(new KryptonLabel
        {
            Text = "Revisar clientes encontrados",
            Dock = DockStyle.Top,
            StateCommon = { ShortText = { Font = new Font("Segoe UI Semibold", 14F), Color1 = Color.FromArgb(43, 66, 50) } }
        });
        heading.Controls.Add(new KryptonLabel
        {
            Text = "Confira os nomes e escolha uma ação para cada linha. Nada será gravado antes da confirmação.",
            Dock = DockStyle.Bottom
        });

        var footer = new KryptonPanel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(8, 9, 8, 5) };
        footer.StateCommon.Color1 = SystemColors.Control;
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 285, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        buttons.Controls.Add(_confirm);
        buttons.Controls.Add(_cancel);
        footer.Controls.Add(buttons);
        footer.Controls.Add(_summary);

        panel.Controls.Add(_grid);
        panel.Controls.Add(footer);
        panel.Controls.Add(heading);
        return panel;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 27;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CandidatoImportacaoCliente.NomeEncontrado), HeaderText = "Encontrado no arquivo", ReadOnly = true, FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CandidatoImportacaoCliente.Ocorrencias), HeaderText = "Repetições", ReadOnly = true, Width = 78, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CandidatoImportacaoCliente.NomeParaCadastro), HeaderText = "Nome para cadastro", FillWeight = 24 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CandidatoImportacaoCliente.Situacao), HeaderText = "Situação", ReadOnly = true, FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(CandidatoImportacaoCliente.ClienteCorrespondenteId),
            HeaderText = "Cliente correspondente",
            DisplayMember = nameof(ClienteOption.Name),
            ValueMember = nameof(ClienteOption.Id),
            DataSource = new[] { new ClienteOption(null, "—") }.Concat(_clientes.Select(c => new ClienteOption(c.Id, c.Ativo ? c.Nome : $"{c.Nome} (inativo)"))).ToList(),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FillWeight = 23
        });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(CandidatoImportacaoCliente.Acao),
            HeaderText = "Ação",
            DisplayMember = nameof(ActionOption.Name),
            ValueMember = nameof(ActionOption.Value),
            DataSource = new[]
            {
                new ActionOption(AcaoImportacaoCliente.CriarNovo, "Criar novo cliente"),
                new ActionOption(AcaoImportacaoCliente.UsarExistente, "Usar cliente existente"),
                new ActionOption(AcaoImportacaoCliente.Ignorar, "Ignorar"),
                new ActionOption(AcaoImportacaoCliente.Revisar, "Revisar antes de importar")
            },
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FillWeight = 20
        });
        foreach (DataGridViewColumn column in _grid.Columns)
            if (column.Width == 100) column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private void GridCellEndEdit(object? sender, DataGridViewCellEventArgs args)
    {
        if (args.RowIndex < 0 || _grid.Rows[args.RowIndex].DataBoundItem is not CandidatoImportacaoCliente candidate)
            return;
        if (_grid.Columns[args.ColumnIndex].DataPropertyName == nameof(CandidatoImportacaoCliente.NomeParaCadastro))
        {
            try
            {
                _service.Reavaliar(candidate);
                _source.ResetItem(args.RowIndex);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Nome inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var unresolved = _candidates.Count(c => c.Acao == AcaoImportacaoCliente.Revisar);
        _summary.Text = $"{_candidates.Sum(c => c.Ocorrencias)} ocorrência(s) • {_candidates.Count(c => c.Acao == AcaoImportacaoCliente.CriarNovo)} novo(s) • {unresolved} para revisar";
        _confirm.Enabled = _candidates.Count > 0 && unresolved == 0;
    }

    private void ConfirmImport()
    {
        _grid.EndEdit();
        _source.EndEdit();
        try
        {
            Resultado = _service.Confirmar(_candidates);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Falha ao confirmar importação de clientes");
            var message = exception.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) || exception.Message.Contains("busy", StringComparison.OrdinalIgnoreCase)
                ? "O banco de dados está sendo usado por outro processo. Aguarde alguns segundos e tente novamente."
                : exception is UnauthorizedAccessException
                    ? "O Windows não permitiu gravar os dados. Verifique as permissões e tente novamente."
                    : exception.Message;
            MessageBox.Show(this, message, "Não foi possível importar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private sealed record ClienteOption(long? Id, string Name);
    private sealed record ActionOption(AcaoImportacaoCliente Value, string Name);
}
