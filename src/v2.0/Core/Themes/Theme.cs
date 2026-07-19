using System.Drawing;

namespace GameOptimizer.Core.Themes
{
    public abstract class Theme
    {
        public static Theme Current { get; set; } = new DarkTheme();

        public abstract Color Background { get; }
        public abstract Color Foreground { get; }
        public abstract Color Accent { get; }
        public abstract Color CardBackground { get; }
    }

    public class DarkTheme : Theme
    {
        public override Color Background => Color.FromArgb(28, 28, 30);
        public override Color Foreground => Color.White;
        public override Color Accent => Color.FromArgb(80, 110, 240);
        public override Color CardBackground => Color.FromArgb(35, 35, 38);
    }

    public class LightTheme : Theme
    {
        public override Color Background => Color.FromArgb(245, 245, 247);
        public override Color Foreground => Color.Black;
        public override Color Accent => Color.FromArgb(0, 122, 255);
        public override Color CardBackground => Color.White;
    }
}