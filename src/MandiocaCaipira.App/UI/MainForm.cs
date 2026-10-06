using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using MandiocaCaipira.App.Data;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using Serilog;

namespace MandiocaCaipira.App;

public sealed class MainForm : Form
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly ClienteService _clientes; private readonly VendaService _vendas; private readonly PagamentoService _pagamentos;
    private readonly ResumoRepository _resumo; private readonly ReciboService _recibos; private readonly BackupService _backup;
    private readonly TabControl _pages = new() { Dock = DockStyle.Fill, Appearance = TabAppearance.FlatButtons, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(0, 1) };
    private readonly Label _title = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 17), ForeColor = Ui.Text };
    private readonly ToolStripStatusLabel _status = new("Pronto"); private readonly Dictionary<int, Button> _nav = [];
    private readonly DataGridView _clients = Ui.Grid(), _pending = Ui.Grid(), _sales = Ui.Grid(), _payments = Ui.Grid(), _distribution = Ui.Grid(false);
    private readonly ComboBox _saleClient = Ui.Combo(), _paymentClient = Ui.Combo(), _historyClient = Ui.Combo();
    private readonly NumericUpDown _kg = Ui.Number(3), _price = Ui.Number(2), _initial = Ui.Number(2), _received = Ui.Number(2);
    private readonly DateTimePicker _saleDate = Ui.Date(), _paymentDate = Ui.Date(), _from = Ui.Date(), _to = Ui.Date();
    private readonly Label _total = Ui.Value("R$ 0,00", 22), _credit = Ui.Value("R$ 0,00", 15), _debt = Ui.Value("R$ 0,00", 15);
    private readonly Label _daySales = Ui.Value("0"), _dayKg = Ui.Value("0,000 kg"), _dayValue = Ui.Value("R$ 0,00"), _dayReceipts = Ui.Value("R$ 0,00"), _openBalance = Ui.Value("R$ 0,00");
    private readonly TextBox _search = new() { Width = 280, PlaceholderText = "Pesquisar por nome" };
    private readonly CheckBox _inactive = new() { Text = "Incluir clientes inativos", AutoSize = true, Padding = new Padding(0, 7, 0, 0) };
    private BindingList<DistributionRow> _lines = [];

    public MainForm(ClienteService clientes, VendaService vendas, PagamentoService pagamentos, ResumoRepository resumo, ReciboService recibos, BackupService backup)
    {
        _clientes = clientes; _vendas = vendas; _pagamentos = pagamentos; _resumo = resumo; _recibos = recibos; _backup = backup;
        Text = "Mandioca Caipira"; MinimumSize = new Size(1120, 720); Size = new Size(1360, 820); StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.Font; BackColor = Ui.Background; AutoScaleMode = AutoScaleMode.Dpi; Controls.Add(BuildShell()); ConfigureGrids(); WireEvents(); ShowPage(0); RefreshAll();
    }

    private Control BuildShell()
    {
        _pages.TabPages.AddRange([HomePage(), ClientsPage(), SalePage(), PaymentPage(), PendingPage(), HistoryPage()]);
        var header = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.White, Padding = new Padding(24, 18, 20, 8) }; header.Controls.Add(_title);
        var status = new StatusStrip { SizingGrip = false, BackColor = Color.White }; status.Items.Add(_status);
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Ui.Background }; content.Controls.Add(_pages); content.Controls.Add(header); content.Controls.Add(status);
        var shell = new Panel { Dock = DockStyle.Fill }; shell.Controls.Add(content); shell.Controls.Add(BuildNavigation()); return shell;
    }

    private Control BuildNavigation()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 205, BackColor = Ui.Nav, Padding = new Padding(12, 18, 12, 12) };
        var brand = new Label { Text = "MANDIOCA\nCAIPIRA", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 15), Height = 70, Dock = DockStyle.Top };
        var menu = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 390, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        string[] labels = ["Início", "Clientes", "Nova venda", "Registrar pagamento", "Pendências", "Histórico"];
        for (var i = 0; i < labels.Length; i++) { var index = i; var b = Ui.NavButton(labels[i], () => ShowPage(index)); _nav[index] = b; menu.Controls.Add(b); }
        var backup = Ui.NavButton("Fazer backup agora", CreateBackup); var folder = Ui.NavButton("Abrir pasta de backups", () => OpenFolder(CaminhosDados.Backups)); var receipts = Ui.NavButton("Abrir comprovantes", () => OpenFolder(CaminhosDados.Recibos));
        panel.Controls.Add(folder); folder.Dock = DockStyle.Bottom; panel.Controls.Add(backup); backup.Dock = DockStyle.Bottom; panel.Controls.Add(receipts); receipts.Dock = DockStyle.Bottom; panel.Controls.Add(menu); panel.Controls.Add(brand); return panel;
    }

    private TabPage HomePage()
    {
        var page = Ui.Page("Início"); var summary = new TableLayoutPanel { Dock = DockStyle.Top, Height = 118, ColumnCount = 5, Padding = new Padding(0, 5, 0, 10) };
        for (var i = 0; i < 5; i++) summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        summary.Controls.Add(Ui.Summary("Vendas de hoje", _daySales), 0, 0); summary.Controls.Add(Ui.Summary("Quantidade vendida", _dayKg), 1, 0); summary.Controls.Add(Ui.Summary("Valor vendido", _dayValue), 2, 0); summary.Controls.Add(Ui.Summary("Recebimentos de hoje", _dayReceipts), 3, 0); summary.Controls.Add(Ui.Summary("Saldo em aberto", _openBalance), 4, 0);
        var actions = Ui.Section("Acesso rápido", "Operações mais usadas no dia a dia", 150); var buttons = Ui.Toolbar(); buttons.Controls.AddRange([Ui.PrimaryButton("Registrar nova venda", () => ShowPage(2)), Ui.PrimaryButton("Registrar pagamento", () => ShowPage(3)), Ui.SecondaryButton("Ver pendências", () => ShowPage(4)), Ui.SecondaryButton("Atualizar resumo", RefreshSummary)]); actions.Controls.Add(buttons);
        page.Controls.Add(actions); page.Controls.Add(summary); return page;
    }
    private TabPage ClientsPage() { var page = Ui.Page("Clientes"); var bar = Ui.Toolbar(); bar.Controls.AddRange([_search, _inactive, Ui.PrimaryButton("Cadastrar cliente", NewClient), Ui.SecondaryButton("Inativar ou reativar", ToggleClient)]); page.Controls.Add(_clients); page.Controls.Add(bar); return page; }
    private TabPage SalePage()
    {
        var page = Ui.Page("Nova venda"); var box = Ui.Section("Dados da venda", "Preencha os dados e confira o total antes de registrar.", 410); var form = Ui.Form(6);
        Ui.Field(form, "Cliente", _saleClient, 0); Ui.Field(form, "Quantidade (kg)", _kg, 1); Ui.Field(form, "Preço por kg", _price, 2); Ui.Field(form, "Data da operação", _saleDate, 3); Ui.Field(form, "Pagamento inicial (opcional)", _initial, 4);
        var totalPanel = new Panel { Dock = DockStyle.Fill, Height = 72, BackColor = Ui.LightGreen, Padding = new Padding(12, 7, 12, 6) }; _total.Dock = DockStyle.Fill; totalPanel.Controls.Add(_total); Ui.Field(form, "Total da venda", totalPanel, 5);
        var submit = Ui.PrimaryButton("Registrar venda", RegisterSale); submit.Dock = DockStyle.Bottom; box.Controls.Add(submit); box.Controls.Add(form); page.Controls.Add(box); return page;
    }
    private TabPage PaymentPage()
    {
        var page = Ui.Page("Registrar pagamento"); var top = Ui.Section("Recebimento", "Escolha o cliente, informe o valor e confira a distribuição.", 225); var form = Ui.Form(3);
        Ui.Field(form, "Cliente", _paymentClient, 0); Ui.Field(form, "Valor recebido", _received, 1); Ui.Field(form, "Data do recebimento", _paymentDate, 2);
        var metrics = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 350, FlowDirection = FlowDirection.TopDown, Padding = new Padding(15, 4, 0, 0) }; metrics.Controls.Add(Ui.InlineMetric("Dívida atual", _debt)); metrics.Controls.Add(Ui.InlineMetric("Crédito restante", _credit)); top.Controls.Add(metrics); top.Controls.Add(form);
        var actions = Ui.Toolbar(); actions.Controls.AddRange([Ui.SecondaryButton("Distribuir automaticamente", Distribute), Ui.PrimaryButton("Confirmar pagamento", RegisterPayment)]);
        var open = Ui.Section("Vendas em aberto", "Use a última coluna para definir quanto será destinado a cada venda.", 400); open.Dock = DockStyle.Fill; open.Controls.Add(_distribution);
        page.Controls.Add(open); page.Controls.Add(actions); page.Controls.Add(top); return page;
    }
    private TabPage PendingPage() { var page = Ui.Page("Pendências"); var bar = Ui.Toolbar(); bar.Controls.AddRange([Ui.PrimaryButton("Registrar pagamento da seleção", PaySelected), Ui.SecondaryButton("Atualizar", RefreshPending)]); page.Controls.Add(_pending); page.Controls.Add(bar); return page; }
    private TabPage HistoryPage()
    {
        var page = Ui.Page("Histórico"); _from.Value = DateTime.Today.AddMonths(-1); var filters = Ui.Toolbar(); filters.Controls.AddRange([Ui.Caption("Cliente"), _historyClient, Ui.Caption("De"), _from, Ui.Caption("Até"), _to, Ui.PrimaryButton("Aplicar filtros", RefreshHistory)]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 285, BackColor = Ui.Border }; split.Panel1.Padding = new Padding(0, 36, 0, 0); split.Panel2.Padding = new Padding(0, 36, 0, 0);
        split.Panel1.Controls.Add(_sales); split.Panel1.Controls.Add(Ui.SectionHeader("Vendas", Ui.DangerButton("Cancelar venda", CancelSale), Ui.SecondaryButton("Gerar comprovante", GenerateSaleReceipt)));
        split.Panel2.Controls.Add(_payments); split.Panel2.Controls.Add(Ui.SectionHeader("Pagamentos", Ui.DangerButton("Estornar pagamento", ReversePayment), Ui.SecondaryButton("Gerar recibo", GeneratePaymentReceipt)));
        page.Controls.Add(split); page.Controls.Add(filters); return page;
    }

    private void ConfigureGrids()
    {
        Ui.Columns(_clients, ("Name", "Nome", 55, DataGridViewContentAlignment.MiddleLeft), ("Situacao", "Situação", 20, DataGridViewContentAlignment.MiddleCenter), ("Cadastro", "Cadastrado em", 25, DataGridViewContentAlignment.MiddleCenter));
        Ui.Columns(_pending, ("Client", "Cliente", 32, DataGridViewContentAlignment.MiddleLeft), ("Data", "Data", 15, DataGridViewContentAlignment.MiddleCenter), ("Total", "Valor original", 18, DataGridViewContentAlignment.MiddleRight), ("Recebido", "Recebido", 17, DataGridViewContentAlignment.MiddleRight), ("Saldo", "Saldo", 18, DataGridViewContentAlignment.MiddleRight));
        Ui.Columns(_sales, ("Client", "Cliente", 20, DataGridViewContentAlignment.MiddleLeft), ("Data", "Data", 9, DataGridViewContentAlignment.MiddleCenter), ("Quantidade", "Quantidade", 10, DataGridViewContentAlignment.MiddleRight), ("Total", "Total", 11, DataGridViewContentAlignment.MiddleRight), ("Recebido", "Recebido", 11, DataGridViewContentAlignment.MiddleRight), ("Saldo", "Saldo", 11, DataGridViewContentAlignment.MiddleRight), ("Situacao", "Situação", 11, DataGridViewContentAlignment.MiddleCenter), ("Motivo", "Observação", 17, DataGridViewContentAlignment.MiddleLeft));
        Ui.Columns(_payments, ("Client", "Cliente", 22, DataGridViewContentAlignment.MiddleLeft), ("Data", "Data", 10, DataGridViewContentAlignment.MiddleCenter), ("Value", "Valor recebido", 14, DataGridViewContentAlignment.MiddleRight), ("Aplicado", "Aplicado", 12, DataGridViewContentAlignment.MiddleRight), ("Credito", "Crédito", 11, DataGridViewContentAlignment.MiddleRight), ("Situacao", "Situação", 11, DataGridViewContentAlignment.MiddleCenter), ("Motivo", "Observação", 20, DataGridViewContentAlignment.MiddleLeft));
        Ui.Columns(_distribution, ("Data", "Data da venda", 15, DataGridViewContentAlignment.MiddleCenter), ("Total", "Valor original", 20, DataGridViewContentAlignment.MiddleRight), ("Recebido", "Já recebido", 20, DataGridViewContentAlignment.MiddleRight), ("Saldo", "Saldo atual", 20, DataGridViewContentAlignment.MiddleRight), ("Aplicar", "Aplicar neste pagamento", 25, DataGridViewContentAlignment.MiddleRight));
        foreach (DataGridViewColumn column in _distribution.Columns) column.ReadOnly = true;
        if (_distribution.Columns["Aplicar"] is { } applyColumn) { applyColumn.ReadOnly = false; applyColumn.DefaultCellStyle.Format = "N2"; } _distribution.EditMode = DataGridViewEditMode.EditOnEnter;
    }
    private void WireEvents()
    {
        _search.TextChanged += (_, _) => RefreshClients(); _inactive.CheckedChanged += (_, _) => RefreshClients(); _kg.ValueChanged += (_, _) => CalculateTotal(); _price.ValueChanged += (_, _) => CalculateTotal();
        _paymentClient.SelectedIndexChanged += (_, _) => LoadDistribution(); _received.ValueChanged += (_, _) => RefreshPaymentMetrics(); _distribution.CellValueChanged += (_, _) => RefreshPaymentMetrics();
        _distribution.DataError += (_, args) => { args.ThrowException = false; ShowError("Informe um valor monetário válido na distribuição."); };
        FormClosing += (_, _) => Log.Information("Janela principal encerrada pelo usuário");
    }
    private void ShowPage(int index) { _pages.SelectedIndex = index; _title.Text = _pages.TabPages[index].Text; foreach (var n in _nav) n.Value.BackColor = n.Key == index ? Ui.NavSelected : Ui.Nav; if (index == 0) RefreshSummary(); if (index == 3) LoadDistribution(); if (index == 4) RefreshPending(); if (index == 5) RefreshHistory(); }
    private void RefreshAll() { RefreshClients(); RefreshCombos(); RefreshPending(); RefreshHistory(); RefreshSummary(); CalculateTotal(); }
    private void RefreshClients() => _clients.DataSource = _clientes.Listar(_search.Text, _inactive.Checked).Select(c => new ClientRow(c.Id, c.Nome, c.Ativo ? "Ativo" : "Inativo", c.CriadoEm.LocalDateTime.ToString("g", PtBr), c.Ativo)).ToList();
    private void RefreshCombos() { Fill(_saleClient, _clientes.Listar(), false); Fill(_paymentClient, _clientes.Listar(), false); Fill(_historyClient, _clientes.Listar(incluirInativos: true), true); }
    private void RefreshPending() => _pending.DataSource = _vendas.Listar(somentePendentes: true).Select(ToSaleRow).ToList();
    private void RefreshSummary() { var r = _resumo.Obter(DateOnly.FromDateTime(DateTime.Today)); _daySales.Text = r.VendasRealizadas.ToString("N0", PtBr); _dayKg.Text = Kg(r.QuantidadeGramas); _dayValue.Text = Money(r.ValorVendidoCentavos); _dayReceipts.Text = Money(r.RecebimentosCentavos); _openBalance.Text = Money(_vendas.Listar(somentePendentes: true).Sum(v => v.SaldoCentavos)); SetStatus("Resumo atualizado."); }
    private void RefreshHistory()
    {
        var from = DateOnly.FromDateTime(_from.Value); var to = DateOnly.FromDateTime(_to.Value); if (from > to) { ShowError("A data inicial não pode ser posterior à data final."); return; } var client = SelectedId(_historyClient);
        _sales.DataSource = _vendas.Listar(client == 0 ? null : client, from, to).Select(ToSaleRow).ToList(); _payments.DataSource = _pagamentos.Listar(client == 0 ? null : client, from, to).Select(p => new PaymentRow(p.Id, p.ClienteId, p.Cliente, p.DataOperacao.ToString("d", PtBr), Money(p.ValorCentavos), Money(p.AlocadoCentavos), Money(p.CreditoCentavos), p.EstornadoEm is null ? "Válido" : "Estornado", p.MotivoEstorno)).ToList(); SetStatus("Histórico atualizado.");
    }
    private static SaleRow ToSaleRow(VendaResumo v) => new(v.Id, v.ClienteId, v.Cliente, v.DataOperacao.ToString("d", PtBr), Kg(v.QuantidadeGramas), Money(v.TotalCentavos), Money(v.RecebidoCentavos), Money(v.SaldoCentavos), v.Cancelada ? "Cancelada" : v.SaldoCentavos == 0 ? "Paga" : v.RecebidoCentavos > 0 ? "Parcial" : "Pendente", v.MotivoCancelamento);

    private void NewClient() { var name = TextPrompt.Show(this, "Cadastrar cliente", "Nome do cliente", "Informe como o cliente será identificado nas vendas e pagamentos."); if (name is not null) Run(() => { _clientes.Cadastrar(name); RefreshClients(); RefreshCombos(); }, "Cliente cadastrado com sucesso."); }
    private void ToggleClient() { if (Selected<ClientRow>(_clients) is not { } row) { ShowSelection("Selecione um cliente."); return; } Run(() => { _clientes.DefinirAtivo(row.Id, !row.Ativo); RefreshClients(); RefreshCombos(); }, row.Ativo ? "Cliente inativado." : "Cliente reativado."); }
    private void CalculateTotal() { try { _total.Text = Money(_vendas.CalcularTotal(Grams(_kg.Value), Cents(_price.Value))); } catch { _total.Text = "R$ 0,00"; } }
    private void RegisterSale()
    {
        long id = 0; if (!Run(() => id = _vendas.Registrar(SelectedId(_saleClient), Grams(_kg.Value), Cents(_price.Value), DateOnly.FromDateTime(_saleDate.Value), Cents(_initial.Value)), "Venda registrada com sucesso.")) return;
        RefreshAll(); _kg.Value = _kg.Minimum; _price.Value = 0; _initial.Value = 0; _saleClient.Focus(); if (MessageBox.Show(this, "Deseja abrir o comprovante desta venda?", "Venda registrada", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) GenerateAndOpen(() => _recibos.GerarVenda(id));
    }
    private void LoadDistribution() { var id = SelectedId(_paymentClient); _lines = new BindingList<DistributionRow>((id == 0 ? [] : _vendas.Listar(id, somentePendentes: true).Select(v => new DistributionRow(v))).ToList()); _distribution.DataSource = _lines; RefreshPaymentMetrics(); }
    private void Distribute() { var rest = Cents(_received.Value); foreach (var line in _lines.OrderBy(x => x.OperationDate)) { line.ApplyCents = Math.Min(rest, line.BalanceCents); rest -= line.ApplyCents; } _distribution.Refresh(); RefreshPaymentMetrics(); }
    private void RefreshPaymentMetrics() { _distribution.EndEdit(); var credit = Cents(_received.Value) - _lines.Sum(x => x.ApplyCents); _debt.Text = Money(_lines.Sum(x => x.BalanceCents)); _credit.Text = Money(credit); _credit.ForeColor = credit < 0 ? Ui.Danger : Ui.Text; }
    private void RegisterPayment()
    {
        _distribution.EndEdit(); var value = Cents(_received.Value); if (_lines.Any(x => x.ApplyCents < 0)) { ShowError("A distribuição não pode conter valores negativos."); return; } var items = _lines.Where(x => x.ApplyCents > 0).Select(x => new AlocacaoSolicitada(x.SaleId, x.ApplyCents)).ToList(); var allocated = items.Sum(x => x.ValorCentavos); if (value <= 0) { ShowError("Informe o valor recebido."); return; }
        if (MessageBox.Show(this, $"Valor recebido: {Money(value)}\nDestinado às vendas: {Money(allocated)}\nCrédito restante: {Money(value - allocated)}\n\nConfirmar o pagamento?", "Confirmar pagamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        long id = 0; if (!Run(() => id = _pagamentos.Registrar(SelectedId(_paymentClient), value, DateOnly.FromDateTime(_paymentDate.Value), items), "Pagamento registrado com sucesso.")) return; _received.Value = 0; RefreshAll(); if (MessageBox.Show(this, "Deseja abrir o recibo deste pagamento?", "Pagamento registrado", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) GenerateAndOpen(() => _recibos.GerarPagamento(id));
    }
    private void PaySelected() { if (Selected<SaleRow>(_pending) is not { } row) { ShowSelection("Selecione uma pendência."); return; } _paymentClient.SelectedValue = row.ClientId; ShowPage(3); }
    private void CancelSale() { if (Selected<SaleRow>(_sales) is not { } row) { ShowSelection("Selecione uma venda."); return; } var reason = TextPrompt.Show(this, "Cancelar venda", "Motivo do cancelamento", "A venda permanecerá no histórico."); if (reason is null || MessageBox.Show(this, $"Cancelar a venda de {row.Client}, no valor de {row.Total}?", "Confirmar cancelamento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; Run(() => { _vendas.Cancelar(row.Id, reason); RefreshAll(); }, "Venda cancelada."); }
    private void ReversePayment() { if (Selected<PaymentRow>(_payments) is not { } row) { ShowSelection("Selecione um pagamento."); return; } var reason = TextPrompt.Show(this, "Estornar pagamento", "Motivo do estorno", "As vendas afetadas voltarão a apresentar saldo."); if (reason is null || MessageBox.Show(this, $"Estornar o pagamento de {row.Value} recebido de {row.Client}?\n\nOs saldos afetados serão reabertos.", "Confirmar estorno", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; Run(() => { _pagamentos.Estornar(row.Id, reason); RefreshAll(); }, "Pagamento estornado."); }
    private void GenerateSaleReceipt() { if (Selected<SaleRow>(_sales) is { } row) GenerateAndOpen(() => _recibos.GerarVenda(row.Id)); else ShowSelection("Selecione uma venda."); }
    private void GeneratePaymentReceipt() { if (Selected<PaymentRow>(_payments) is { } row) GenerateAndOpen(() => _recibos.GerarPagamento(row.Id)); else ShowSelection("Selecione um pagamento."); }
    private void GenerateAndOpen(Func<string> generate) => Run(() => Process.Start(new ProcessStartInfo(generate()) { UseShellExecute = true }), "Comprovante gerado e aberto.", false);
    private void CreateBackup() => Run(() => { var path = _backup.Criar(); MessageBox.Show(this, $"Backup criado com sucesso em:\n{path}", "Backup concluído", MessageBoxButtons.OK, MessageBoxIcon.Information); }, "Backup concluído.", false);

    private bool Run(Action action, string success, bool showSuccess = true) { try { action(); SetStatus(success); if (showSuccess) MessageBox.Show(this, success, "Mandioca Caipira", MessageBoxButtons.OK, MessageBoxIcon.Information); return true; } catch (Exception ex) { Log.Warning(ex, "Operação solicitada pelo usuário não pôde ser concluída"); ShowError(FriendlyError(ex)); return false; } }
    private static string FriendlyError(Exception ex) => ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase) ? "O banco de dados está sendo usado por outro processo. Aguarde alguns segundos e tente novamente." : ex is UnauthorizedAccessException ? "O Windows não permitiu gravar o arquivo. Verifique as permissões e tente novamente." : ex.Message;
    private void SetStatus(string text) => _status.Text = $"{text}  •  {DateTime.Now.ToString("T", PtBr)}"; private void ShowError(string text) => MessageBox.Show(this, text, "Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Warning); private void ShowSelection(string text) => MessageBox.Show(this, text, "Seleção necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void OpenFolder(string path) { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true }); }
    private static T? Selected<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;
    private static void Fill(ComboBox combo, IReadOnlyList<Cliente> source, bool all) { var old = SelectedId(combo); var list = source.Select(x => new ComboItem(x.Id, x.Ativo ? x.Nome : $"{x.Nome} (inativo)")).ToList(); if (all) list.Insert(0, new ComboItem(0, "Todos os clientes")); combo.DataSource = list; combo.DisplayMember = nameof(ComboItem.Name); combo.ValueMember = nameof(ComboItem.Id); if (old > 0) combo.SelectedValue = old; }
    private static long SelectedId(ComboBox combo) => combo.SelectedValue is long id ? id : 0; private static long Grams(decimal kg) => checked((long)decimal.Round(kg * 1000m, 0, MidpointRounding.AwayFromZero)); private static long Cents(decimal value) => checked((long)decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero)); private static string Money(long cents) => (cents / 100m).ToString("C", PtBr); private static string Kg(long grams) => $"{grams / 1000m:N3} kg";
    private sealed record ComboItem(long Id, string Name); private sealed record ClientRow(long Id, string Name, string Situacao, string Cadastro, bool Ativo); private sealed record SaleRow(long Id, long ClientId, string Client, string Data, string Quantidade, string Total, string Recebido, string Saldo, string Situacao, string? Motivo); private sealed record PaymentRow(long Id, long ClientId, string Client, string Data, string Value, string Aplicado, string Credito, string Situacao, string? Motivo);
    private sealed class DistributionRow(VendaResumo sale) { public long SaleId { get; } = sale.Id; public DateOnly OperationDate { get; } = sale.DataOperacao; public string Data => sale.DataOperacao.ToString("d", PtBr); public string Total => Money(sale.TotalCentavos); public string Recebido => Money(sale.RecebidoCentavos); public string Saldo => Money(sale.SaldoCentavos); public long BalanceCents { get; } = sale.SaldoCentavos; public long ApplyCents { get; set; } public decimal Aplicar { get => ApplyCents / 100m; set => ApplyCents = Cents(value); } }
}
