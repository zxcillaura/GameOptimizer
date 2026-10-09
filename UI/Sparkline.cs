using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI
{
    /// <summary>
    /// Minimal line/area chart used to visualize ping/jitter samples over time.
    /// No external charting dependency — pure GDI+.
    /// </summary>
    public class Sparkline : Control
    {
        private readonly List<double> _samples = new();
        public Color LineColor { get; set; } = Theme.Accent;
        public string Unit { get; set; } = "мс";

        public Sparkline()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Height = 90;
        }

        public void SetSamples(IEnumerable<double> values)
        {
            _samples.Clear();
            _samples.AddRange(values);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new SolidBrush(BackColor);
            g.FillRectangle(bg, ClientRectangle);

            if (_samples.Count < 2)
            {
                using var muted = new SolidBrush(Theme.Muted);
                using var font = new Font("Segoe UI", 8f);
                var text = "Нет данных — запустите тест";
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, muted, Width / 2f - size.Width / 2f, Height / 2f - size.Height / 2f);
                return;
            }

            var padding = 10f;
            var min = _samples.Min();
            var max = Math.Max(_samples.Max(), min + 1);
            var stepX = (Width - padding * 2) / (_samples.Count - 1);

            float Xat(int i) => padding + i * stepX;
            float Yat(double v) => padding + (float)((max - v) / (max - min)) * (Height - padding * 2);

            using var path = new GraphicsPath();
            path.AddLine(Xat(0), Height - padding, Xat(0), Yat(_samples[0]));
            for (var i = 0; i < _samples.Count; i++) path.AddLine(Xat(Math.Max(0, i - 1)), Yat(_samples[Math.Max(0, i - 1)]), Xat(i), Yat(_samples[i]));
            path.AddLine(Xat(_samples.Count - 1), Yat(_samples[^1]), Xat(_samples.Count - 1), Height - padding);
            path.CloseFigure();
            using var fillBrush = new LinearGradientBrush(new Point(0, 0), new Point(0, Height), Color.FromArgb(70, LineColor), Color.FromArgb(0, LineColor));
            g.FillPath(fillBrush, path);

            using var linePen = new Pen(LineColor, 2f) { LineJoin = LineJoin.Round };
            for (var i = 1; i < _samples.Count; i++)
                g.DrawLine(linePen, Xat(i - 1), Yat(_samples[i - 1]), Xat(i), Yat(_samples[i]));

            using var dotBrush = new SolidBrush(LineColor);
            var lastX = Xat(_samples.Count - 1);
            var lastY = Yat(_samples[^1]);
            g.FillEllipse(dotBrush, lastX - 3, lastY - 3, 6, 6);

            using var labelFont = new Font("Consolas", 8f, FontStyle.Bold);
            using var labelBrush = new SolidBrush(Theme.Text);
            var label = $"{_samples[^1]:0} {Unit}";
            var labelSize = g.MeasureString(label, labelFont);
            g.DrawString(label, labelFont, labelBrush, Width - labelSize.Width - 8, 6);
        }
    }
}
