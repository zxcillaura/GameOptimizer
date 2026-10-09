using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;

namespace GameOptimizer.UI.Controls
{
    public enum FlatButtonStyle { Filled, Soft, Outline, Ghost }

    /// <summary>Modern custom-painted button with hover/press animation.</summary>
    public class FlatButton : Control
    {
        private readonly Timer _anim;
        private double _hover;      // 0..1
        private bool   _pressed;
        private bool   _isHover;

        public FlatButtonStyle Style { get; set; } = FlatButtonStyle.Soft;
        public Color AccentColor { get; set; } = Theme.Accent;
        public string IconKind   { get; set; } = "";
        public int    Radius     { get; set; } = 9;
        public string Hint       { get; set; } = "";

        public FlatButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint             |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Size = new Size(150, 34);

            _anim = new Timer { Interval = 16 };
            _anim.Tick += (_, __) =>
            {
                double target = _isHover ? 1 : 0;
                double diff = target - _hover;
                if (Math.Abs(diff) < 0.05) { _hover = target; _anim.Stop(); }
                else _hover += diff * 0.25;
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { _isHover = true;  _anim.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; _pressed = false; _anim.Start(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color fill, text = Theme.Text, border = Color.Transparent;
            switch (Style)
            {
                case FlatButtonStyle.Filled:
                    fill   = Theme.Mix(AccentColor, Color.White, _hover * 0.18);
                    text   = Color.FromArgb(7, 12, 20);
                    break;
                case FlatButtonStyle.Soft:
                    fill   = Theme.Alpha(AccentColor, (int)(28 + _hover * 44));
                    border = Theme.Alpha(AccentColor, (int)(90 + _hover * 90));
                    text   = Theme.Mix(AccentColor, Color.White, 0.45 + _hover * 0.35);
                    break;
                case FlatButtonStyle.Outline:
                    fill   = Theme.Alpha(Theme.Surface, (int)(120 + _hover * 100));
                    border = Theme.Alpha(AccentColor, (int)(110 + _hover * 90));
                    text   = Theme.TextSecondary;
                    break;
                default:
                    fill   = Theme.Alpha(Color.White, (int)(_hover * 16));
                    text   = Theme.TextSecondary;
                    break;
            }

            if (!Enabled)
            {
                fill = Theme.Alpha(Theme.Surface, 90);
                text = Theme.Muted;
                border = Color.Transparent;
            }

            using (var b = new SolidBrush(fill))
                g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, Radius);

            if (border.A > 0)
            {
                using var p = new Pen(border, 1f);
                g.DrawRoundedRectangle(p, r.X, r.Y, r.Width, r.Height, Radius);
            }

            int textLeft = 10;
            if (!string.IsNullOrEmpty(IconKind))
            {
                int iconBox = Height - 14;
                var ir = new Rectangle(11, (Height - iconBox) / 2, iconBox, iconBox);
                IconPainter.Draw(g, ir, IconKind, Enabled ? text : Theme.Muted, 1.5f);
                textLeft = ir.Right + 7;
            }

            var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            using (var tb = new SolidBrush(text))
                g.DrawString(Text, Font, tb,
                    new RectangleF(textLeft, 0, Width - textLeft - 10, Height), fmt);

            if (_pressed)
            {
                using var pb = new SolidBrush(Theme.Alpha(Color.Black, 40));
                g.FillRoundedRectangle(pb, r.X, r.Y, r.Width, r.Height, Radius);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _anim?.Dispose();
            base.Dispose(disposing);
        }
    }
}
