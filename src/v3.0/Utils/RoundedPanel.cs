using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameOptimizer.Utils
{
    /// <summary>
    /// Панель с закруглёнными углами для современного дизайна
    /// </summary>
    public class RoundedPanel : Panel
    {
        private int _cornerRadius = 8;

        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = value;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            using (GraphicsPath path = CreateRoundedRectanglePath(
                new Rectangle(0, 0, this.Width - 1, this.Height - 1), _cornerRadius))
            {
                this.Region = new Region(path);
                
                // Рисуем грань панели
                using (Pen pen = new Pen(Color.FromArgb(50, 50, 55), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Переопределяем фон для правильного рендеринга с закруглёнными углами
            using (GraphicsPath path = CreateRoundedRectanglePath(
                new Rectangle(0, 0, this.Width, this.Height), _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            // Углы
            int x = rect.X;
            int y = rect.Y;
            int w = rect.Width;
            int h = rect.Height;
            int r = radius;

            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + w - r, y, r, r, 270, 90);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    /// <summary>
    /// Кнопка с закруглёнными углами
    /// </summary>
    public class RoundedButton : Button
    {
        private int _cornerRadius = 6;

        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = value;
                this.Invalidate();
            }
        }

        public RoundedButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            using (GraphicsPath path = CreateRoundedRectanglePath(
                new Rectangle(0, 0, this.Width, this.Height), _cornerRadius))
            {
                // Фон
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Текст
                TextRenderer.DrawText(e.Graphics, this.Text, this.Font, this.ClientRectangle,
                    this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Invalidate();
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int x = rect.X;
            int y = rect.Y;
            int w = rect.Width;
            int h = rect.Height;
            int r = radius;

            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + w - r, y, r, r, 270, 90);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
