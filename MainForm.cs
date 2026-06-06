namespace CleanYourSetup;

internal sealed class MainForm : Form
{
    private ComboBox _durationBox = null!;
    private NumericUpDown _customMinutes = null!;
    private NumericUpDown _customSeconds = null!;
    private ComboBox _modeBox = null!;
    private CheckBox _screenOverlayCheck = null!;
    private CheckBox _beepCheck = null!;
    private Label _statusLabel = null!;

    public MainForm()
    {
        Text = "Clean Your Setup";
        Width = 640;
        Height = 620;
        MinimumSize = new Size(640, 620);
        MaximumSize = new Size(640, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BrandColors.Black;
        ForeColor = BrandColors.OffWhite;
        Icon = AppIcon.Load();
        Font = CreateFont(10f, FontStyle.Regular);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        BuildUi();
        UpdateCustomTimeVisibility();
    }

    public void RestoreFromExternalLaunch()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Show();
        Activate();
    }

    private void BuildUi()
    {
        BuildBrand();

        Label title = CreateLabel("Clean Your Setup", 34, 78, 500, 42, 24f, FontStyle.Bold, BrandColors.OffWhite);
        Label subtitle = CreateLabel("Block input while you clean. Minimal, timed, safe.", 36, 120, 520, 28, 10.5f, FontStyle.Regular, BrandColors.MutedText);

        Panel card = CreateCard(32, 170, 560, 310);

        Label durationLabel = CreateCardLabel("Duration", 28, 26, 160, true, card);
        _durationBox = CreateComboBox(28, 57, 210);
        _durationBox.Items.AddRange(new object[]
        {
            new DurationOption("30 seconds", 30, false),
            new DurationOption("1 minute", 60, false),
            new DurationOption("2 minutes", 120, false),
            new DurationOption("5 minutes", 300, false),
            new DurationOption("Custom", 0, true)
        });
        _durationBox.SelectedIndex = 1;
        _durationBox.SelectedIndexChanged += (_, _) => UpdateCustomTimeVisibility();

        Label customLabel = CreateCardLabel("Custom", 284, 26, 140, false, card);
        _customMinutes = CreateNumberBox(284, 57, 74, 0, 60, 1);
        _customSeconds = CreateNumberBox(398, 57, 74, 0, 59, 0);
        Label minLabel = CreateSmallCardLabel("min", 362, 62, 38, card);
        Label secLabel = CreateSmallCardLabel("sec", 476, 62, 38, card);

        Label modeLabel = CreateCardLabel("Block", 28, 122, 160, true, card);
        _modeBox = CreateComboBox(28, 153, 210);
        _modeBox.Items.AddRange(new object[]
        {
            new InputModeOption("Keyboard + mouse", InputBlockMode.KeyboardAndMouse),
            new InputModeOption("Keyboard only", InputBlockMode.KeyboardOnly),
            new InputModeOption("Mouse only", InputBlockMode.MouseOnly),
            new InputModeOption("Screen overlay only", InputBlockMode.OverlayOnly)
        });
        _modeBox.SelectedIndex = 0;
        _modeBox.SelectedIndexChanged += (_, _) =>
        {
            if (GetSelectedMode() == InputBlockMode.OverlayOnly)
            {
                _screenOverlayCheck.Checked = true;
            }
        };

        _screenOverlayCheck = CreateCheckBox("Screen overlay on all monitors", 284, 151, 250, false, card);
        _beepCheck = CreateCheckBox("Beep during last 5 seconds", 284, 186, 250, true, card);

        Label safety = CreateSmallCardLabel("Emergency unlock: spam Esc, Space, or Enter 8 times.", 28, 226, 480, card);
        safety.ForeColor = BrandColors.MutedText;

        Button startButton = CreateButton("Start Cleaning Mode", 28, 258, 500, 42, BrandColors.Crimson);
        startButton.Click += (_, _) => StartCleaning();

        card.Controls.Add(durationLabel);
        card.Controls.Add(_durationBox);
        card.Controls.Add(customLabel);
        card.Controls.Add(_customMinutes);
        card.Controls.Add(_customSeconds);
        card.Controls.Add(minLabel);
        card.Controls.Add(secLabel);
        card.Controls.Add(modeLabel);
        card.Controls.Add(_modeBox);
        card.Controls.Add(_screenOverlayCheck);
        card.Controls.Add(_beepCheck);
        card.Controls.Add(safety);
        card.Controls.Add(startButton);

        _statusLabel = CreateLabel("Ready.", 36, 500, 540, 46, 9.5f, FontStyle.Regular, BrandColors.MutedText);
        _statusLabel.TextAlign = ContentAlignment.TopLeft;

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(card);
        Controls.Add(_statusLabel);
    }

    private void BuildBrand()
    {
        Controls.Add(new BrandMarkControl
        {
            Left = 36,
            Top = 28
        });
    }

    private void StartCleaning()
    {
        int seconds = GetSelectedDurationSeconds();
        if (seconds < 5)
        {
            MessageBox.Show("Minimum cleaning time is 5 seconds.", "Clean Your Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (seconds > 3600)
        {
            MessageBox.Show("Maximum cleaning time is 60 minutes.", "Clean Your Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        InputBlockMode mode = GetSelectedMode();
        bool overlayEnabled = _screenOverlayCheck.Checked || mode == InputBlockMode.OverlayOnly;

        CleaningSessionOptions options = new()
        {
            DurationSeconds = seconds,
            Mode = mode,
            ScreenOverlayEnabled = overlayEnabled,
            BeepLastFiveSeconds = _beepCheck.Checked,
            UnlockTapCount = 8
        };

        using CleaningOverlayForm overlay = new(options);
        Hide();
        DialogResult result = overlay.ShowDialog();
        Show();
        Activate();

        string endText = overlay.EndReasonText;
        _statusLabel.Text = string.IsNullOrWhiteSpace(endText) ? "Done." : endText;
    }

    private int GetSelectedDurationSeconds()
    {
        if (_durationBox.SelectedItem is not DurationOption option)
        {
            return 60;
        }

        if (!option.IsCustom)
        {
            return option.Seconds;
        }

        return ((int)_customMinutes.Value * 60) + (int)_customSeconds.Value;
    }

    private InputBlockMode GetSelectedMode()
    {
        if (_modeBox.SelectedItem is InputModeOption option)
        {
            return option.Mode;
        }

        return InputBlockMode.KeyboardAndMouse;
    }

    private void UpdateCustomTimeVisibility()
    {
        bool isCustom = _durationBox.SelectedItem is DurationOption option && option.IsCustom;
        _customMinutes.Enabled = isCustom;
        _customSeconds.Enabled = isCustom;
    }

    private Panel CreateCard(int left, int top, int width, int height)
    {
        Panel panel = new()
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = BrandColors.Black
        };

        panel.Paint += (_, e) =>
        {
            using Pen pen = new(BrandColors.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        return panel;
    }

    private Label CreateLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = BrandColors.Black,
            ForeColor = color,
            Font = CreateFont(size, style)
        };
    }

    private Label CreateCardLabel(string text, int left, int top, int width, bool bold, Control parent)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            BackColor = parent.BackColor,
            ForeColor = bold ? BrandColors.OffWhite : BrandColors.MutedText,
            Font = CreateFont(10f, bold ? FontStyle.Bold : FontStyle.Regular)
        };
    }

    private Label CreateSmallCardLabel(string text, int left, int top, int width, Control parent)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 26,
            BackColor = parent.BackColor,
            ForeColor = BrandColors.MutedText,
            Font = CreateFont(9f, FontStyle.Regular)
        };
    }

    private ComboBox CreateComboBox(int left, int top, int width)
    {
        return new ComboBox
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 34,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = BrandColors.Black,
            ForeColor = BrandColors.OffWhite,
            FlatStyle = FlatStyle.Flat,
            Font = CreateFont(10f, FontStyle.Regular)
        };
    }

    private NumericUpDown CreateNumberBox(int left, int top, int width, int minimum, int maximum, int value)
    {
        return new NumericUpDown
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 34,
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            BackColor = BrandColors.Black,
            ForeColor = BrandColors.OffWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Font = CreateFont(10f, FontStyle.Regular)
        };
    }

    private CheckBox CreateCheckBox(string text, int left, int top, int width, bool isChecked, Control parent)
    {
        return new CheckBox
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 26,
            Checked = isChecked,
            BackColor = parent.BackColor,
            ForeColor = BrandColors.OffWhite,
            Font = CreateFont(9.5f, FontStyle.Regular)
        };
    }

    private Button CreateButton(string text, int left, int top, int width, int height, Color backColor)
    {
        Button button = new()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = backColor,
            ForeColor = BrandColors.OffWhite,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = CreateFont(10.5f, FontStyle.Bold)
        };

        button.FlatAppearance.BorderColor = backColor == BrandColors.Crimson ? BrandColors.Crimson : BrandColors.Border;
        button.FlatAppearance.MouseOverBackColor = backColor == BrandColors.Crimson ? ColorTranslator.FromHtml("#5F1113") : BrandColors.Border;
        button.FlatAppearance.MouseDownBackColor = BrandColors.Black;

        return button;
    }

    private static Font CreateFont(float size, FontStyle style)
    {
        try
        {
            return new Font("Inter", size, style);
        }
        catch
        {
            return new Font("Segoe UI", size, style);
        }
    }

    private sealed record DurationOption(string Text, int Seconds, bool IsCustom)
    {
        public override string ToString() => Text;
    }

    private sealed record InputModeOption(string Text, InputBlockMode Mode)
    {
        public override string ToString() => Text;
    }
}
