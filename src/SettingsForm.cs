using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class SettingsForm : Form
    {
        private static readonly Color WindowBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color CardBackColor = Color.White;
        private static readonly Color BorderColor = Color.FromArgb(220, 226, 235);
        private static readonly Color PrimaryColor = Color.FromArgb(33, 128, 96);
        private static readonly Color TextColor = Color.FromArgb(30, 41, 59);
        private static readonly Color MutedTextColor = Color.FromArgb(100, 116, 139);

        private readonly AudioEndpointController _audio;
        private AppSettings _settings;
        private ComboBox _deviceCombo;
        private ComboBox _overlayCombo;
        private CheckBox _closeToTrayCheck;
        private CheckBox _startWithWindowsCheck;
        private TrackBar _overlayOpacityTrack;
        private Label _overlayOpacityValueLabel;
        private Label _hotkeyValueLabel;
        private Label _deviceHintLabel;
        private HotkeyGesture _currentHotkey;

        public Func<AppSettings, string> ApplySettings;

        public SettingsForm(AudioEndpointController audio, AppSettings settings)
        {
            _audio = audio;
            _settings = settings == null ? AppSettings.Default() : settings.Clone();
            _currentHotkey = _settings.Hotkey == null ? HotkeyGesture.Default() : _settings.Hotkey.Clone();
            InitializeComponent();
            LoadFromSettings(_settings);
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings == null ? AppSettings.Default() : settings.Clone();
            LoadFromSettings(_settings);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PopulateDevices(GetSelectedDeviceId());
        }

        private void InitializeComponent()
        {
            Text = "MicMute Preferences";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(820, 610);
            Size = new Size(880, 650);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            BackColor = WindowBackColor;
            ForeColor = TextColor;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.BackColor = WindowBackColor;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            Controls.Add(root);

            root.Controls.Add(CreateContent(), 0, 0);
            root.Controls.Add(CreateFooter(), 0, 1);
        }

        private Control CreateContent()
        {
            Panel shell = new Panel();
            shell.Dock = DockStyle.Fill;
            shell.BackColor = WindowBackColor;
            shell.Padding = new Padding(28, 24, 28, 0);

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.ColumnCount = 2;
            content.RowCount = 3;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.Controls.Add(content);

            Control deviceCard = CreateDeviceCard();
            content.Controls.Add(deviceCard, 0, 0);
            content.SetColumnSpan(deviceCard, 2);

            Control hotkeyCard = CreateHotkeyCard();
            content.Controls.Add(hotkeyCard, 0, 1);
            content.SetColumnSpan(hotkeyCard, 2);

            content.Controls.Add(CreateBehaviorCard(), 0, 2);
            content.Controls.Add(CreateOverlayCard(), 1, 2);

            return shell;
        }

        private Control CreateDeviceCard()
        {
            RoundedPanel card = CreateCard();
            TableLayoutPanel layout = CreateCardLayout(3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("Microphone", "Select the input device to control."), 0, 0);

            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.ColumnCount = 2;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            row.Margin = new Padding(0, 6, 0, 0);

            _deviceCombo = new ComboBox();
            _deviceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _deviceCombo.Dock = DockStyle.Fill;
            _deviceCombo.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            row.Controls.Add(_deviceCombo, 0, 0);

            Button refresh = CreateSecondaryButton("Refresh");
            refresh.Dock = DockStyle.Fill;
            refresh.Click += delegate { PopulateDevices(GetSelectedDeviceId()); };
            row.Controls.Add(refresh, 1, 0);

            layout.Controls.Add(row, 0, 1);

            _deviceHintLabel = CreateHintLabel("Use Windows default to follow the current input device.");
            layout.Controls.Add(_deviceHintLabel, 0, 2);

            return card;
        }

        private Control CreateHotkeyCard()
        {
            RoundedPanel card = CreateCard();
            TableLayoutPanel layout = CreateCardLayout(3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("Hotkey", "Use a keyboard shortcut or mouse button."), 0, 0);

            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.ColumnCount = 2;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            row.Margin = new Padding(0, 8, 0, 0);

            _hotkeyValueLabel = new Label();
            _hotkeyValueLabel.Dock = DockStyle.Fill;
            _hotkeyValueLabel.BackColor = Color.FromArgb(248, 250, 252);
            _hotkeyValueLabel.BorderStyle = BorderStyle.FixedSingle;
            _hotkeyValueLabel.ForeColor = TextColor;
            _hotkeyValueLabel.Font = new Font("Segoe UI", 10.0f, FontStyle.Bold);
            _hotkeyValueLabel.Padding = new Padding(12, 0, 12, 0);
            _hotkeyValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            row.Controls.Add(_hotkeyValueLabel, 0, 0);

            Button record = CreatePrimaryButton("Record");
            record.Dock = DockStyle.Fill;
            record.Click += delegate { RecordHotkey(); };
            row.Controls.Add(record, 1, 0);

            layout.Controls.Add(row, 0, 1);
            layout.Controls.Add(CreateHintLabel("Press Record, then press a key combo or supported mouse button."), 0, 2);

            return card;
        }

        private Control CreateBehaviorCard()
        {
            RoundedPanel card = CreateCard();
            card.Margin = new Padding(0, 8, 8, 0);

            TableLayoutPanel layout = CreateCardLayout(4);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("Behavior", "Close and startup preferences."), 0, 0);

            _closeToTrayCheck = CreateCheckBox("Close to tray");
            layout.Controls.Add(_closeToTrayCheck, 0, 1);

            _startWithWindowsCheck = CreateCheckBox("Start with Windows");
            layout.Controls.Add(_startWithWindowsCheck, 0, 2);

            layout.Controls.Add(CreateHintLabel("Startup is registered for this Windows user."), 0, 3);
            return card;
        }

        private Control CreateOverlayCard()
        {
            RoundedPanel card = CreateCard();
            card.Margin = new Padding(8, 8, 0, 0);

            TableLayoutPanel layout = CreateCardLayout(5);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("Overlay", "Compact mute status notification."), 0, 0);

            Label positionLabel = CreateFieldLabel("Position");
            layout.Controls.Add(positionLabel, 0, 1);

            _overlayCombo = new ComboBox();
            _overlayCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _overlayCombo.Dock = DockStyle.Fill;
            _overlayCombo.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopLeft, "Top left"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopCenter, "Top center"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopRight, "Top right"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.Center, "Center"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomLeft, "Bottom left"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomCenter, "Bottom center"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomRight, "Bottom right"));
            layout.Controls.Add(_overlayCombo, 0, 2);

            TableLayoutPanel opacityHeader = new TableLayoutPanel();
            opacityHeader.Dock = DockStyle.Fill;
            opacityHeader.ColumnCount = 2;
            opacityHeader.RowCount = 1;
            opacityHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            opacityHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

            opacityHeader.Controls.Add(CreateFieldLabel("Opacity"), 0, 0);
            _overlayOpacityValueLabel = CreateFieldLabel("90%");
            _overlayOpacityValueLabel.TextAlign = ContentAlignment.MiddleRight;
            opacityHeader.Controls.Add(_overlayOpacityValueLabel, 1, 0);
            layout.Controls.Add(opacityHeader, 0, 3);

            _overlayOpacityTrack = new TrackBar();
            _overlayOpacityTrack.Dock = DockStyle.Top;
            _overlayOpacityTrack.Minimum = 30;
            _overlayOpacityTrack.Maximum = 100;
            _overlayOpacityTrack.TickFrequency = 10;
            _overlayOpacityTrack.SmallChange = 5;
            _overlayOpacityTrack.LargeChange = 10;
            _overlayOpacityTrack.Height = 42;
            _overlayOpacityTrack.Margin = new Padding(0, 0, 0, 0);
            _overlayOpacityTrack.ValueChanged += delegate { UpdateOpacityLabel(); };
            layout.Controls.Add(_overlayOpacityTrack, 0, 4);

            return card;
        }

        private Control CreateFooter()
        {
            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.BackColor = WindowBackColor;
            footer.Padding = new Padding(28, 14, 28, 18);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Right;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.AutoSize = true;
            footer.Controls.Add(buttons);

            Button ok = CreatePrimaryButton("OK");
            ok.Width = 92;
            ok.Click += delegate
            {
                if (TryApply())
                {
                    Hide();
                }
            };

            Button cancel = CreateSecondaryButton("Cancel");
            cancel.Width = 92;
            cancel.Click += delegate { Hide(); };

            Button apply = CreateSecondaryButton("Apply");
            apply.Width = 92;
            apply.Click += delegate { TryApply(); };

            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(apply);
            AcceptButton = ok;
            CancelButton = cancel;

            return footer;
        }

        private RoundedPanel CreateCard()
        {
            RoundedPanel card = new RoundedPanel();
            card.Dock = DockStyle.Fill;
            card.BackColor = CardBackColor;
            card.BorderColor = BorderColor;
            card.Radius = 8;
            card.Padding = new Padding(18, 10, 18, 10);
            card.Margin = new Padding(0, 0, 0, 10);
            return card;
        }

        private static TableLayoutPanel CreateCardLayout(int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = rows;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            return layout;
        }

        private Control CreateSectionTitle(string titleText, string subtitleText)
        {
            TableLayoutPanel block = new TableLayoutPanel();
            block.Dock = DockStyle.Fill;
            block.ColumnCount = 1;
            block.RowCount = 2;
            block.Margin = Padding.Empty;
            block.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            block.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));

            Label title = new Label();
            title.Text = titleText;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 10.0f, FontStyle.Bold);
            title.ForeColor = TextColor;
            title.TextAlign = ContentAlignment.MiddleLeft;
            block.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = subtitleText;
            subtitle.Dock = DockStyle.Fill;
            subtitle.Font = new Font("Segoe UI", 8.4f, FontStyle.Regular);
            subtitle.ForeColor = MutedTextColor;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            subtitle.AutoEllipsis = true;
            block.Controls.Add(subtitle, 0, 1);

            return block;
        }

        private Label CreateHintLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.Font = new Font("Segoe UI", 8.0f, FontStyle.Regular);
            label.ForeColor = MutedTextColor;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
            return label;
        }

        private Label CreateFieldLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.Font = new Font("Segoe UI", 8.7f, FontStyle.Bold);
            label.ForeColor = MutedTextColor;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private CheckBox CreateCheckBox(string text)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Text = text;
            checkBox.Dock = DockStyle.Fill;
            checkBox.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            checkBox.ForeColor = TextColor;
            checkBox.AutoSize = false;
            checkBox.Padding = new Padding(0, 6, 0, 0);
            return checkBox;
        }

        private Button CreatePrimaryButton(string text)
        {
            Button button = CreateButton(text);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 148, 112);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 104, 78);
            return button;
        }

        private Button CreateSecondaryButton(string text)
        {
            Button button = CreateButton(text);
            button.BackColor = Color.White;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 243, 248);
            return button;
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Height = 34;
            button.Width = 96;
            button.Margin = new Padding(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private void LoadFromSettings(AppSettings settings)
        {
            _currentHotkey = settings.Hotkey == null ? HotkeyGesture.Default() : settings.Hotkey.Clone();
            PopulateDevices(settings.DeviceId);
            _closeToTrayCheck.Checked = settings.CloseToTray;
            _startWithWindowsCheck.Checked = settings.StartWithWindows;
            _overlayOpacityTrack.Value = AppSettings.NormalizeOverlayOpacityPercent(settings.OverlayOpacityPercent);
            SelectOverlay(settings.OverlayPosition);
            UpdateHotkeyPreview();
            UpdateOpacityLabel();
        }

        private void PopulateDevices(string selectedDeviceId)
        {
            if (_deviceCombo == null)
            {
                return;
            }

            _deviceCombo.BeginUpdate();
            _deviceCombo.Items.Clear();
            _deviceCombo.Items.Add(new DeviceComboItem(string.Empty, "Windows default microphone"));

            List<AudioDeviceInfo> devices = new List<AudioDeviceInfo>();
            bool failed = false;
            try
            {
                devices = _audio.GetCaptureDevices();
            }
            catch
            {
                failed = true;
            }

            bool found = string.IsNullOrEmpty(selectedDeviceId);
            for (int i = 0; i < devices.Count; i++)
            {
                AudioDeviceInfo device = devices[i];
                DeviceComboItem item = new DeviceComboItem(device.Id, device.Name);
                _deviceCombo.Items.Add(item);
                if (string.Equals(device.Id, selectedDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }

            if (!found)
            {
                _deviceCombo.Items.Add(new DeviceComboItem(selectedDeviceId, "Unavailable microphone"));
            }

            for (int i = 0; i < _deviceCombo.Items.Count; i++)
            {
                DeviceComboItem item = _deviceCombo.Items[i] as DeviceComboItem;
                if (item != null && string.Equals(item.Id, selectedDeviceId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    _deviceCombo.SelectedIndex = i;
                    break;
                }
            }

            if (_deviceCombo.SelectedIndex < 0)
            {
                _deviceCombo.SelectedIndex = 0;
            }

            _deviceCombo.EndUpdate();

            if (_deviceHintLabel != null)
            {
                _deviceHintLabel.Text = failed
                    ? "Could not read the device list. The default microphone can still be used."
                    : "Use Windows default to follow the current input device.";
            }
        }

        private void SelectOverlay(OverlayPosition position)
        {
            for (int i = 0; i < _overlayCombo.Items.Count; i++)
            {
                EnumComboItem item = _overlayCombo.Items[i] as EnumComboItem;
                if (item != null && item.Value == position)
                {
                    _overlayCombo.SelectedIndex = i;
                    return;
                }
            }

            _overlayCombo.SelectedIndex = 5;
        }

        private void RecordHotkey()
        {
            using (HotkeyCaptureForm capture = new HotkeyCaptureForm(_currentHotkey))
            {
                capture.Icon = Icon;
                if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedGesture != null)
                {
                    _currentHotkey = capture.CapturedGesture.Clone();
                    UpdateHotkeyPreview();
                }
            }
        }

        private bool TryApply()
        {
            AppSettings candidate = BuildSettingsFromControls();
            if (candidate.Hotkey == null || !candidate.Hotkey.IsValid())
            {
                MessageBox.Show(this, "Record a hotkey to use.", "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string error = ApplySettings == null ? null : ApplySettings(candidate);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(this, error, "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _settings = candidate.Clone();
            UpdateHotkeyPreview();
            return true;
        }

        private AppSettings BuildSettingsFromControls()
        {
            EnumComboItem overlay = _overlayCombo.SelectedItem as EnumComboItem;
            HotkeyGesture hotkey = _currentHotkey == null ? HotkeyGesture.Default() : _currentHotkey.Clone();

            return new AppSettings
            {
                DeviceId = GetSelectedDeviceId(),
                Hotkey = hotkey,
                CloseToTray = _closeToTrayCheck.Checked,
                StartWithWindows = _startWithWindowsCheck.Checked,
                OverlayPosition = overlay == null ? OverlayPosition.BottomCenter : overlay.Value,
                OverlayOpacityPercent = _overlayOpacityTrack == null ? 90 : _overlayOpacityTrack.Value
            };
        }

        private string GetSelectedDeviceId()
        {
            DeviceComboItem item = _deviceCombo == null ? null : _deviceCombo.SelectedItem as DeviceComboItem;
            return item == null ? string.Empty : item.Id;
        }

        private void UpdateHotkeyPreview()
        {
            if (_hotkeyValueLabel == null)
            {
                return;
            }

            _hotkeyValueLabel.Text = _currentHotkey == null ? "Not set" : _currentHotkey.ToDisplayString();
        }

        private void UpdateOpacityLabel()
        {
            if (_overlayOpacityValueLabel == null || _overlayOpacityTrack == null)
            {
                return;
            }

            _overlayOpacityValueLabel.Text = _overlayOpacityTrack.Value.ToString() + "%";
        }

        private sealed class DeviceComboItem
        {
            public string Id { get; private set; }
            private readonly string _name;

            public DeviceComboItem(string id, string name)
            {
                Id = id ?? string.Empty;
                _name = name ?? "Microphone";
            }

            public override string ToString()
            {
                return _name;
            }
        }

        private sealed class EnumComboItem
        {
            public OverlayPosition Value { get; private set; }
            private readonly string _name;

            public EnumComboItem(OverlayPosition value, string name)
            {
                Value = value;
                _name = name;
            }

            public override string ToString()
            {
                return _name;
            }
        }

        private sealed class RoundedPanel : Panel
        {
            public int Radius { get; set; }
            public Color BorderColor { get; set; }

            public RoundedPanel()
            {
                Radius = 8;
                BorderColor = Color.FromArgb(220, 226, 235);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedPath(bounds, Radius))
                using (SolidBrush brush = new SolidBrush(BackColor))
                using (Pen pen = new Pen(BorderColor))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }

            private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
            {
                int diameter = radius * 2;
                GraphicsPath path = new GraphicsPath();
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
