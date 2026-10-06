namespace MandiocaCaipira.App;

internal static class Ui
{
    public static readonly Font Font = new("Segoe UI", 10);
    public static readonly Color Background = Color.FromArgb(244, 246, 244);
    public static readonly Color Nav = Color.FromArgb(38, 55, 45);
    public static readonly Color NavSelected = Color.FromArgb(57, 88, 68);
    public static readonly Color Primary = Color.FromArgb(48, 104, 67);
    public static readonly Color Text = Color.FromArgb(38, 43, 40);
    public static readonly Color Muted = Color.FromArgb(98, 106, 101);
    public static readonly Color Border = Color.FromArgb(214, 219, 216);
    public static readonly Color LightGreen = Color.FromArgb(235, 244, 238);
    public static readonly Color Danger = Color.FromArgb(158, 54, 48);

    public static TabPage Page(string title) => new(title) { BackColor = Background, Padding = new Padding(4) };
    public static Label Caption(string text) => new() { Text = text, AutoSize = true, ForeColor = Text, Padding = new Padding(4, 8, 2, 0) };
    public static Label Value(string text, float size = 18) => new() { Text = text, AutoSize = false, Height = 38, Font = new Font("Segoe UI Semibold", size), ForeColor = Text, TextAlign = ContentAlignment.MiddleLeft };
    public static ComboBox Combo() => new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, DropDownHeight = 240 };
    public static DateTimePicker Date() => new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    public static NumericUpDown Number(int decimals) => new() { DecimalPlaces = decimals, Minimum = decimals == 3 ? .001m : 0, Maximum = 100_000_000m, ThousandsSeparator = true, Width = 180, TextAlign = HorizontalAlignment.Right, Increment = decimals == 3 ? .100m : 1m };
    public static FlowLayoutPanel Toolbar(int height = 58) => new() { Dock = DockStyle.Top, Height = height, BackColor = Color.White, Padding = new Padding(10, 10, 8, 7), WrapContents = false };
    public static Button PrimaryButton(string text, Action action) => Button(text, Primary, Color.White, action);
    public static Button SecondaryButton(string text, Action action) => Button(text, Color.White, Text, action, Border);
    public static Button DangerButton(string text, Action action) => Button(text, Color.White, Danger, action, Color.FromArgb(220, 185, 182));
    private static Button Button(string text, Color back, Color fore, Action action, Color? border = null)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 36, MinimumSize = new Size(100, 36), Padding = new Padding(12, 0, 12, 0), FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = border ?? back; b.FlatAppearance.MouseOverBackColor = back == Color.White ? Background : Color.FromArgb(39, 88, 56); b.Click += (_, _) => action(); return b;
    }
    public static Button NavButton(string text, Action action)
    {
        var b = new Button { Text = text, Width = 180, Height = 45, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), BackColor = Nav, ForeColor = Color.White, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0; b.Click += (_, _) => action(); return b;
    }
    public static Panel Summary(string label, Control value)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(15, 12, 12, 8), BorderStyle = BorderStyle.FixedSingle };
        var l = new Label { Text = label, Dock = DockStyle.Top, Height = 25, ForeColor = Muted }; value.Dock = DockStyle.Fill; p.Controls.Add(value); p.Controls.Add(l); return p;
    }
    public static Panel InlineMetric(string label, Control value)
    {
        var p = new Panel { Width = 330, Height = 62 }; var l = new Label { Text = label, Dock = DockStyle.Top, Height = 20, ForeColor = Muted }; value.Dock = DockStyle.Fill; p.Controls.Add(value); p.Controls.Add(l); return p;
    }
    public static Panel Section(string title, string subtitle, int height = 390)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.White, Padding = new Padding(20, 68, 20, 18), BorderStyle = BorderStyle.FixedSingle };
        p.Controls.Add(new Label { Text = title, Location = new Point(20, 12), AutoSize = true, Font = new Font("Segoe UI Semibold", 13), ForeColor = Text });
        p.Controls.Add(new Label { Text = subtitle, Location = new Point(21, 40), AutoSize = true, ForeColor = Muted });
        return p;
    }
    public static Control SectionHeader(string title, params Control[] actions)
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 350, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        foreach (var c in actions) { c.Height = 30; flow.Controls.Add(c); }
        bar.Controls.Add(flow); bar.Controls.Add(new Label { Text = title, Dock = DockStyle.Left, Width = 180, Font = new Font("Segoe UI Semibold", 11), Padding = new Padding(8, 8, 0, 0) }); return bar;
    }
    public static TableLayoutPanel Form(int rows)
    {
        var f = new TableLayoutPanel { Dock = DockStyle.Left, Width = 610, ColumnCount = 2, RowCount = rows, Padding = new Padding(5) };
        f.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); f.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++) f.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); return f;
    }
    public static void Field(TableLayoutPanel form, string label, Control control, int row) { var l = Caption(label); l.Anchor = AnchorStyles.Left; control.Anchor = AnchorStyles.Left | AnchorStyles.Right; form.Controls.Add(l, 0, row); form.Controls.Add(control, 1, row); }
    public static DataGridView Grid(bool readOnly = true) => new()
    {
        Dock = DockStyle.Fill, ReadOnly = readOnly, AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, GridColor = Border, RowTemplate = { Height = 34 }, ColumnHeadersHeight = 40,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(236, 239, 237), ForeColor = Text, Font = new Font("Segoe UI Semibold", 9.5f), SelectionBackColor = Color.FromArgb(236, 239, 237) },
        DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Text, SelectionBackColor = Color.FromArgb(220, 235, 225), SelectionForeColor = Text, Padding = new Padding(5, 0, 5, 0) },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(249, 250, 249) }
    };
    public static void Columns(DataGridView grid, params (string Property, string Header, int Weight, DataGridViewContentAlignment Align)[] columns)
    {
        foreach (var c in columns) grid.Columns.Add(new DataGridViewTextBoxColumn { Name = c.Property, DataPropertyName = c.Property, HeaderText = c.Header, FillWeight = c.Weight, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = c.Align } });
    }
}
