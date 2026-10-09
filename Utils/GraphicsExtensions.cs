using System.Drawing;
using System.Drawing.Drawing2D;

// Extension methods on System.Drawing.Graphics for rounded rectangle helpers.
// Placed in a global-visible namespace so all UI files can consume them
// without extra `using` directives.
namespace GameOptimizer.Utils
{
    public static class GraphicsExtensions
    {
        /// <summary>Draws a rounded rectangle border.</summary>
        public static void DrawRoundedRectangle(this Graphics g, Pen pen,
            float x, float y, float width, float height, float radius)
        {
            using var path = BuildPath(x, y, width, height, radius);
            g.DrawPath(pen, path);
        }

        /// <summary>Fills a rounded rectangle.</summary>
        public static void FillRoundedRectangle(this Graphics g, Brush brush,
            float x, float y, float width, float height, float radius)
        {
            using var path = BuildPath(x, y, width, height, radius);
            g.FillPath(brush, path);
        }

        private static GraphicsPath BuildPath(float x, float y, float w, float h, float r)
        {
            float d = r * 2f;
            var path = new GraphicsPath();
            path.AddArc(x,         y,         d, d, 180, 90);
            path.AddArc(x + w - d, y,         d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
            path.AddArc(x,         y + h - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
