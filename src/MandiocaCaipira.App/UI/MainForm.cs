using System.Globalization;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using MandiocaCaipira.App.Services;
using Serilog;

namespace MandiocaCaipira.App;

public sealed class MainForm : Form
{
    private readonly ClienteService _clientes;
    private readonly VendaService _vendas;
    private readonly PagamentoService _pagamentos;
    private readonly ResumoRepository _resumo;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _clientesGrid = Grid();
    private readonly DataGridView _pendenciasGrid = Grid();
    private readonly DataGridView _vendasGrid = Grid();
    private readonly DataGridView _pagamentosGrid = Grid();
    private readonly DataGridView _alocacoesGrid = Grid(false);
    private readonly ComboBox _vendaCliente = Combo(), _pagamentoCliente = Combo(), _historicoCliente = Combo();
    private readonly NumericUpDown _kg = Numero(3), _preco = Numero(2), _inicial = Numero(2), _recebido = Numero(2);
    private readonly DateTimePicker _dataVenda = Data(), _dataPagamento = Data(), _inicio = Data(), _fim = Data();
    private readonly Label _total = Valor("R$ 0,00"), _credito = Valor("Crédito: R$ 0,00");
    private readonly Label _rVendas = Valor("0"), _rKg = Valor("0 kg"), _rValor = Valor("R$ 0,00"), _rRecebido = Valor("R$ 0,00");
    private readonly TextBox _pesquisa = new() { Width = 240, PlaceholderText = "Pesquisar cliente" };
    private readonly CheckBox _inativos = new() { Text = "Mostrar inativos", AutoSize = true };
    private List<AlocacaoLinha> _linhas = [];

    public MainForm(ClienteService clientes, VendaService vendas, PagamentoService pagamentos, ResumoRepository resumo)
    {
        _clientes = clientes; _vendas = vendas; _pagamentos = pagamentos; _resumo = resumo;
        Text = "Mandioca Caipira"; WindowState = FormWindowState.Maximized; MinimumSize = new Size(1050, 680);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10); Controls.Add(_tabs);
        _tabs.TabPages.AddRange([Inicio(), Clientes(), NovaVenda(), Pagamentos(), Pendencias(), Historico()]);
        AtualizarTudo();
    }

    private TabPage Inicio()
    {
        var p = Page("Início"); var cards = Bar(125);
        cards.Controls.AddRange([Card("Vendas de hoje", _rVendas), Card("Quantidade", _rKg), Card("Valor vendido", _rValor), Card("Recebimentos", _rRecebido)]);
        var actions = Bar(); actions.Controls.AddRange([Button("Nova Venda", () => _tabs.SelectedIndex = 2), Button("Novo Pagamento", () => _tabs.SelectedIndex = 3), Button("Pendências", () => _tabs.SelectedIndex = 4), Button("Atualizar", AtualizarResumo)]);
        p.Controls.Add(actions); p.Controls.Add(cards); return p;
    }

    private TabPage Clientes()
    {
        var p = Page("Clientes"); var b = Bar(); b.Controls.AddRange([_pesquisa, _inativos, Button("Novo Cliente", NovoCliente), Button("Inativar / Reativar", AlternarCliente)]);
        _pesquisa.TextChanged += (_, _) => AtualizarClientes(); _inativos.CheckedChanged += (_, _) => AtualizarClientes();
        p.Controls.Add(_clientesGrid); p.Controls.Add(b); return p;
    }

    private TabPage NovaVenda()
    {
        var p = Page("Nova Venda"); var f = FormPanel();
        Field(f, "Cliente", _vendaCliente, 0); Field(f, "Quantidade (kg)", _kg, 1); Field(f, "Preço por kg", _preco, 2);
        Field(f, "Data da operação", _dataVenda, 3); Field(f, "Pagamento inicial (opcional)", _inicial, 4); Field(f, "Total", _total, 5);
        f.Controls.Add(Button("Registrar Venda", RegistrarVenda), 1, 6);
        _kg.ValueChanged += (_, _) => CalcularTotal(); _preco.ValueChanged += (_, _) => CalcularTotal();
        p.Controls.Add(f); return p;
    }

    private TabPage Pagamentos()
    {
        var p = Page("Pagamentos"); var b = Bar(100);
        b.Controls.AddRange([Label("Cliente"), _pagamentoCliente, Label("Valor recebido"), _recebido, Label("Data"), _dataPagamento,
            Button("Distribuir automaticamente", Distribuir), Button("Confirmar Pagamento", RegistrarPagamento), _credito]);
        _pagamentoCliente.SelectedIndexChanged += (_, _) => CarregarAlocacoes(); _recebido.ValueChanged += (_, _) => AtualizarCredito();
        _alocacoesGrid.CellValueChanged += (_, _) => AtualizarCredito();
        p.Controls.Add(_alocacoesGrid); p.Controls.Add(b); return p;
    }

    private TabPage Pendencias()
    {
        var p = Page("Pendências"); var b = Bar(); b.Controls.AddRange([Button("Atualizar", AtualizarPendencias), Button("Registrar pagamento", PagarSelecionada)]);
        p.Controls.Add(_pendenciasGrid); p.Controls.Add(b); return p;
    }

    private TabPage Historico()
    {
        var p = Page("Histórico"); _inicio.Value = DateTime.Today.AddMonths(-1); var b = Bar();
        b.Controls.AddRange([Label("Cliente"), _historicoCliente, Label("De"), _inicio, Label("Até"), _fim, Button("Filtrar", AtualizarHistorico), Button("Cancelar venda", CancelarVenda), Button("Estornar pagamento", EstornarPagamento)]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        split.Panel1.Controls.Add(_vendasGrid); split.Panel1.Controls.Add(Title("Vendas")); split.Panel2.Controls.Add(_pagamentosGrid); split.Panel2.Controls.Add(Title("Pagamentos"));
        p.Controls.Add(split); p.Controls.Add(b); return p;
    }

    private void AtualizarTudo() { AtualizarClientes(); AtualizarCombos(); AtualizarPendencias(); AtualizarHistorico(); AtualizarResumo(); CalcularTotal(); }
    private void AtualizarClientes() => _clientesGrid.DataSource = _clientes.Listar(_pesquisa.Text, _inativos.Checked).Select(c => new { c.Id, c.Nome, Situação = c.Ativo ? "Ativo" : "Inativo", Cadastro = c.CriadoEm.LocalDateTime }).ToList();
    private void AtualizarCombos() { Fill(_vendaCliente, _clientes.Listar(), false); Fill(_pagamentoCliente, _clientes.Listar(), false); Fill(_historicoCliente, _clientes.Listar(incluirInativos: true), true); }
    private void AtualizarPendencias() => _pendenciasGrid.DataSource = _vendas.Listar(somentePendentes: true).Select(v => new { v.Id, v.ClienteId, v.Cliente, Data = v.DataOperacao, Total = Money(v.TotalCentavos), Recebido = Money(v.RecebidoCentavos), Saldo = Money(v.SaldoCentavos) }).ToList();
    private void AtualizarResumo() { var r = _resumo.Obter(DateOnly.FromDateTime(DateTime.Today)); _rVendas.Text = r.VendasRealizadas.ToString(); _rKg.Text = $"{r.QuantidadeGramas / 1000m:N3} kg"; _rValor.Text = Money(r.ValorVendidoCentavos); _rRecebido.Text = Money(r.RecebimentosCentavos); }

    private void AtualizarHistorico()
    {
        var id = SelectedId(_historicoCliente); var de = DateOnly.FromDateTime(_inicio.Value); var ate = DateOnly.FromDateTime(_fim.Value);
        _vendasGrid.DataSource = _vendas.Listar(id == 0 ? null : id, de, ate).Select(v => new { v.Id, v.Cliente, Data = v.DataOperacao, Quantidade = $"{v.QuantidadeGramas / 1000m:N3} kg", Total = Money(v.TotalCentavos), Recebido = Money(v.RecebidoCentavos), Saldo = Money(v.SaldoCentavos), Situação = v.Cancelada ? "Cancelada" : v.SaldoCentavos == 0 ? "Paga" : v.RecebidoCentavos > 0 ? "Parcial" : "Pendente", v.MotivoCancelamento }).ToList();
        _pagamentosGrid.DataSource = _pagamentos.Listar(id == 0 ? null : id, de, ate).Select(x => new { x.Id, x.Cliente, Data = x.DataOperacao, Valor = Money(x.ValorCentavos), Alocado = Money(x.AlocadoCentavos), Crédito = Money(x.CreditoCentavos), Situação = x.EstornadoEm is null ? "Válido" : "Estornado", x.MotivoEstorno }).ToList();
    }

    private void NovoCliente() { var nome = Microsoft.VisualBasic.Interaction.InputBox("Nome do cliente:", "Novo Cliente"); if (!string.IsNullOrWhiteSpace(nome)) Run(() => { _clientes.Cadastrar(nome); AtualizarClientes(); AtualizarCombos(); }, "Cliente cadastrado."); }
    private void AlternarCliente() { if (_clientesGrid.CurrentRow is null) return; var id = CellId(_clientesGrid); var ativo = _clientesGrid.CurrentRow.Cells["Situação"].Value?.ToString() == "Ativo"; Run(() => { _clientes.DefinirAtivo(id, !ativo); AtualizarClientes(); AtualizarCombos(); }, ativo ? "Cliente inativado." : "Cliente reativado."); }
    private void CalcularTotal() { try { _total.Text = Money(_vendas.CalcularTotal(Grams(_kg.Value), Cents(_preco.Value))); } catch { _total.Text = "R$ 0,00"; } }
    private void RegistrarVenda() => Run(() => { _vendas.Registrar(SelectedId(_vendaCliente), Grams(_kg.Value), Cents(_preco.Value), DateOnly.FromDateTime(_dataVenda.Value), Cents(_inicial.Value)); _inicial.Value = 0; AtualizarTudo(); }, "Venda registrada.");

    private void CarregarAlocacoes()
    {
        var id = SelectedId(_pagamentoCliente); _linhas = id == 0 ? [] : _vendas.Listar(id, somentePendentes: true).Select(v => new AlocacaoLinha(v)).ToList();
        _alocacoesGrid.DataSource = _linhas;
        foreach (DataGridViewColumn c in _alocacoesGrid.Columns) c.ReadOnly = c.Name != nameof(AlocacaoLinha.Alocar);
        if (_alocacoesGrid.Columns[nameof(AlocacaoLinha.VendaId)] is { } vendaColumn)
            vendaColumn.HeaderText = "Venda";
        Hide(_alocacoesGrid, nameof(AlocacaoLinha.SaldoCentavos), nameof(AlocacaoLinha.AlocarCentavos));
        if (_alocacoesGrid.Columns[nameof(AlocacaoLinha.Alocar)] is { } alocarColumn)
        {
            alocarColumn.HeaderText = "Alocar (R$)";
            alocarColumn.DefaultCellStyle.Format = "N2";
        }
        AtualizarCredito();
    }
    private void Distribuir() { var restante = Cents(_recebido.Value); foreach (var l in _linhas.OrderBy(x => x.Data)) { l.AlocarCentavos = Math.Min(restante, l.SaldoCentavos); restante -= l.AlocarCentavos; } _alocacoesGrid.Refresh(); AtualizarCredito(); }
    private void AtualizarCredito() { _alocacoesGrid.EndEdit(); _credito.Text = $"Crédito: {Money(Cents(_recebido.Value) - _linhas.Sum(x => x.AlocarCentavos))}"; }
    private void RegistrarPagamento()
    {
        _alocacoesGrid.EndEdit(); var valor = Cents(_recebido.Value); var items = _linhas.Where(x => x.AlocarCentavos > 0).Select(x => new AlocacaoSolicitada(x.VendaId, x.AlocarCentavos)).ToList(); var alocado = items.Sum(x => x.ValorCentavos);
        if (MessageBox.Show($"Recebido: {Money(valor)}\nDistribuído: {Money(alocado)}\nCrédito: {Money(valor - alocado)}\n\nConfirmar?", "Pagamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Run(() => { _pagamentos.Registrar(SelectedId(_pagamentoCliente), valor, DateOnly.FromDateTime(_dataPagamento.Value), items); _recebido.Value = 0; AtualizarTudo(); }, "Pagamento registrado.");
    }
    private void PagarSelecionada() { if (_pendenciasGrid.CurrentRow is null) return; _pagamentoCliente.SelectedValue = Convert.ToInt64(_pendenciasGrid.CurrentRow.Cells["ClienteId"].Value); _tabs.SelectedIndex = 3; }
    private void CancelarVenda() { if (_vendasGrid.CurrentRow is null) return; var motivo = Microsoft.VisualBasic.Interaction.InputBox("Motivo do cancelamento:", "Cancelar Venda"); if (!string.IsNullOrWhiteSpace(motivo)) Run(() => { _vendas.Cancelar(CellId(_vendasGrid), motivo); AtualizarTudo(); }, "Venda cancelada."); }
    private void EstornarPagamento() { if (_pagamentosGrid.CurrentRow is null) return; var motivo = Microsoft.VisualBasic.Interaction.InputBox("Motivo do estorno:", "Estornar Pagamento"); if (string.IsNullOrWhiteSpace(motivo) || MessageBox.Show("As vendas afetadas voltarão a ter saldo. Confirmar?", "Estorno", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; Run(() => { _pagamentos.Estornar(CellId(_pagamentosGrid), motivo); AtualizarTudo(); }, "Pagamento estornado."); }

    private static void Run(Action action, string success) { try { action(); MessageBox.Show(success, "Mandioca Caipira", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { Log.Warning(ex, "Operação solicitada pelo usuário não pôde ser concluída"); MessageBox.Show(ex.Message, "Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
    private static void Fill(ComboBox combo, IReadOnlyList<Cliente> source, bool all) { var old = SelectedId(combo); var list = source.Select(x => new Item(x.Id, x.Ativo ? x.Nome : $"{x.Nome} (inativo)")).ToList(); if (all) list.Insert(0, new Item(0, "Todos")); combo.DataSource = list; combo.DisplayMember = nameof(Item.Nome); combo.ValueMember = nameof(Item.Id); if (old > 0) combo.SelectedValue = old; }
    private static long SelectedId(ComboBox combo) => combo.SelectedValue is long id ? id : 0;
    private static long CellId(DataGridView grid) => Convert.ToInt64(grid.CurrentRow!.Cells["Id"].Value);
    private static void Hide(DataGridView grid, params string[] columns) { foreach (var name in columns) if (grid.Columns[name] is { } column) column.Visible = false; }
    private static long Grams(decimal kg) => checked((long)decimal.Round(kg * 1000m, 0, MidpointRounding.AwayFromZero));
    private static long Cents(decimal value) => checked((long)decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero));
    private static string Money(long cents) => (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    private static DataGridView Grid(bool readOnly = true) => new() { Dock = DockStyle.Fill, ReadOnly = readOnly, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false, BackgroundColor = Color.White };
    private static ComboBox Combo() => new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private static NumericUpDown Numero(int decimals) => new() { DecimalPlaces = decimals, Minimum = decimals == 3 ? 0.001m : 0, Maximum = 100000000, ThousandsSeparator = true, Width = 130 };
    private static DateTimePicker Data() => new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private static Label Label(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(6, 8, 0, 0) };
    private static Label Valor(string text) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", 15, FontStyle.Bold), Padding = new Padding(4) };
    private static Label Title(string text) => new() { Text = text, Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private static Button Button(string text, Action action) { var b = new Button { Text = text, AutoSize = true, Height = 34, Padding = new Padding(8, 0, 8, 0) }; b.Click += (_, _) => action(); return b; }
    private static FlowLayoutPanel Bar(int height = 55) => new() { Dock = DockStyle.Top, Height = height, Padding = new Padding(5), WrapContents = true };
    private static TabPage Page(string title) => new(title) { Padding = new Padding(10), BackColor = Color.White };
    private static TableLayoutPanel FormPanel() => new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(30), ColumnCount = 2, RowCount = 7 };
    private static void Field(TableLayoutPanel f, string name, Control c, int row) { f.Controls.Add(Label(name), 0, row); f.Controls.Add(c, 1, row); }
    private static Control Card(string name, Control value) { var p = new Panel { Width = 220, Height = 90, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(8) }; p.Controls.Add(Label(name)); p.Controls.Add(value); value.Top = 30; return p; }
    private sealed record Item(long Id, string Nome);
    private sealed class AlocacaoLinha(VendaResumo venda)
    {
        public long VendaId { get; } = venda.Id; public DateOnly Data { get; } = venda.DataOperacao;
        public string Total { get; } = Money(venda.TotalCentavos); public string Recebido { get; } = Money(venda.RecebidoCentavos); public string Saldo { get; } = Money(venda.SaldoCentavos);
        public long SaldoCentavos { get; } = venda.SaldoCentavos; public long AlocarCentavos { get; set; }
        public decimal Alocar { get => AlocarCentavos / 100m; set => AlocarCentavos = Cents(value); }
    }
}
