using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.Controls
{
    /// <summary>
    /// Scrolling line chart with a gradient area fill. Keeps a fixed window of
    /// samples per series and redraws on demand.
    /// </summary>
    public class LiveChart : Control
    {
        public sealed class Series
        {
            public string Name { get; set; } = "";
            public Color  Color { get; set; } = Theme.Accent;
            public List<double> Values { get; } = new();
            public bool   Fill { get; set; }
        }

        private readonly List<Series> _series = new();
        private int _capacity = 120;

        public string Title   { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public double Max     { get; set; } = 100;
        public bool   AutoScale { get; set; } = false;
        public string Unit    { get; set; } = "%";
        public int    Capacity
        {
            get => _capacity;
            set { _capacity = Math.Max(10, value); TrimAll(); }
        }
        public bool ShowGrid  { get; set; } = true;

        public LiveChart()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);
            BackColor = Theme.Card;
            Font = new Font("Segoe UI", 9f);
        }

        public Series AddSeries(string name, Color color, bool fill = false)
        {
            var s = new Series { Name = name, Color = color, Fill = fill };
            _series.Add(s);
            return s;
        }

        public void Push(params double[] values)
        {
            for (int i = 0; i < values.Length && i < _series.Count; i++)
            {
                _series[i].Values.Add(values[i]);
                if (_series[i].Values.Count > _capacity) _series[i].Values.RemoveAt(0);
            }
            Invalidate();
        }

        public void Clear()
        {
            foreach (var s in _series) s.Values.Clear();
            Invalidate();
        }

        private void TrimAll()
        {
            foreach (var s in _series)
                while (s.Values.Count > _capacity) s.Values.RemoveAt(0);
        }

        public double LastValue => _series.Count > 0 && _series[0].Values.Count > 0
            ? _series[0].Values.Last() : 0;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            const int padL = 10, padR = 10, padT = 30, padB = 22;
            var plot = new Rectangle(padL, padT, Math.Max(10, Width - padL - padR),
                                     Math.Max(10, Height - padT - padB));

            // header
            using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var b = new SolidBrush(Theme.TextSecondary))
                g.DrawString(Title, f, b, padL, 8);

            if (!string.IsNullOrEmpty(Subtitle))
            {
                using var f = new Font("Segoe UI", 8f);
                var sz = g.MeasureString(Subtitle, f);
                using var b = new SolidBrush(Theme.Muted);
                g.DrawString(Subtitle, f, b, Width - padR - sz.Width, 9);
            }

            // current value badge
            if (_series.Count > 0 && _series[0].Values.Count > 0)
            {
                string txt = $"{_series[0].Values.Last():F0}{Unit}";
                using var f = new Font("Segoe UI", 11f, FontStyle.Bold);
                var sz = g.MeasureString(txt, f);
                var r = new RectangleF(plot.Right - sz.Width - 10, plot.Top + 4, sz.Width + 8, sz.Height + 2);
                using (var bb = new SolidBrush(Theme.Alpha(_series[0].Color, 38)))
                using (var p = new Pen(Theme.Alpha(_series[0].Color, 120)))
                {
                    g.FillRectangle(bb, r);
                    g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
                }
                using var tb = new SolidBrush(_series[0].Color);
                g.DrawString(txt, f, tb, r.X + 4, r.Y + 1);
            }

            if (ShowGrid)
            {
                using var gp = new Pen(Theme.Alpha(Theme.Border, 110), 1);
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Top + plot.Height * i / 4f;
                    g.DrawLine(gp, plot.Left, y, plot.Right, y);
                }
                for (int i = 0; i <= 6; i++)
                {
                    float x = plot.Left + plot.Width * i / 6f;
                    g.DrawLine(gp, x, plot.Top, x, plot.Bottom);
                }
            }

            double max = Max;
            if (AutoScale)
            {
                double m = 1;
                foreach (var s in _series)
                    if (s.Values.Count > 0) m = Math.Max(m, s.Values.Max());
                max = Math.Ceiling(m / 10.0) * 10.0;
                if (max <= 0) max = 1;
            }

            foreach (var s in _series)
            {
                if (s.Values.Count < 2) continue;

                var pts = new PointF[s.Values.Count];
                float stepX = plot.Width / (float)(Math.Max(2, _capacity - 1));
                float offset = plot.Width - (s.Values.Count - 1) * stepX;

                for (int i = 0; i < s.Values.Count; i++)
                {
                    double v = Math.Max(0, Math.Min(max, s.Values[i]));
                    float x = plot.Left + offset + i * stepX;
                    float y = plot.Bottom - (float)(v / max * plot.Height);
                    pts[i] = new PointF(x, y);
                }

                if (s.Fill)
                {
                    var poly = new List<PointF>(pts) { new(plot.Right, plot.Bottom), new(pts[0].X, plot.Bottom) };
                    using var fillBrush = new LinearGradientBrush(
                        plot, Theme.Alpha(s.Color, 90), Theme.Alpha(s.Color, 0), LinearGradientMode.Vertical);
                    g.FillPolygon(fillBrush, poly.ToArray());
                }

                using var pen = new Pen(s.Color, 1.8f) { LineJoin = LineJoin.Round };
                g.DrawLines(pen, pts);

                // leading dot
                var last = pts[pts.Length - 1];
                using var dot = new SolidBrush(s.Color);
                g.FillEllipse(dot, last.X - 3f, last.Y - 3f, 6f, 6f);
            }

            // axis caption
            using (var f = new Font("Segoe UI", 7.5f))
            using (var b = new SolidBrush(Theme.Muted))
                g.DrawString($"0 – {max:F0}{(Unit == "%" ? "%" : "")}", f, b, padL, Height - 16);
        }
    }
}
