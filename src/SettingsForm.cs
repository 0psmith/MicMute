using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class SettingsForm : Form
    {
        private static readonly Color WindowBackColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBackColor = Color.White;
        private static readonly Color BorderColor = Color.FromArgb(224, 229, 237);
        private static readonly Color PrimaryColor = Color.FromArgb(40, 135, 92);
        private static readonly Color TextColor = Color.FromArgb(32, 38, 46);
        private static readonly Color MutedTextColor = Color.FromArgb(105, 116, 130);

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
            Text = "MicMute 설정";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 620);
            Size = new Size(820, 680);
            Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            BackColor = WindowBackColor;
            ForeColor = TextColor;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.BackColor = WindowBackColor;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            Controls.Add(root);

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateContent(), 0, 1);
            root.Controls.Add(CreateFooter(), 0, 2);
        }

        private Control CreateHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = WindowBackColor;
            header.Padding = new Padding(28, 22, 28, 12);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.Controls.Add(layout);

            TableLayoutPanel titleBlock = new TableLayoutPanel();
            titleBlock.Dock = DockStyle.Fill;
            titleBlock.ColumnCount = 1;
            titleBlock.RowCount = 2;
            titleBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            titleBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.Controls.Add(titleBlock, 0, 0);

            Label title = new Label();
            title.Text = "MicMute";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Malgun Gothic", 18.0f, FontStyle.Bold);
            title.ForeColor = TextColor;
            title.TextAlign = ContentAlignment.MiddleLeft;
            titleBlock.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = "마이크 음소거, 단축키, 오버레이 동작을 조정합니다.";
            subtitle.Dock = DockStyle.Fill;
            subtitle.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            subtitle.ForeColor = MutedTextColor;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            titleBlock.Controls.Add(subtitle, 0, 1);

            Label badge = new Label();
            badge.Text = "설정";
            badge.Dock = DockStyle.None;
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            badge.Size = new Size(112, 30);
            badge.Margin = new Padding(0, 6, 0, 0);
            badge.BackColor = Color.FromArgb(225, 244, 236);
            badge.ForeColor = PrimaryColor;
            badge.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold);
            badge.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(badge, 1, 0);

            return header;
        }

        private Control CreateContent()
        {
            Panel shell = new Panel();
            shell.Dock = DockStyle.Fill;
            shell.BackColor = WindowBackColor;
            shell.Padding = new Padding(28, 0, 28, 0);

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.ColumnCount = 2;
            content.RowCount = 3;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("마이크", "음소거를 제어할 입력 장치를 선택합니다."), 0, 0);

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
            _deviceCombo.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            row.Controls.Add(_deviceCombo, 0, 0);

            Button refresh = CreateSecondaryButton("새로 고침");
            refresh.Dock = DockStyle.Fill;
            refresh.Click += delegate { PopulateDevices(GetSelectedDeviceId()); };
            row.Controls.Add(refresh, 1, 0);

            layout.Controls.Add(row, 0, 1);

            _deviceHintLabel = CreateHintLabel("Windows 기본 마이크를 선택하면 현재 기본 입력 장치를 따라갑니다.");
            layout.Controls.Add(_deviceHintLabel, 0, 2);

            return card;
        }

        private Control CreateHotkeyCard()
        {
            RoundedPanel card = CreateCard();
            TableLayoutPanel layout = CreateCardLayout(3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("단축키", "키보드 조합이나 마우스 특수 버튼으로 음소거를 전환합니다."), 0, 0);

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
            _hotkeyValueLabel.Font = new Font("Malgun Gothic", 10.0f, FontStyle.Bold);
            _hotkeyValueLabel.Padding = new Padding(12, 0, 12, 0);
            _hotkeyValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            row.Controls.Add(_hotkeyValueLabel, 0, 0);

            Button record = CreatePrimaryButton("기록");
            record.Dock = DockStyle.Fill;
            record.Click += delegate { RecordHotkey(); };
            row.Controls.Add(record, 1, 0);

            layout.Controls.Add(row, 0, 1);
            layout.Controls.Add(CreateHintLabel("기록을 누른 뒤 사용할 키 조합, 마우스 가운데 버튼, XButton1, XButton2를 누르세요."), 0, 2);

            return card;
        }

        private Control CreateBehaviorCard()
        {
            RoundedPanel card = CreateCard();
            card.Margin = new Padding(0, 8, 8, 0);

            TableLayoutPanel layout = CreateCardLayout(4);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("동작", "앱 종료와 Windows 시작 동작을 설정합니다."), 0, 0);

            _closeToTrayCheck = CreateCheckBox("창 닫기 시 트레이로 이동");
            layout.Controls.Add(_closeToTrayCheck, 0, 1);

            _startWithWindowsCheck = CreateCheckBox("Windows 시작 시 자동 실행");
            layout.Controls.Add(_startWithWindowsCheck, 0, 2);

            layout.Controls.Add(CreateHintLabel("자동 실행은 현재 사용자 계정의 시작 프로그램에 등록됩니다."), 0, 3);
            return card;
        }

        private Control CreateOverlayCard()
        {
            RoundedPanel card = CreateCard();
            card.Margin = new Padding(8, 8, 0, 0);

            TableLayoutPanel layout = CreateCardLayout(5);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            layout.Controls.Add(CreateSectionTitle("오버레이", "음소거 전환 시 표시되는 작은 알림을 조정합니다."), 0, 0);

            Label positionLabel = CreateFieldLabel("위치");
            layout.Controls.Add(positionLabel, 0, 1);

            _overlayCombo = new ComboBox();
            _overlayCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _overlayCombo.Dock = DockStyle.Fill;
            _overlayCombo.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopLeft, "왼쪽 위"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopCenter, "위쪽 가운데"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.TopRight, "오른쪽 위"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.Center, "가운데"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomLeft, "왼쪽 아래"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomCenter, "아래쪽 가운데"));
            _overlayCombo.Items.Add(new EnumComboItem(OverlayPosition.BottomRight, "오른쪽 아래"));
            layout.Controls.Add(_overlayCombo, 0, 2);

            TableLayoutPanel opacityHeader = new TableLayoutPanel();
            opacityHeader.Dock = DockStyle.Fill;
            opacityHeader.ColumnCount = 2;
            opacityHeader.RowCount = 1;
            opacityHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            opacityHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

            opacityHeader.Controls.Add(CreateFieldLabel("투명도"), 0, 0);
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

            Button ok = CreatePrimaryButton("확인");
            ok.Width = 92;
            ok.Click += delegate
            {
                if (TryApply())
                {
                    Hide();
                }
            };

            Button cancel = CreateSecondaryButton("취소");
            cancel.Width = 92;
            cancel.Click += delegate { Hide(); };

            Button apply = CreateSecondaryButton("적용");
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
            card.Radius = 10;
            card.Padding = new Padding(18, 16, 18, 16);
            card.Margin = new Padding(0, 0, 0, 8);
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
            block.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            block.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            Label title = new Label();
            title.Text = titleText;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Malgun Gothic", 10.0f, FontStyle.Bold);
            title.ForeColor = TextColor;
            title.TextAlign = ContentAlignment.MiddleLeft;
            block.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = subtitleText;
            subtitle.Dock = DockStyle.Fill;
            subtitle.Font = new Font("Malgun Gothic", 8.2f, FontStyle.Regular);
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
            label.Font = new Font("Malgun Gothic", 8.0f, FontStyle.Regular);
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
            label.Font = new Font("Malgun Gothic", 8.7f, FontStyle.Bold);
            label.ForeColor = MutedTextColor;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private CheckBox CreateCheckBox(string text)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Text = text;
            checkBox.Dock = DockStyle.Fill;
            checkBox.Font = new Font("Malgun Gothic", 9.2f, FontStyle.Regular);
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
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 154, 107);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 111, 75);
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
            button.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold);
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
            _deviceCombo.Items.Add(new DeviceComboItem(string.Empty, "Windows 기본 마이크"));

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
                _deviceCombo.Items.Add(new DeviceComboItem(selectedDeviceId, "사용할 수 없는 마이크"));
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
                    ? "장치 목록을 읽을 수 없습니다. 기본 마이크는 계속 사용할 수 있습니다."
                    : "Windows 기본 마이크를 선택하면 현재 기본 입력 장치를 따라갑니다.";
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
                MessageBox.Show(this, "사용할 단축키를 기록하세요.", "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            _hotkeyValueLabel.Text = _currentHotkey == null ? "설정되지 않음" : _currentHotkey.ToDisplayString();
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
                _name = name ?? "마이크";
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
                Radius = 10;
                BorderColor = Color.FromArgb(224, 229, 237);
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
