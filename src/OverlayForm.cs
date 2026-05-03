using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class OverlayForm : Form
    {
        private readonly Label _title;
        private readonly Label _device;
        private readonly Timer _hideTimer;
        private readonly MicGlyphControl _glyph;

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(28, 31, 36);
            ForeColor = Color.White;
            Opacity = 0.90;
            Size = new Size(168, 52);
            Padding = new Padding(8);

            _glyph = new MicGlyphControl();
            _glyph.Location = new Point(10, 11);
            _glyph.Size = new Size(30, 30);
            Controls.Add(_glyph);

            _title = new Label();
            _title.AutoSize = false;
            _title.Location = new Point(48, 7);
            _title.Size = new Size(112, 21);
            _title.Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);
            _title.ForeColor = Color.White;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_title);

            _device = new Label();
            _device.AutoSize = false;
            _device.Location = new Point(49, 27);
            _device.Size = new Size(108, 17);
            _device.Font = new Font("Malgun Gothic", 7.0f, FontStyle.Regular);
            _device.ForeColor = Color.FromArgb(210, 216, 225);
            _device.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_device);

            _hideTimer = new Timer();
            _hideTimer.Interval = 1200;
            _hideTimer.Tick += delegate
            {
                _hideTimer.Stop();
                Hide();
            };
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        public void ShowStatus(bool muted, string deviceName, OverlayPosition position, int opacityPercent)
        {
            _glyph.Muted = muted;
            _title.Text = muted ? "마이크 음소거됨" : "마이크 켜짐";
            _device.Text = Truncate(deviceName, 16);
            Opacity = AppSettings.NormalizeOverlayOpacityPercent(opacityPercent) / 100.0;
            Place(position);
            _hideTimer.Stop();
            Show();
            BringToFront();
            _hideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.0f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void Place(OverlayPosition position)
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            const int margin = 16;
            int x;
            int y;

            switch (position)
            {
                case OverlayPosition.TopLeft:
                    x = area.Left + margin;
                    y = area.Top + margin;
                    break;
                case OverlayPosition.TopCenter:
                    x = area.Left + (area.Width - Width) / 2;
                    y = area.Top + margin;
                    break;
                case OverlayPosition.TopRight:
                    x = area.Right - Width - margin;
                    y = area.Top + margin;
                    break;
                case OverlayPosition.Center:
                    x = area.Left + (area.Width - Width) / 2;
                    y = area.Top + (area.Height - Height) / 2;
                    break;
                case OverlayPosition.BottomLeft:
                    x = area.Left + margin;
                    y = area.Bottom - Height - margin;
                    break;
                case OverlayPosition.BottomRight:
                    x = area.Right - Width - margin;
                    y = area.Bottom - Height - margin;
                    break;
                default:
                    x = area.Left + (area.Width - Width) / 2;
                    y = area.Bottom - Height - margin;
                    break;
            }

            Location = new Point(x, y);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "기본 마이크";
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 1) + "...";
        }

        private sealed class MicGlyphControl : Control
        {
            private bool _muted;

            public bool Muted
            {
                get { return _muted; }
                set
                {
                    _muted = value;
                    Invalidate();
                }
            }

            public MicGlyphControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color accent = _muted ? Color.FromArgb(232, 82, 82) : Color.FromArgb(71, 190, 126);

                using (SolidBrush brush = new SolidBrush(accent))
                using (Pen pen = new Pen(accent, 2.4f))
                using (Pen slash = new Pen(Color.White, 2.4f))
                {
                    e.Graphics.FillRoundedRectangle(brush, new RectangleF(11, 3, 8, 15), 4);
                    e.Graphics.DrawArc(pen, 6, 11, 18, 13, 0, 180);
                    e.Graphics.DrawLine(pen, 15, 23, 15, 27);
                    e.Graphics.DrawLine(pen, 10, 27, 20, 27);

                    if (_muted)
                    {
                        e.Graphics.DrawLine(slash, 6, 25, 25, 6);
                    }
                }
            }
        }
    }
}
