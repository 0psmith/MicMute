using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class OverlayForm : Form
    {
        private const int CornerRadius = 8;

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
            BackColor = Color.FromArgb(24, 29, 36);
            ForeColor = Color.White;
            Opacity = 0.90;
            Size = new Size(204, 58);
            Padding = new Padding(10);

            _glyph = new MicGlyphControl();
            _glyph.Location = new Point(12, 13);
            _glyph.Size = new Size(32, 32);
            Controls.Add(_glyph);

            _title = new Label();
            _title.AutoSize = false;
            _title.Location = new Point(54, 8);
            _title.Size = new Size(138, 23);
            _title.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            _title.ForeColor = Color.White;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_title);

            _device = new Label();
            _device.AutoSize = false;
            _device.Location = new Point(55, 31);
            _device.Size = new Size(136, 18);
            _device.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            _device.ForeColor = Color.FromArgb(202, 210, 220);
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
            _title.Text = muted ? "Microphone muted" : "Microphone live";
            _device.Text = Truncate(deviceName, 22);
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
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (Pen pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.0f))
            using (GraphicsPath path = CreateRoundedPath(bounds, CornerRadius))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            Region oldRegion = Region;
            using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region = new Region(path);
            }

            if (oldRegion != null)
            {
                oldRegion.Dispose();
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
                return "Default microphone";
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 1) + "...";
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
