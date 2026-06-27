namespace Control_OS_Lunix.UI.Views;

internal static class ModernUi
{
    public static readonly Color AppBackground = Color.FromArgb(242, 245, 250);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color SurfaceMuted = Color.FromArgb(248, 250, 252);
    public static readonly Color Primary = Color.FromArgb(23, 92, 211);
    public static readonly Color PrimaryHover = Color.FromArgb(18, 73, 170);
    public static readonly Color Success = Color.FromArgb(24, 128, 84);
    public static readonly Color Danger = Color.FromArgb(196, 51, 73);
    public static readonly Color Warning = Color.FromArgb(180, 98, 21);
    public static readonly Color Border = Color.FromArgb(223, 228, 236);
    public static readonly Color TextStrong = Color.FromArgb(24, 34, 48);
    public static readonly Color TextMuted = Color.FromArgb(92, 103, 122);

    public static Font TitleFont(float size = 20f)
    {
        return new Font("Segoe UI Semibold", size, FontStyle.Bold);
    }

    public static Font SectionFont(float size = 12f)
    {
        return new Font("Segoe UI Semibold", size, FontStyle.Bold);
    }

    public static Font BodyFont(float size = 9.5f)
    {
        return new Font("Segoe UI", size, FontStyle.Regular);
    }

    public static Panel CreateCard(int padding = 18)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            Padding = new Padding(padding),
            Margin = new Padding(0)
        };
    }

    public static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = SectionFont(),
            ForeColor = TextStrong,
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    public static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = TextMuted,
            Margin = new Padding(0, 6, 8, 0)
        };
    }

    public static void StyleInput(Control control)
    {
        control.Font = BodyFont();
        control.BackColor = Color.White;
        control.Margin = new Padding(0, 4, 0, 6);

        switch (control)
        {
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numericUpDown:
                numericUpDown.BorderStyle = BorderStyle.FixedSingle;
                break;
        }
    }

    public static Button CreateButton(string text, bool primary = false, Color? accentColor = null)
    {
        Color baseColor = accentColor ?? (primary ? Primary : SurfaceMuted);
        Color textColor = primary || accentColor.HasValue ? Color.White : TextStrong;

        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = baseColor,
            ForeColor = textColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 8),
            Padding = new Padding(16, 9, 16, 9),
            MinimumSize = new Size(0, 38)
        };

        button.FlatAppearance.BorderColor = primary || accentColor.HasValue ? baseColor : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? PrimaryHover : Color.FromArgb(238, 242, 247);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(15, 60, 138) : Color.FromArgb(230, 235, 242);
        return button;
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = true;
        checkBox.ForeColor = TextStrong;
        checkBox.Font = BodyFont();
        checkBox.Margin = new Padding(0, 4, 0, 4);
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 251);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextStrong;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 42;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = TextStrong;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(229, 238, 255);
        grid.DefaultCellStyle.SelectionForeColor = TextStrong;
        grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
        grid.RowTemplate.Height = 40;
    }
}
