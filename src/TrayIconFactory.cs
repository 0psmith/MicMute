using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MicMute
{
    internal static class TrayIconFactory
    {
        public static Icon Create(bool muted)
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Color accent = muted ? Color.FromArgb(218, 62, 62) : Color.FromArgb(45, 170, 105);
                using (SolidBrush brush = new SolidBrush(accent))
                using (Pen pen = new Pen(Color.White, 3.0f))
                using (Pen accentPen = new Pen(accent, 4.0f))
                {
                    graphics.FillRoundedRectangle(brush, new RectangleF(11, 4, 10, 15), 5);
                    graphics.DrawLine(accentPen, 16, 18, 16, 25);
                    graphics.DrawArc(accentPen, 8, 11, 16, 13, 0, 180);
                    graphics.DrawLine(accentPen, 11, 26, 21, 26);

                    if (muted)
                    {
                        graphics.DrawLine(pen, 7, 25, 25, 7);
                        using (Pen redPen = new Pen(Color.FromArgb(190, 35, 35), 2.0f))
                        {
                            graphics.DrawLine(redPen, 7, 25, 25, 7);
                        }
                    }
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon icon = Icon.FromHandle(handle))
                    {
                        return (Icon)icon.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2.0f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
