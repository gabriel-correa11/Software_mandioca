using System.Diagnostics;
using System.Globalization;
using Krypton.Toolkit;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Services;
using Serilog;

namespace MandiocaCaipira.App;

/// <summary>
/// Spike visual isolado. Não substitui a tela de Histórico atual.
/// </summary>
public sealed class HistoricoKryptonForm : KryptonForm
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly ClienteService _clientes;
    private readonly VendaService _vendas;
    private readonly PagamentoService _pagamentos;
    private readonly ReciboService _recibos;
    private readonly Action<int> _navigate;
    private readonly KryptonComboBox _client = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly KryptonDateTimePicker _from = DatePicker();
    private readonly KryptonDateTimePicker _to = DatePicker();
    private readonly KryptonDataGridView _sales = Grid();
    private readonly KryptonDataGridView _payments = Grid();
    private readonly ToolStripStatusLabel _status = new("Pronto");
    private readonly SplitContainer _resultsSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 5 };

    public HistoricoKryptonForm(ClienteService clientes, VendaService vendas,
        PagamentoService pagamentos, ReciboService recibos, Action<int> navigate)
    {
        _clientes = clientes;
        _vendas = vendas;
        _pagamentos = pagamentos;
        _recibos = recibos;
        _navigate = navigate;

        Text = "Mandioca Caipira — Histórico (proposta clássica)";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1024, 650);
        Size = new Size(1240, 740);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;
        PaletteMode = PaletteMode.ProfessionalSystem;
        Controls.Add(BuildLayout());
        ConfigureGrids();
        FillClients();
        _from.Value = DateTime.Today.AddMonths(-1);
        WireEvents();
        RefreshHistory();
        Shown += (_, _) =>
        {
            _resultsSplit.Panel1MinSize = 150;
            _resultsSplit.Panel2MinSize = 150;
            _resultsSplit.SplitterDistance = Math.Max(150, (_resultsSplit.Height - _resultsSplit.SplitterWidth) / 2);
            _client.Focus();
        };
    }

    private Control BuildLayout()
    {
        var body = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        body.StateCommon.Color1 = SystemColors.Control;
        body.Controls.Add(BuildResults());
        body.Controls.Add(BuildFilters());
        body.Controls.Add(BuildHeading());
        body.Controls.Add(BuildToolStrip());
        body.Controls.Add(BuildMenu());
        body.Controls.Add(BuildStatus());
        return body;
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("&Arquivo");
        file.DropDownItems.Add("&Fechar proposta", null, (_, _) => Close());
        var registers = new ToolStripMenuItem("&Cadastros");
        registers.DropDownItems.Add("&Clientes", null, (_, _) => Navigate(1));
        var movement = new ToolStripMenuItem("&Movimentação");
        movement.DropDownItems.Add("&Nova venda", null, (_, _) => Navigate(2));
        movement.DropDownItems.Add("&Registrar pagamento", null, (_, _) => Navigate(3));
        movement.DropDownItems.Add(new ToolStripSeparator());
        movement.DropDownItems.Add("&Pendências", null, (_, _) => Navigate(4));
        var queries = new ToolStripMenuItem("&Consultas");
        queries.DropDownItems.Add(new ToolStripMenuItem("&Histórico") { Checked = true, Enabled = false });
        menu.Items.AddRange([file, registers, movement, queries]);
        MainMenuStrip = menu;
        return menu;
    }

    private ToolStrip BuildToolStrip()
    {
        var strip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(5, 2, 5, 2) };
        strip.Items.Add(new ToolStripButton("Atualizar") { ToolTipText = "Atualizar histórico (F5)" });
        strip.Items[0].Click += (_, _) => RefreshHistory();
        return strip;
    }

    private Control BuildHeading()
    {
        var panel = new KryptonPanel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(8, 6, 8, 4) };
        panel.StateCommon.Color1 = SystemColors.Control;
        panel.Controls.Add(new KryptonLabel
        {
            Text = "Histórico de movimentações",
            Dock = DockStyle.Top,
            StateCommon = { ShortText = { Font = new Font("Segoe UI Semibold", 14F), Color1 = Color.FromArgb(43, 66, 50) } }
        });
        panel.Controls.Add(new KryptonLabel
        {
            Text = "Consulte vendas e pagamentos registrados no período.",
            Dock = DockStyle.Bottom,
            StateCommon = { ShortText = { Color1 = Color.FromArgb(80, 85, 82) } }
        });
        return panel;
    }

    private Control BuildFilters()
    {
        var group = new KryptonGroupBox { Text = "Filtros", Dock = DockStyle.Top, Height = 88, Padding = new Padding(6) };
        NeutralGroup(group);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1, Padding = new Padding(4, 5, 4, 4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(Label("Cliente:"), 0, 0);
        layout.Controls.Add(_client, 1, 0);
        layout.Controls.Add(Label("De:"), 2, 0);
        layout.Controls.Add(_from, 3, 0);
        layout.Controls.Add(Label("Até:"), 4, 0);
        layout.Controls.Add(_to, 5, 0);
        var filter = PrimaryButton("&Aplicar filtros", RefreshHistory);
        layout.Controls.Add(filter, 6, 0);
        group.Panel.Controls.Add(layout);
        AcceptButton = filter;
        return group;
    }

    private Control BuildResults()
    {
        _resultsSplit.Panel1.Controls.Add(ResultGroup("Vendas", _sales,
            SecondaryButton("Gerar &comprovante", GenerateSaleReceipt),
            ExceptionalButton("Cancelar venda...", CancelSale)));
        _resultsSplit.Panel2.Controls.Add(ResultGroup("Pagamentos", _payments,
            SecondaryButton("Gerar &recibo", GeneratePaymentReceipt),
            ExceptionalButton("Estornar pagamento...", ReversePayment)));
        return _resultsSplit;
    }

    private static Control ResultGroup(string title, Control grid, params KryptonButton[] actions)
    {
        var group = new KryptonGroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(5) };
        NeutralGroup(group);
        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(3, 4, 3, 2),
            WrapContents = false
        };
        actionBar.Controls.AddRange(actions);
        group.Panel.Controls.Add(grid);
        group.Panel.Controls.Add(actionBar);
        return group;
    }

    private StatusStrip BuildStatus()
    {
        var strip = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = true };
        strip.Items.Add(_status);
        return strip;
    }

    private void ConfigureGrids()
    {
        Columns(_sales,
            ("Cliente", "Cliente", 22, DataGridViewContentAlignment.MiddleLeft),
            ("Data", "Data", 10, DataGridViewContentAlignment.MiddleCenter),
            ("Quantidade", "Quantidade", 11, DataGridViewContentAlignment.MiddleRight),
            ("Total", "Total", 12, DataGridViewContentAlignment.MiddleRight),
            ("Recebido", "Recebido", 12, DataGridViewContentAlignment.MiddleRight),
            ("Saldo", "Saldo", 12, DataGridViewContentAlignment.MiddleRight),
            ("Situacao", "Situação", 10, DataGridViewContentAlignment.MiddleCenter),
            ("Observacao", "Observação", 18, DataGridViewContentAlignment.MiddleLeft));
        Columns(_payments,
            ("Cliente", "Cliente", 26, DataGridViewContentAlignment.MiddleLeft),
            ("Data", "Data", 11, DataGridViewContentAlignment.MiddleCenter),
            ("Valor", "Valor recebido", 15, DataGridViewContentAlignment.MiddleRight),
            ("Aplicado", "Aplicado", 13, DataGridViewContentAlignment.MiddleRight),
            ("Credito", "Crédito", 12, DataGridViewContentAlignment.MiddleRight),
            ("Situacao", "Situação", 10, DataGridViewContentAlignment.MiddleCenter),
            ("Observacao", "Observação", 18, DataGridViewContentAlignment.MiddleLeft));
    }

    private void WireEvents()
    {
        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.F5) { RefreshHistory(); args.Handled = true; }
            if (args.KeyCode == Keys.Escape) { Close(); args.Handled = true; }
        };
        FormClosed += (_, _) => Log.Information("Spike visual Krypton fechado");
    }

    private void FillClients()
    {
        var items = _clientes.Listar(incluirInativos: true)
            .Select(c => new ComboItem(c.Id, c.Ativo ? c.Nome : $"{c.Nome} (inativo)"))
            .Prepend(new ComboItem(0, "Todos os clientes"))
            .ToList();
        _client.DataSource = items;
        _client.DisplayMember = nameof(ComboItem.Name);
        _client.ValueMember = nameof(ComboItem.Id);
    }

    private void RefreshHistory()
    {
        var from = DateOnly.FromDateTime(_from.Value);
        var to = DateOnly.FromDateTime(_to.Value);
        if (from > to) { Warn("A data inicial não pode ser posterior à data final."); return; }
        var client = _client.SelectedValue is long id && id > 0 ? id : (long?)null;
        _sales.DataSource = _vendas.Listar(client, from, to).Select(v => new SaleRow(
            v.Id, v.Cliente, v.DataOperacao.ToString("d", PtBr), $"{v.QuantidadeGramas / 1000m:N3} kg",
            Money(v.TotalCentavos), Money(v.RecebidoCentavos), Money(v.SaldoCentavos),
            v.Cancelada ? "Cancelada" : v.SaldoCentavos == 0 ? "Paga" : v.RecebidoCentavos > 0 ? "Parcial" : "Pendente",
            v.MotivoCancelamento ?? string.Empty)).ToList();
        _payments.DataSource = _pagamentos.Listar(client, from, to).Select(p => new PaymentRow(
            p.Id, p.Cliente, p.DataOperacao.ToString("d", PtBr), Money(p.ValorCentavos),
            Money(p.AlocadoCentavos), Money(p.CreditoCentavos), p.EstornadoEm is null ? "Válido" : "Estornado",
            p.MotivoEstorno ?? string.Empty)).ToList();
        _status.Text = $"Histórico atualizado às {DateTime.Now.ToString("HH:mm", PtBr)}";
    }

    private void GenerateSaleReceipt() { if (Selected<SaleRow>(_sales) is { } row) Run(() => Open(_recibos.GerarVenda(row.Id)), "Comprovante aberto."); else Selection("Selecione uma venda."); }
    private void GeneratePaymentReceipt() { if (Selected<PaymentRow>(_payments) is { } row) Run(() => Open(_recibos.GerarPagamento(row.Id)), "Recibo aberto."); else Selection("Selecione um pagamento."); }
    private void CancelSale()
    {
        if (Selected<SaleRow>(_sales) is not { } row) { Selection("Selecione uma venda."); return; }
        var reason = TextPrompt.Show(this, "Cancelar venda", "Motivo do cancelamento", "A venda permanecerá no histórico.");
        if (reason is null || MessageBox.Show(this, $"Cancelar a venda de {row.Cliente}, no valor de {row.Total}?", "Confirmar cancelamento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Run(() => { _vendas.Cancelar(row.Id, reason); RefreshHistory(); }, "Venda cancelada.");
    }
    private void ReversePayment()
    {
        if (Selected<PaymentRow>(_payments) is not { } row) { Selection("Selecione um pagamento."); return; }
        var reason = TextPrompt.Show(this, "Estornar pagamento", "Motivo do estorno", "As vendas afetadas voltarão a apresentar saldo.");
        if (reason is null || MessageBox.Show(this, $"Estornar o pagamento de {row.Valor} recebido de {row.Cliente}?", "Confirmar estorno", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Run(() => { _pagamentos.Estornar(row.Id, reason); RefreshHistory(); }, "Pagamento estornado.");
    }

    private void Navigate(int page) { Close(); _navigate(page); }
    private void Run(Action action, string success) { try { action(); _status.Text = success; } catch (Exception ex) { Log.Warning(ex, "Operação no spike Krypton falhou"); Warn(ex.Message); } }
    private void Warn(string message) => MessageBox.Show(this, message, "Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private void Selection(string message) => MessageBox.Show(this, message, "Seleção necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void Open(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    private static T? Selected<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;
    private static string Money(long cents) => (cents / 100m).ToString("C", PtBr);
    private static KryptonLabel Label(string text) => new() { Text = text, Anchor = AnchorStyles.Left, AutoSize = true };
    private static KryptonDateTimePicker DatePicker() => new() { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    private static KryptonButton PrimaryButton(string text, Action action)
    {
        var button = BaseButton(text, action);
        button.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 9F);
        return button;
    }
    private static KryptonButton SecondaryButton(string text, Action action) => BaseButton(text, action);
    private static KryptonButton ExceptionalButton(string text, Action action)
    {
        var button = BaseButton(text, action);
        button.StateCommon.Content.ShortText.Color1 = Color.FromArgb(132, 49, 44);
        return button;
    }
    private static KryptonButton BaseButton(string text, Action action)
    {
        var button = new KryptonButton { Text = text, AutoSize = true, MinimumSize = new Size(110, 28), Margin = new Padding(4, 0, 0, 0) };
        button.Click += (_, _) => action();
        return button;
    }
    private static KryptonDataGridView Grid()
    {
        var grid = new KryptonDataGridView
        {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BorderStyle = BorderStyle.Fixed3D,
        RowTemplate = { Height = 25 },
        ColumnHeadersHeight = 30,
        BackgroundColor = SystemColors.Window,
        GridColor = Color.FromArgb(210, 210, 210),
        StandardTab = true,
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SystemColors.Window,
            ForeColor = Color.FromArgb(35, 35, 35),
            SelectionBackColor = Color.FromArgb(62, 105, 73),
            SelectionForeColor = Color.White
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 246, 244) }
        };
        grid.StateCommon.Background.Color1 = SystemColors.Window;
        return grid;
    }

    private static void NeutralGroup(KryptonGroupBox group)
    {
        group.StateCommon.Back.Color1 = SystemColors.Control;
        group.Panel.StateCommon.Color1 = SystemColors.Control;
    }
    private static void Columns(DataGridView grid, params (string Property, string Header, int Weight, DataGridViewContentAlignment Alignment)[] columns)
    {
        foreach (var column in columns)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = column.Property,
                HeaderText = column.Header,
                FillWeight = column.Weight,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = column.Alignment, Padding = new Padding(3, 0, 3, 0) }
            });
    }

    private sealed record ComboItem(long Id, string Name);
    private sealed record SaleRow(long Id, string Cliente, string Data, string Quantidade, string Total, string Recebido, string Saldo, string Situacao, string Observacao);
    private sealed record PaymentRow(long Id, string Cliente, string Data, string Valor, string Aplicado, string Credito, string Situacao, string Observacao);
}
