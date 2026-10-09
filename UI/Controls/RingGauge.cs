using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.Controls
{
    /// <summary>
    /// Animated 270° ring gauge with a gradient sweep — the signature element of v7.
    /// </summary>
    public class RingGauge : Control
    {
        private readonly Timer _anim;
        private double _target;
        private double _current;
        private double _thickness = 10;

        public double Value
        {
            get => _target;
            set
            {
                _target = Math.Max(0, Math.Min(MaxValue, value));
                if (!_anim.Enabled) _anim.Start();
                Invalidate();
            }
        }

        public double MaxValue   { get; set; } = 100;
        public string Caption    { get; set; } = "";
        public string Unit       { get; set; } = "%";
        public string BigText    { get; set; } = "";
        public bool   UseScaleColor { get; set; } = true;
        public Color  AccentFrom { get; set; } = Theme.GradientStart;
        public Color  AccentTo   { get; set; } = Theme.GradientEnd;
        public double Thickness  { get => _thickness; set { _thickness = value; Invalidate(); } }

        public RingGauge()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint             |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(150, 150);
            Font = new Font("Segoe UI", 9f);

            _anim = new Timer { Interval = 16 };
            _anim.Tick += (_, __) =>
            {
                double diff = _target - _current;
                if (Math.Abs(diff) < 0.4) { _current = _target; _anim.Stop(); }
                else _current += diff * 0.16;
                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float pad = (float)_thickness / 2f + 3f;
            float size = Math.Min(Width, Height) - pad * 2f;
            if (size <= 6) return;

            var rect = new RectangleF(pad, pad, size, size);

            using (var track = new Pen(Theme.Alpha(Theme.Border, 190), (float)_thickness)
                   { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(track, rect, 135f, 270f);

            double pct = MaxValue <= 0 ? 0 : Math.Max(0, Math.Min(1, _current / MaxValue));
            if (pct > 0.001)
            {
                Color from = UseScaleColor ? Theme.ForLoad(pct * 100.0) : AccentFrom;
                Color to   = UseScaleColor ? Theme.Mix(Theme.ForLoad(pct * 100.0), Theme.AccentAlt, 0.55)
                                           : AccentTo;

                using var brush = new LinearGradientBrush(
                    Rectangle.Round(rect), from, to, LinearGradientMode.ForwardDiagonal);
                using var pen = new Pen(brush, (float)_thickness)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(pen, rect, 135f, (float)(270.0 * pct));
            }

            // centre value
            string big = string.IsNullOrEmpty(BigText) ? _current.ToString("F0") : BigText;
            using (var f = new Font("Segoe UI", Math.Max(13f, size * 0.20f), FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                var box = new RectangleF(0, Height * 0.34f, Width, Height * 0.30f);
                using var shadow = new SolidBrush(Theme.Alpha(Color.Black, 90));
                g.DrawString(big, f, shadow, new RectangleF(box.X + 1, box.Y + 1, box.Width, box.Height), sf);
                using var tb = new SolidBrush(Theme.Text);
                g.DrawString(big, f, tb, box, sf);
            }

            if (!string.IsNullOrEmpty(Unit))
            {
                using var f = new Font("Segoe UI", 8.5f);
                using var sf = new StringFormat { Alignment = StringAlignment.Center };
                using var tb = new SolidBrush(Theme.TextSecondary);
                g.DrawString(Unit, f, tb, new RectangleF(0, Height * 0.60f, Width, 16), sf);
            }

            if (!string.IsNullOrEmpty(Caption))
            {
                using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var sf = new StringFormat { Alignment = StringAlignment.Center };
                using var tb = new SolidBrush(Theme.Muted);
                g.DrawString(Caption.ToUpperInvariant(), f, tb,
                    new RectangleF(0, Height - 22, Width, 18), sf);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _anim?.Dispose();
            base.Dispose(disposing);
        }
    }
}
