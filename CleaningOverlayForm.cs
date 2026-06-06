namespace CleanYourSetup;

internal sealed class CleaningOverlayForm : Form
{
    private enum Phase
    {
        Countdown,
        Running,
        Finished
    }

    private readonly CleaningSessionOptions _options;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<ScreenShieldForm> _secondaryOverlays = new();
    private readonly HashSet<Keys> _overlayPressedEmergencyKeys = new();
    private readonly InputBlocker _inputBlocker;

    private readonly Panel _centerPanel;
    private readonly Label _stateLabel;
    private readonly Label _timeLabel;
    private readonly Label _modeLabel;
    private readonly Label _unlockLabel;
    private readonly Panel _progressTrack;
    private readonly Panel _progressFill;
    private readonly Button _cancelButton;

    private Phase _phase = Phase.Countdown;
    private int _countdown = 3;
    private int _remainingSeconds;
    private int _overlayEmergencyPresses;
    private DateTimeOffset _lastOverlayEmergencyPressUtc = DateTimeOffset.MinValue;
    private bool _finished;

    public CleaningOverlayForm(CleaningSessionOptions options)
    {
        _options = options;
        _remainingSeconds = Math.Clamp(options.DurationSeconds, 5, 3600);
        _inputBlocker = new InputBlocker(options.UnlockTapCount);
        _inputBlocker.EmergencyUnlockProgressChanged += InputBlocker_EmergencyUnlockProgressChanged;
        _inputBlocker.EmergencyUnlockRequested += (_, _) => SafeFinish("Emergency unlock.");

        Text = "Clean Your Setup";
        FormBorderStyle = options.ScreenOverlayEnabled ? FormBorderStyle.None : FormBorderStyle.FixedSingle;
        StartPosition = options.ScreenOverlayEnabled ? FormStartPosition.Manual : FormStartPosition.CenterScreen;
        BackColor = BrandColors.Black;
        ForeColor = BrandColors.OffWhite;
        TopMost = true;
        ShowInTaskbar = !options.ScreenOverlayEnabled;
        Icon = AppIcon.Load();
        Font = CreateFont(11f, FontStyle.Regular);
        KeyPreview = true;
        MaximizeBox = false;
        MinimizeBox = false;

        if (options.ScreenOverlayEnabled)
        {
            Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        }
        else
        {
            Width = 680;
            Height = 520;
            MinimumSize = new Size(680, 520);
            MaximumSize = new Size(680, 520);
        }

        BrandMarkControl brandMark = new()
        {
            Left = 36,
            Top = 28
        };

        _centerPanel = new Panel
        {
            Width = options.ScreenOverlayEnabled ? 760 : 600,
            Height = 350,
            BackColor = BrandColors.Black
        };
        _centerPanel.Paint += (_, e) =>
        {
            using Pen pen = new(BrandColors.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _centerPanel.Width - 1, _centerPanel.Height - 1);
        };

        int panelWidth = _centerPanel.Width;
        int contentLeft = 36;
        int contentWidth = panelWidth - 72;

        _stateLabel = CreatePanelLabel("Starting in", contentLeft, 34, contentWidth, 34, 14f, FontStyle.Bold, BrandColors.MutedText);
        _timeLabel = CreatePanelLabel("3", contentLeft, 72, contentWidth, 92, 52f, FontStyle.Bold, BrandColors.OffWhite);
        _modeLabel = CreatePanelLabel(FormatMode(options.Mode), contentLeft, 168, contentWidth, 28, 10f, FontStyle.Regular, BrandColors.MutedText);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Left = (panelWidth - 120) / 2,
            Top = 210,
            Width = 120,
            Height = 36,
            BackColor = BrandColors.Black,
            ForeColor = BrandColors.OffWhite,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = CreateFont(9.5f, FontStyle.Bold)
        };
        _cancelButton.FlatAppearance.BorderColor = BrandColors.Border;
        _cancelButton.FlatAppearance.MouseOverBackColor = BrandColors.Surface;
        _cancelButton.Click += (_, _) => Finish("Canceled before lock.");

        _unlockLabel = CreatePanelLabel($"Spam Esc / Space / Enter {_options.UnlockTapCount} times to unlock early.", contentLeft, 260, contentWidth, 28, 9.5f, FontStyle.Regular, BrandColors.MutedText);

        _progressTrack = new Panel
        {
            Left = (panelWidth - 380) / 2,
            Top = 300,
            Width = 380,
            Height = 10,
            BackColor = BrandColors.Border
        };

        _progressFill = new Panel
        {
            Left = 0,
            Top = 0,
            Width = 0,
            Height = 10,
            BackColor = BrandColors.Crimson
        };
        _progressTrack.Controls.Add(_progressFill);

        _centerPanel.Controls.Add(_stateLabel);
        _centerPanel.Controls.Add(_timeLabel);
        _centerPanel.Controls.Add(_modeLabel);
        _centerPanel.Controls.Add(_cancelButton);
        _centerPanel.Controls.Add(_unlockLabel);
        _centerPanel.Controls.Add(_progressTrack);

        Controls.Add(brandMark);
        Controls.Add(_centerPanel);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += Timer_Tick;

        KeyDown += Overlay_KeyDown;
        KeyUp += Overlay_KeyUp;
        Resize += (_, _) => LayoutOverlay();
        LayoutOverlay();
    }

    public string EndReasonText { get; private set; } = string.Empty;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ShowSecondaryOverlays();
        Activate();
        UpdateCountdownUi();
        _timer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Cleanup();
        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_options.ScreenOverlayEnabled)
        {
            using Pen pen = new(BrandColors.Crimson, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_finished)
        {
            return;
        }

        if (_phase == Phase.Countdown)
        {
            _countdown--;

            if (_countdown > 0)
            {
                UpdateCountdownUi();
                return;
            }

            StartRunning();
            return;
        }

        if (_phase == Phase.Running)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                Finish("Done. Input enabled.");
                return;
            }

            UpdateRunningUi();
            MaybeBeep();
        }
    }

    private void StartRunning()
    {
        try
        {
            _phase = Phase.Running;
            _cancelButton.Visible = false;
            SleepManager.PreventSleep();

            if (_options.Mode != InputBlockMode.OverlayOnly)
            {
                _inputBlocker.StartBlocking(_options.Mode);
            }

            UpdateRunningUi();
            MaybeBeep();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Input blocking could not start.\n\n" + ex.Message,
                "Clean Your Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            Finish("Could not start input blocking.");
        }
    }

    private void UpdateCountdownUi()
    {
        _stateLabel.Text = "Starting in";
        _timeLabel.Text = _countdown.ToString();
        _modeLabel.Text = FormatMode(_options.Mode);
        _unlockLabel.Text = "Click Cancel before locking starts.";
        _progressFill.Width = 0;
    }

    private void UpdateRunningUi()
    {
        _stateLabel.Text = "Cleaning Mode";
        _timeLabel.Text = FormatTime(_remainingSeconds);
        _modeLabel.Text = FormatMode(_options.Mode);
        _unlockLabel.Text = $"Spam Esc / Space / Enter {_options.UnlockTapCount} times to unlock early.";
    }

    private void MaybeBeep()
    {
        if (_options.BeepLastFiveSeconds && _remainingSeconds is >= 1 and <= 5)
        {
            SoundAlertPlayer.PlayDefaultBeep();
        }
    }

    private void InputBlocker_EmergencyUnlockProgressChanged(object? sender, EmergencyUnlockProgressEventArgs e)
    {
        BeginInvoke(new Action(() => UpdateEmergencyProgress(e.Current, e.Required)));
    }

    private void Overlay_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_phase != Phase.Running || _options.Mode != InputBlockMode.OverlayOnly)
        {
            return;
        }

        if (e.KeyCode is not (Keys.Escape or Keys.Space or Keys.Enter))
        {
            return;
        }

        if (_overlayPressedEmergencyKeys.Contains(e.KeyCode))
        {
            return;
        }

        _overlayPressedEmergencyKeys.Add(e.KeyCode);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastOverlayEmergencyPressUtc > TimeSpan.FromSeconds(3))
        {
            _overlayEmergencyPresses = 0;
        }

        _lastOverlayEmergencyPressUtc = now;
        _overlayEmergencyPresses = Math.Min(_overlayEmergencyPresses + 1, _options.UnlockTapCount);
        UpdateEmergencyProgress(_overlayEmergencyPresses, _options.UnlockTapCount);

        if (_overlayEmergencyPresses >= _options.UnlockTapCount)
        {
            Finish("Emergency unlock.");
        }
    }

    private void Overlay_KeyUp(object? sender, KeyEventArgs e)
    {
        _overlayPressedEmergencyKeys.Remove(e.KeyCode);
    }

    private void UpdateEmergencyProgress(int current, int required)
    {
        if (_phase == Phase.Finished || required <= 0)
        {
            return;
        }

        int clamped = Math.Clamp(current, 0, required);
        _progressFill.Width = (int)Math.Round(_progressTrack.Width * (clamped / (double)required));
        _unlockLabel.Text = $"Emergency unlock: {clamped} / {required}";
    }

    private void SafeFinish(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            BeginInvoke(new Action(() => Finish(message)));
        }
        catch
        {
            Cleanup();
        }
    }

    private void Finish(string message)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        _phase = Phase.Finished;
        EndReasonText = message;
        Cleanup();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Cleanup()
    {
        try
        {
            _timer.Stop();
        }
        catch
        {
            // Best-effort cleanup.
        }

        try
        {
            _inputBlocker.Dispose();
        }
        catch
        {
            // Best-effort cleanup.
        }

        SleepManager.AllowSleep();
        CloseSecondaryOverlays();
    }

    private void ShowSecondaryOverlays()
    {
        if (!_options.ScreenOverlayEnabled)
        {
            return;
        }

        Rectangle primaryBounds = Screen.PrimaryScreen?.Bounds ?? Bounds;

        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.Bounds == primaryBounds)
            {
                continue;
            }

            ScreenShieldForm shield = new(screen);
            shield.KeyDown += Overlay_KeyDown;
            shield.KeyUp += Overlay_KeyUp;
            _secondaryOverlays.Add(shield);
            shield.Show(this);
        }
    }

    private void CloseSecondaryOverlays()
    {
        foreach (ScreenShieldForm shield in _secondaryOverlays.ToArray())
        {
            try
            {
                if (!shield.IsDisposed)
                {
                    shield.Close();
                    shield.Dispose();
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        _secondaryOverlays.Clear();
    }

    private void LayoutOverlay()
    {
        if (_centerPanel == null)
        {
            return;
        }

        _centerPanel.Left = Math.Max(24, (ClientSize.Width - _centerPanel.Width) / 2);
        _centerPanel.Top = Math.Max(86, (ClientSize.Height - _centerPanel.Height) / 2);
    }

    private Label CreatePanelLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = BrandColors.Black,
            ForeColor = color,
            Font = CreateFont(size, style)
        };
    }

    private static string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return minutes > 0 ? $"{minutes}:{seconds:00}" : seconds.ToString();
    }

    private static string FormatMode(InputBlockMode mode)
    {
        return mode switch
        {
            InputBlockMode.KeyboardOnly => "Keyboard only",
            InputBlockMode.MouseOnly => "Mouse only",
            InputBlockMode.OverlayOnly => "Screen overlay only",
            _ => "Keyboard + mouse"
        };
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
}
