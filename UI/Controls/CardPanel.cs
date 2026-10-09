using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;

namespace GameOptimizer.UI.Controls
{
    /// <summary>Rounded surface with an optional title and accent stripe.</summary>
    public class CardPanel : Panel
    {
        public int    CornerRadius { get; set; } = 12;
        public string Title        { get; set; } = "";
        public string Subtitle     { get; set; } = "";
        public Color  AccentColor  { get; set; } = Theme.Accent;
        public bool   ShowAccent   { get; set; } = false;
        public bool   Highlight    { get; set; } = false;
        public Color  FillColor    { get; set; } = Theme.Card;
        public Color  BorderColor  { get; set; } = Theme.Border;

        public CardPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint             |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
            ApplyPadding();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var b = new SolidBrush(FillColor))
                g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, CornerRadius);

            if (Highlight)
            {
                using var glow = new Pen(Theme.Alpha(AccentColor, 150), 1.6f);
                g.DrawRoundedRectangle(glow, r.X, r.Y, r.Width, r.Height, CornerRadius);
            }
            else if (BorderColor.A > 0)
            {
                using var p = new Pen(BorderColor, 1f);
                g.DrawRoundedRectangle(p, r.X, r.Y, r.Width, r.Height, CornerRadius);
            }

            if (ShowAccent)
            {
                using var path = RoundedStrip(new Rectangle(0, 0, 4, Height), CornerRadius);
                using var b = new LinearGradientBrush(new Rectangle(0, 0, 4, Math.Max(2, Height)),
                    AccentColor, Theme.Alpha(AccentColor, 40), LinearGradientMode.Vertical);
                g.FillPath(b, path);
            }

            if (!string.IsNullOrEmpty(Title))
            {
                using var f = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var b = new SolidBrush(Theme.Text);
                g.DrawString(Title, f, b, 14, 11);
            }

            if (!string.IsNullOrEmpty(Subtitle))
            {
                using var f = new Font("Segoe UI", 8f);
                using var b = new SolidBrush(Theme.Muted);
                var sz = g.MeasureString(Subtitle, f);
                g.DrawString(Subtitle, f, b, Width - sz.Width - 14, 13);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath RoundedStrip(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom - radius);
            p.AddLine(r.X, r.Bottom, r.X, r.Y + radius);
            p.CloseFigure();
            return p;
        }

        private void ApplyPadding()
        {
            int top = 14;
            if (!string.IsNullOrEmpty(Title)) top = 40;
            Padding = new Padding(14, top, 14, 14);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            ApplyPadding();
            base.OnResize(eventargs);
        }
    }
}
