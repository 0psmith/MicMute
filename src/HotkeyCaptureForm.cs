using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class HotkeyCaptureForm : Form
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;

        private static readonly Color WindowBackColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBackColor = Color.White;
        private static readonly Color BorderColor = Color.FromArgb(224, 229, 237);
        private static readonly Color PrimaryColor = Color.FromArgb(40, 135, 92);
        private static readonly Color TextColor = Color.FromArgb(32, 38, 46);
        private static readonly Color MutedTextColor = Color.FromArgb(105, 116, 130);

        private readonly LowLevelKeyboardProc _keyboardProc;
        private readonly LowLevelMouseProc _mouseProc;
        private IntPtr _keyboardHook;
        private IntPtr _mouseHook;
        private Label _currentLabel;
        private bool _closingFromCapture;

        public HotkeyGesture CapturedGesture { get; private set; }

        public HotkeyCaptureForm(HotkeyGesture current)
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            CapturedGesture = current == null ? null : current.Clone();
            InitializeComponent(current);
        }

        private void InitializeComponent(HotkeyGesture current)
        {
            Text = "단축키 기록";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(540, 280);
            MinimumSize = new Size(540, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            BackColor = WindowBackColor;
            ForeColor = TextColor;
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            Controls.Add(root);

            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 1;
            header.RowCount = 2;
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            root.Controls.Add(header, 0, 0);

            Label title = new Label();
            title.Text = "단축키 기록";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Malgun Gothic", 14.0f, FontStyle.Bold);
            title.ForeColor = TextColor;
            title.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = "사용할 키 조합이나 마우스 특수 버튼을 누르면 바로 저장됩니다.";
            subtitle.Dock = DockStyle.Fill;
            subtitle.Font = new Font("Malgun Gothic", 8.5f, FontStyle.Regular);
            subtitle.ForeColor = MutedTextColor;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(subtitle, 0, 1);

            RoundedPanel card = new RoundedPanel();
            card.Dock = DockStyle.Fill;
            card.BackColor = CardBackColor;
            card.BorderColor = BorderColor;
            card.Radius = 10;
            card.Padding = new Padding(18, 16, 18, 16);
            root.Controls.Add(card, 0, 1);

            TableLayoutPanel cardLayout = new TableLayoutPanel();
            cardLayout.Dock = DockStyle.Fill;
            cardLayout.ColumnCount = 1;
            cardLayout.RowCount = 3;
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(cardLayout);

            Label prompt = new Label();
            prompt.Text = "입력 대기 중";
            prompt.Dock = DockStyle.Fill;
            prompt.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold);
            prompt.ForeColor = PrimaryColor;
            prompt.TextAlign = ContentAlignment.MiddleLeft;
            cardLayout.Controls.Add(prompt, 0, 0);

            _currentLabel = new Label();
            _currentLabel.AutoSize = false;
            _currentLabel.BorderStyle = BorderStyle.FixedSingle;
            _currentLabel.Dock = DockStyle.Fill;
            _currentLabel.BackColor = Color.FromArgb(248, 250, 252);
            _currentLabel.Padding = new Padding(14, 0, 14, 0);
            _currentLabel.TextAlign = ContentAlignment.MiddleLeft;
            _currentLabel.ForeColor = TextColor;
            _currentLabel.Font = new Font("Malgun Gothic", 12.0f, FontStyle.Bold);
            _currentLabel.Text = current == null ? "입력 대기 중..." : current.ToDisplayString();
            cardLayout.Controls.Add(_currentLabel, 0, 1);

            Label hint = new Label();
            hint.Text = "Ctrl, Alt, Shift, Win 조합을 지원합니다. 마우스 가운데 버튼, XButton1, XButton2도 사용할 수 있습니다. Esc를 누르면 취소합니다.";
            hint.Dock = DockStyle.Fill;
            hint.Font = new Font("Malgun Gothic", 8.2f, FontStyle.Regular);
            hint.ForeColor = MutedTextColor;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.AutoEllipsis = true;
            cardLayout.Controls.Add(hint, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Right;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.AutoSize = true;
            buttons.Padding = new Padding(0, 12, 0, 0);
            root.Controls.Add(buttons, 0, 2);

            Button cancel = CreateSecondaryButton("취소");
            cancel.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            buttons.Controls.Add(cancel);

            CancelButton = cancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            InstallHooks();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UninstallHooks();
            base.OnFormClosed(e);
        }

        private Button CreateSecondaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Height = 34;
            button.Width = 92;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 243, 248);
            button.BackColor = Color.White;
            button.ForeColor = TextColor;
            button.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold);
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private void InstallHooks()
        {
            if (_keyboardHook == IntPtr.Zero)
            {
                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
            }

            if (_mouseHook == IntPtr.Zero)
            {
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
            }
        }

        private void UninstallHooks()
        {
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN || message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    KBDLLHOOKSTRUCT hook = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    Keys key = (Keys)hook.vkCode;

                    if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                    {
                        if (key == Keys.Escape && !AnyModifierDown())
                        {
                            BeginInvoke(new MethodInvoker(delegate
                            {
                                DialogResult = DialogResult.Cancel;
                                Close();
                            }));
                            return new IntPtr(1);
                        }

                        if (IsModifierOnlyKey(key))
                        {
                            UpdateModifierPreview();
                            return new IntPtr(1);
                        }

                        HotkeyGesture gesture = CreateGestureFromCurrentModifiers();
                        gesture.TriggerType = HotkeyTriggerType.Keyboard;
                        gesture.KeyCode = (int)key;
                        gesture.MouseButton = MouseHotkeyButton.None;

                        if (gesture.IsValid())
                        {
                            CompleteCapture(gesture);
                            return new IntPtr(1);
                        }
                    }

                    return new IntPtr(1);
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                MouseHotkeyButton button = MouseHotkeyButton.None;

                if (message == WM_MBUTTONDOWN)
                {
                    button = MouseHotkeyButton.Middle;
                }
                else if (message == WM_XBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT hook = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    int xButton = (int)((hook.mouseData >> 16) & 0xffff);
                    if (xButton == XBUTTON1)
                    {
                        button = MouseHotkeyButton.XButton1;
                    }
                    else if (xButton == XBUTTON2)
                    {
                        button = MouseHotkeyButton.XButton2;
                    }
                }

                if (button != MouseHotkeyButton.None)
                {
                    HotkeyGesture gesture = CreateGestureFromCurrentModifiers();
                    gesture.TriggerType = HotkeyTriggerType.MouseButton;
                    gesture.KeyCode = 0;
                    gesture.MouseButton = button;
                    CompleteCapture(gesture);
                    return new IntPtr(1);
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void CompleteCapture(HotkeyGesture gesture)
        {
            if (_closingFromCapture)
            {
                return;
            }

            _closingFromCapture = true;
            BeginInvoke(new MethodInvoker(delegate
            {
                CapturedGesture = gesture.Clone();
                _currentLabel.Text = CapturedGesture.ToDisplayString();
                DialogResult = DialogResult.OK;
                Close();
            }));
        }

        private void UpdateModifierPreview()
        {
            HotkeyGesture modifiers = CreateGestureFromCurrentModifiers();
            string text = string.Empty;
            AppendModifier(ref text, modifiers.Control, "Ctrl");
            AppendModifier(ref text, modifiers.Alt, "Alt");
            AppendModifier(ref text, modifiers.Shift, "Shift");
            AppendModifier(ref text, modifiers.Win, "Win");

            if (text.Length == 0)
            {
                text = "입력 대기 중...";
            }
            else
            {
                text += " + ...";
            }

            BeginInvoke(new MethodInvoker(delegate { _currentLabel.Text = text; }));
        }

        private HotkeyGesture CreateGestureFromCurrentModifiers()
        {
            return new HotkeyGesture
            {
                Control = ModifierDown(Keys.ControlKey, Keys.LControlKey, Keys.RControlKey),
                Alt = ModifierDown(Keys.Menu, Keys.LMenu, Keys.RMenu),
                Shift = ModifierDown(Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey),
                Win = ModifierDown(Keys.LWin, Keys.RWin)
            };
        }

        private static bool AnyModifierDown()
        {
            return ModifierDown(Keys.ControlKey, Keys.LControlKey, Keys.RControlKey) ||
                   ModifierDown(Keys.Menu, Keys.LMenu, Keys.RMenu) ||
                   ModifierDown(Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey) ||
                   ModifierDown(Keys.LWin, Keys.RWin);
        }

        private static bool ModifierDown(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if ((GetAsyncKeyState((int)keys[i]) & 0x8000) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsModifierOnlyKey(Keys key)
        {
            return key == Keys.ControlKey ||
                   key == Keys.LControlKey ||
                   key == Keys.RControlKey ||
                   key == Keys.ShiftKey ||
                   key == Keys.LShiftKey ||
                   key == Keys.RShiftKey ||
                   key == Keys.Menu ||
                   key == Keys.LMenu ||
                   key == Keys.RMenu ||
                   key == Keys.LWin ||
                   key == Keys.RWin ||
                   key == Keys.Control ||
                   key == Keys.Shift ||
                   key == Keys.Alt;
        }

        private static void AppendModifier(ref string value, bool enabled, string name)
        {
            if (!enabled)
            {
                return;
            }

            if (value.Length > 0)
            {
                value += " + ";
            }

            value += name;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
