using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;

namespace GameOptimizer.UI
{
    /// <summary>
    /// A Panel that paints itself with rounded corners and an optional border.
    /// Used for info cards on the Dashboard and other pages.
    /// </summary>
    public class RoundedPanel : Panel
    {
        public int    CornerRadius { get; set; } = 8;
        public Color  BorderColor  { get; set; } = Theme.Border;
        public int    BorderWidth  { get; set; } = 1;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var backBrush = new SolidBrush(BackColor);
            g.FillRoundedRectangle(backBrush, 0, 0, Width - 1, Height - 1, CornerRadius);

            if (BorderWidth > 0)
            {
                using var pen = new Pen(BorderColor, BorderWidth);
                g.DrawRoundedRectangle(pen, 0, 0, Width - 1, Height - 1, CornerRadius);
            }
        }
    }
}
