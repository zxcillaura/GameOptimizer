using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI
{
    /// <summary>
    /// Shared factory helpers for recurring UI elements across all pages.
    /// </summary>
    public static class UIHelpers
    {
        public static Label MakeSectionTitle(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Top,
                Height    = 34,
                TextAlign = ContentAlignment.BottomLeft,
                Margin    = new Padding(0, 0, 0, 8)
            };
        }

        public static Label MakeSub(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 9),
                ForeColor = Theme.Muted,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.TopLeft,
                Margin    = new Padding(0, 0, 0, 4)
            };
        }

        public static Panel MakeSeparator()
        {
            return new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Theme.Border,
                Margin    = new Padding(0, 8, 0, 8)
            };
        }
    }
}
