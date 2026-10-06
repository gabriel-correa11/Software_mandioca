using Krypton.Toolkit;

namespace MandiocaCaipira.App;

internal static class Ui
{
    public const int SpaceSmall = 4;
    public const int Space = 8;
    public const int SpaceLarge = 16;
    public static readonly Font Font = new("Segoe UI", 9.5F);
    public static readonly Font TitleFont = new("Segoe UI Semibold", 15F);
    public static readonly Font SectionFont = new("Segoe UI Semibold", 10.5F);
    public static readonly Font ButtonFont = new("Segoe UI", 9F);
    public static readonly Font GridFont = new("Segoe UI", 9F);
    public static readonly Color Background = Color.FromArgb(244, 246, 244);
    public static readonly Color Nav = Color.FromArgb(38, 55, 45);
    public static readonly Color NavSelected = Color.FromArgb(57, 88, 68);
    public static readonly Color Primary = Color.FromArgb(48, 104, 67);
    public static readonly Color Text = Color.FromArgb(38, 43, 40);
    public static readonly Color Muted = Color.FromArgb(98, 106, 101);
    public static readonly Color Border = Color.FromArgb(214, 219, 216);
    public static readonly Color LightGreen = Color.FromArgb(235, 244, 238);
    public static readonly Color Danger = Color.FromArgb(158, 54, 48);

    public static TabPage Page(string title) => new(title) { BackColor = SystemColors.Control, Padding = new Padding(4) };
    public static Label Caption(string text) => new() { Text = text, AutoSize = true, ForeColor = Text, Padding = new Padding(2, 7, 2, 0) };
    public static Label Value(string text, float size = 17) => new() { Text = text, AutoSize = false, Height = 36, Font = new Font("Segoe UI Semibold", size), ForeColor = Text, TextAlign = ContentAlignment.MiddleLeft };
    public static ComboBox Combo() => new() { Width = 300, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, DropDownHeight = 240, Font = Font };
    public static DateTimePicker Date() => new() { Width = 155, Height = 28, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Font };
    public static NumericUpDown Number(int decimals) => new() { DecimalPlaces = decimals, Minimum = decimals == 3 ? .001m : 0, Maximum = 100_000_000m, ThousandsSeparator = true, Width = 180, Height = 28, TextAlign = HorizontalAlignment.Right, Increment = decimals == 3 ? .100m : 1m, Font = Font };
    public static FlowLayoutPanel Toolbar(int height = 48) => new() { Dock = DockStyle.Top, Height = height, BackColor = SystemColors.Control, Padding = new Padding(6), WrapContents = false };
    public static KryptonButton PrimaryButton(string text, Action action) => Button(text, true, false, action);
    public static KryptonButton SecondaryButton(string text, Action action) => Button(text, false, false, action);
    public static KryptonButton DangerButton(string text, Action action) => Button(text, false, true, action);
    private static KryptonButton Button(string text, bool primary, bool danger, Action action)
    {
        var b = new KryptonButton { Text = text, AutoSize = true };
        StyleButton(b, primary, danger);
        b.Click += (_, _) => action(); return b;
    }
    public static void StyleButton(KryptonButton button, bool primary = false, bool danger = false)
    {
        button.AutoSize = true; button.Height = 32; button.MinimumSize = new Size(96, 32); button.Margin = new Padding(0, 0, 8, 0);
        button.StateCommon.Content.ShortText.Font = ButtonFont;
        if (primary) { button.StateCommon.Back.Color1 = Primary; button.StateCommon.Back.Color2 = Primary; button.StateCommon.Content.ShortText.Color1 = Color.White; button.StateCommon.Border.Color1 = Color.FromArgb(36, 82, 52); }
        else button.StateCommon.Content.ShortText.Color1 = danger ? Danger : Text;
    }
    public static Panel Summary(string label, Control value)
    {
        var p = new Panel { Width = 190, Height = 82, BackColor = SystemColors.Window, Margin = new Padding(0, 0, 8, 8), Padding = new Padding(10, 8, 8, 6), BorderStyle = BorderStyle.FixedSingle };
        var l = new Label { Text = label, Dock = DockStyle.Top, Height = 23, ForeColor = Muted, Font = Font }; value.Dock = DockStyle.Fill; p.Controls.Add(value); p.Controls.Add(l); return p;
    }
    public static Panel Section(string title, string subtitle, int height = 390)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = height, BackColor = SystemColors.Control, Padding = new Padding(14, 60, 14, 12), BorderStyle = BorderStyle.FixedSingle };
        p.Controls.Add(new Label { Text = title, Location = new Point(14, 10), AutoSize = true, Font = SectionFont, ForeColor = Text });
        p.Controls.Add(new Label { Text = subtitle, Location = new Point(15, 34), AutoSize = true, ForeColor = Muted });
        return p;
    }
    public static Control SectionHeader(string title, params Control[] actions)
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = SystemColors.Control };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Right, Width = Math.Max(280, actions.Sum(a => a.PreferredSize.Width + 10)), FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 3, 0, 2) };
        foreach (var c in actions) { c.Height = 30; flow.Controls.Add(c); }
        bar.Controls.Add(flow); bar.Controls.Add(new Label { Text = title, Dock = DockStyle.Left, Width = 180, Font = SectionFont, Padding = new Padding(6, 9, 0, 0) }); return bar;
    }
    public static TableLayoutPanel Form(int rows)
    {
        var f = new TableLayoutPanel { Dock = DockStyle.Left, Width = 610, ColumnCount = 2, RowCount = rows, Padding = new Padding(5) };
        f.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); f.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++) f.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); return f;
    }
    public static void Field(TableLayoutPanel form, string label, Control control, int row) { var l = Caption(label); l.Anchor = AnchorStyles.Left; control.Anchor = AnchorStyles.Left | AnchorStyles.Right; form.Controls.Add(l, 0, row); form.Controls.Add(control, 1, row); }
    public static Panel Labeled(string label, Control control)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 8), Padding = new Padding(0, 22, 0, 0) };
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 22, ForeColor = Text, Font = Font });
        return panel;
    }

    public static FlowLayoutPanel ActionBar(params Control[] controls)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(6, 8, 0, 6), BackColor = SystemColors.Control };
        foreach (var control in controls) bar.Controls.Add(control);
        return bar;
    }
    public static DataGridView Grid(bool readOnly = true) => new()
    {
        Dock = DockStyle.Fill, ReadOnly = readOnly, AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.Fixed3D, GridColor = Color.FromArgb(205, 210, 207), RowTemplate = { Height = 27 }, ColumnHeadersHeight = 31,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(226, 230, 227), ForeColor = Text, Font = new Font("Segoe UI Semibold", 9F), SelectionBackColor = Color.FromArgb(226, 230, 227), Padding = new Padding(3, 0, 3, 0) },
        DefaultCellStyle = new DataGridViewCellStyle { BackColor = SystemColors.Window, ForeColor = Text, Font = GridFont, SelectionBackColor = Color.FromArgb(73, 116, 86), SelectionForeColor = Color.White, Padding = new Padding(3, 0, 3, 0) },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(249, 250, 249) }
    };
    public static void Columns(DataGridView grid, params (string Property, string Header, int Weight, DataGridViewContentAlignment Align)[] columns)
    {
        foreach (var c in columns) grid.Columns.Add(new DataGridViewTextBoxColumn { Name = c.Property, DataPropertyName = c.Property, HeaderText = c.Header, FillWeight = c.Weight, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = c.Align } });
    }
}
