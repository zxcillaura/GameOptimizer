using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FormsTimer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI
{
    /// <summary>
    /// Circular performance-score gauge with a glowing arc, a big center value
    /// and a small caption. Used on the Dashboard hero to replace flat text.
    /// </summary>
    public class GaugeControl : Control
    {
        private int _value;
        private int _displayValue;
        private string _caption = "PERFORMANCE SCORE";
        private Color _trackColor = Theme.Border;
        private readonly FormsTimer _animTimer;

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                _animTimer.Stop();
                _animTimer.Start();
            }
        }

        public string Caption { get => _caption; set { _caption = value; Invalidate(); } }

        public GaugeControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            // WinForms Control does not support transparent BackColor here.
            // Use the themed surface color so startup is reliable on Windows 10/11.
            BackColor = Theme.Card;
            Size = new Size(150, 150);
            _animTimer = new FormsTimer { Interval = 12 };
            _animTimer.Tick += (_, _) =>
            {
                if (_displayValue == _value) { _animTimer.Stop(); return; }
                _displayValue += _displayValue < _value ? 1 : -1;
                Invalidate();
            };
        }

        private Color ScoreColor(int v) => v >= 75 ? Theme.Success : v >= 45 ? Theme.Warning : Theme.Danger;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(10, 10, Width - 20, Height - 20);
            const float startAngle = 140f;
            const float sweep = 260f;

            using var trackPen = new Pen(_trackColor, 10) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(trackPen, rect, startAngle, sweep);

            if (_displayValue > 0)
            {
                var color = ScoreColor(_displayValue);
                var valueSweep = sweep * (_displayValue / 100f);
                using var glowPen = new Pen(Color.FromArgb(70, color), 16) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(glowPen, rect, startAngle, valueSweep);
                using var valuePen = new Pen(color, 10) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(valuePen, rect, startAngle, valueSweep);
            }

            var numberText = _displayValue.ToString();
            using var numberFont = new Font("Segoe UI", Height / 5.2f, FontStyle.Bold);
            using var numberBrush = new SolidBrush(Theme.Text);
            var numberSize = g.MeasureString(numberText, numberFont);
            g.DrawString(numberText, numberFont, numberBrush, Width / 2f - numberSize.Width / 2f, Height / 2f - numberSize.Height / 1.6f);

            using var captionFont = new Font("Segoe UI", 7f, FontStyle.Bold);
            using var captionBrush = new SolidBrush(Theme.Muted);
            var captionSize = g.MeasureString(_caption, captionFont);
            g.DrawString(_caption, captionFont, captionBrush, Width / 2f - captionSize.Width / 2f, Height / 2f + numberSize.Height / 2.4f);
        }
    }
}
