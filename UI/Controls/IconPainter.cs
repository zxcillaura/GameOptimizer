using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GameOptimizer.UI.Controls
{
    /// <summary>
    /// Dependency-free vector icons so the sidebar looks designed rather than
    /// assembled from whatever glyphs a system font happens to contain.
    /// </summary>
    public static class IconPainter
    {
        public static void Draw(Graphics g, Rectangle r, string kind, Color color, float stroke = 1.7f)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap   = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var brush = new SolidBrush(color);

            float x = r.X, y = r.Y, w = r.Width, h = r.Height;
            float cx = x + w / 2f, cy = y + h / 2f;

            switch ((kind ?? "").ToLowerInvariant())
            {
                case "home":
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.10f, cy),
                        new PointF(cx, y + h * 0.12f),
                        new PointF(x + w * 0.90f, cy)
                    });
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.22f, cy - h * 0.02f),
                        new PointF(x + w * 0.22f, y + h * 0.88f),
                        new PointF(x + w * 0.78f, y + h * 0.88f),
                        new PointF(x + w * 0.78f, cy - h * 0.02f)
                    });
                    break;

                case "monitor":
                    g.DrawRectangle(pen, x + w * 0.10f, y + h * 0.16f, w * 0.80f, h * 0.54f);
                    g.DrawLine(pen, cx, y + h * 0.70f, cx, y + h * 0.86f);
                    g.DrawLine(pen, x + w * 0.30f, y + h * 0.86f, x + w * 0.70f, y + h * 0.86f);
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.24f, y + h * 0.56f),
                        new PointF(x + w * 0.38f, y + h * 0.38f),
                        new PointF(x + w * 0.50f, y + h * 0.50f),
                        new PointF(x + w * 0.72f, y + h * 0.26f)
                    });
                    break;

                case "gamepad":
                    g.DrawArc(pen, x + w * 0.06f, y + h * 0.30f, w * 0.88f, h * 0.52f, 180, 180);
                    g.DrawLine(pen, x + w * 0.14f, y + h * 0.56f, x + w * 0.34f, y + h * 0.56f);
                    g.DrawLine(pen, x + w * 0.66f, y + h * 0.56f, x + w * 0.86f, y + h * 0.56f);
                    g.FillEllipse(brush, x + w * 0.30f, y + h * 0.44f, w * 0.09f, h * 0.09f);
                    g.FillEllipse(brush, x + w * 0.62f, y + h * 0.44f, w * 0.09f, h * 0.09f);
                    break;

                case "chart":
                    g.DrawLine(pen, x + w * 0.10f, y + h * 0.86f, x + w * 0.90f, y + h * 0.86f);
                    g.DrawLine(pen, x + w * 0.10f, y + h * 0.14f, x + w * 0.10f, y + h * 0.86f);
                    using (var b = new SolidBrush(Color.FromArgb(70, color)))
                    {
                        g.FillRectangle(b, x + w * 0.22f, y + h * 0.52f, w * 0.16f, h * 0.34f);
                        g.FillRectangle(b, x + w * 0.45f, y + h * 0.34f, w * 0.16f, h * 0.52f);
                        g.FillRectangle(b, x + w * 0.68f, y + h * 0.44f, w * 0.16f, h * 0.42f);
                    }
                    break;

                case "bolt":
                    using (var path = new GraphicsPath())
                    {
                        path.AddPolygon(new[]
                        {
                            new PointF(cx + w * 0.06f, y + h * 0.08f),
                            new PointF(x + w * 0.22f, cy + h * 0.04f),
                            new PointF(cx - w * 0.02f, cy + h * 0.04f),
                            new PointF(cx + w * 0.10f, y + h * 0.92f),
                            new PointF(x + w * 0.78f, cy - h * 0.06f),
                            new PointF(cx + w * 0.02f, cy - h * 0.06f)
                        });
                        using var b = new SolidBrush(color);
                        g.FillPath(b, path);
                    }
                    break;

                case "wrench":
                    g.DrawEllipse(pen, x + w * 0.10f, y + h * 0.10f, w * 0.42f, h * 0.42f);
                    g.DrawLine(pen, cx, cy, x + w * 0.86f, y + h * 0.86f);
                    g.DrawEllipse(pen, x + w * 0.62f, y + h * 0.62f, w * 0.30f, h * 0.30f);
                    break;

                case "network":
                    g.DrawEllipse(pen, cx - w * 0.12f, cy - h * 0.12f, w * 0.24f, h * 0.24f);
                    g.DrawEllipse(pen, x + w * 0.04f, y + h * 0.06f, w * 0.20f, h * 0.20f);
                    g.DrawEllipse(pen, x + w * 0.76f, y + h * 0.06f, w * 0.20f, h * 0.20f);
                    g.DrawEllipse(pen, x + w * 0.40f, y + h * 0.74f, w * 0.20f, h * 0.20f);
                    g.DrawLine(pen, cx - w * 0.10f, cy - h * 0.10f, x + w * 0.18f, y + h * 0.20f);
                    g.DrawLine(pen, cx + w * 0.10f, cy - h * 0.10f, x + w * 0.82f, y + h * 0.20f);
                    g.DrawLine(pen, cx, cy + h * 0.12f, cx, y + h * 0.76f);
                    break;

                case "broom":
                    g.DrawLine(pen, x + w * 0.78f, y + h * 0.14f, x + w * 0.34f, y + h * 0.58f);
                    using (var path = new GraphicsPath())
                    {
                        path.AddPolygon(new[]
                        {
                            new PointF(x + w * 0.32f, y + h * 0.52f),
                            new PointF(x + w * 0.10f, y + h * 0.90f),
                            new PointF(x + w * 0.54f, y + h * 0.66f)
                        });
                        using var b = new SolidBrush(Color.FromArgb(150, color));
                        g.FillPath(b, path);
                    }
                    break;

                case "shield":
                    using (var path = new GraphicsPath())
                    {
                        path.AddLines(new[]
                        {
                            new PointF(cx, y + h * 0.10f),
                            new PointF(x + w * 0.84f, y + h * 0.26f),
                            new PointF(x + w * 0.80f, cy + h * 0.14f),
                            new PointF(cx, y + h * 0.90f),
                            new PointF(x + w * 0.20f, cy + h * 0.14f),
                            new PointF(x + w * 0.16f, y + h * 0.26f)
                        });
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.36f, cy),
                        new PointF(x + w * 0.46f, cy + h * 0.10f),
                        new PointF(x + w * 0.66f, cy - h * 0.12f)
                    });
                    break;

                case "sliders":
                    g.DrawLine(pen, x + w * 0.16f, y + h * 0.24f, x + w * 0.84f, y + h * 0.24f);
                    g.DrawLine(pen, x + w * 0.16f, cy,           x + w * 0.84f, cy);
                    g.DrawLine(pen, x + w * 0.16f, y + h * 0.76f, x + w * 0.84f, y + h * 0.76f);
                    g.FillEllipse(brush, x + w * 0.34f, y + h * 0.16f, w * 0.18f, h * 0.16f);
                    g.FillEllipse(brush, x + w * 0.58f, y + h * 0.68f, w * 0.18f, h * 0.16f);
                    g.FillEllipse(brush, x + w * 0.24f, cy - h * 0.08f, w * 0.18f, h * 0.16f);
                    break;

                default:
                    g.DrawEllipse(pen, x + w * 0.16f, y + h * 0.16f, w * 0.68f, h * 0.68f);
                    break;
            }

            g.SmoothingMode = old;
        }
    }
}
