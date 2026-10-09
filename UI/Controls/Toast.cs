using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;

namespace GameOptimizer.UI.Controls
{
    /// <summary>Slide-in notification pinned to the top-right of the host window.</summary>
    public sealed class Toast : Control
    {
        private readonly Timer _slide;
        private readonly Timer _hold;
        private readonly Timer _fade;
        private int _targetLeft;
        private int _targetTop;
        private double _opacity = 1;
        private Color _accent;

        private Toast(string text, Color accent, int ms)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);
            _accent = accent;
            Text = text;
            Size = new Size(330, 56);
            Font = new Font("Segoe UI", 9f);

            _slide = new Timer { Interval = 16 };
            _slide.Tick += (_, __) =>
            {
                int diff = _targetLeft - Left;
                if (Math.Abs(diff) <= 2) { Left = _targetLeft; _slide.Stop(); _hold.Start(); }
                else Left += diff / 3;
            };

            _hold = new Timer { Interval = Math.Max(1200, ms) };
            _hold.Tick += (_, __) => { _hold.Stop(); _fade.Start(); };

            _fade = new Timer { Interval = 16 };
            _fade.Tick += (_, __) =>
            {
                _opacity -= 0.06;
                if (_opacity <= 0.02)
                {
                    _fade.Stop();
                    Parent?.Controls.Remove(this);
                    Dispose();
                }
                else Invalidate();
            };
        }

        public static void Show(Control host, string text, Color accent, int ms = 3200)
        {
            if (host == null || text == null) return;
            try
            {
                var t = new Toast(text, accent, ms);
                host.Controls.Add(t);
                t.BringToFront();
                // on a Form, sit below the custom title bar; inside a panel, hug the top
                t._targetTop = host is Form ? 58 : 14;
                t._targetLeft = host.ClientSize.Width - t.Width - 18;
                t.Left = host.ClientSize.Width + 8;
                t.Top = t._targetTop;
                t._slide.Start();
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(Theme.Mix(Theme.Elevated, Theme.Background, 1 - _opacity)))
                g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, 10);

            using (var p = new Pen(Theme.Alpha(_accent, (int)(200 * _opacity)), 1.3f))
                g.DrawRoundedRectangle(p, r.X, r.Y, r.Width, r.Height, 10);

            using (var b = new SolidBrush(Theme.Alpha(_accent, (int)(255 * _opacity))))
            {
                using var path = new GraphicsPath();
                path.AddArc(0, 0, 20, 20, 90, 90);
                path.AddLine(10, 0, 10, Height);
                path.AddLine(10, Height, 0, Height - 10);
                path.CloseFigure();
                g.FillRectangle(b, 0, 10, 4, Height - 20);
            }

            using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var b = new SolidBrush(Theme.Alpha(Theme.Text, (int)(255 * _opacity))))
                g.DrawString(Text, f, b, 16, 10);

            using (var f = new Font("Segoe UI", 8f))
            using (var b = new SolidBrush(Theme.Alpha(Theme.TextSecondary, (int)(255 * _opacity))))
                g.DrawString("GAMEOPTIMIZ 4.0", f, b, 16, 32);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _slide?.Dispose(); _hold?.Dispose(); _fade?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
