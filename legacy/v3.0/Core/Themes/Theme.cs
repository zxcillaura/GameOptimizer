using System.Drawing;

namespace GameOptimizer.Core.Themes
{
    public abstract class Theme
    {
        public static Theme Current { get; set; } = new DarkTheme();

        // Primary colors
        public abstract Color Background { get; }
        public abstract Color Foreground { get; }
        public abstract Color Accent { get; }
        public abstract Color CardBackground { get; }
        
        // Secondary colors (Fluent Design)
        public abstract Color SidebarBackground { get; }
        public abstract Color SidebarHover { get; }
        public abstract Color SidebarActive { get; }
        public abstract Color BorderColor { get; }
        public abstract Color DisabledColor { get; }
        public abstract Color SuccessColor { get; }
        public abstract Color WarningColor { get; }
        public abstract Color ErrorColor { get; }
        public abstract Color InfoColor { get; }
    }

    public class DarkTheme : Theme
    {
        // Modern dark palette - Fluent Design
        public override Color Background => Color.FromArgb(20, 20, 22);           // Main background
        public override Color Foreground => Color.FromArgb(240, 240, 240);       // Text
        public override Color Accent => Color.FromArgb(90, 140, 255);            // Primary accent (blue)
        public override Color CardBackground => Color.FromArgb(32, 32, 36);      // Card/Panel background
        
        public override Color SidebarBackground => Color.FromArgb(18, 18, 20);   // Darker sidebar
        public override Color SidebarHover => Color.FromArgb(40, 40, 44);        // Hover effect
        public override Color SidebarActive => Color.FromArgb(90, 140, 255);     // Active item
        public override Color BorderColor => Color.FromArgb(50, 50, 55);         // Subtle border
        public override Color DisabledColor => Color.FromArgb(80, 80, 85);       // Disabled state
        public override Color SuccessColor => Color.FromArgb(52, 168, 83);       // Green
        public override Color WarningColor => Color.FromArgb(255, 179, 71);      // Orange
        public override Color ErrorColor => Color.FromArgb(240, 81, 81);         // Red
        public override Color InfoColor => Color.FromArgb(59, 130, 246);         // Cyan
    }

    public class LightTheme : Theme
    {
        // Modern light palette - Fluent Design
        public override Color Background => Color.FromArgb(252, 252, 252);       // Main background
        public override Color Foreground => Color.FromArgb(32, 32, 36);          // Text
        public override Color Accent => Color.FromArgb(0, 102, 204);             // Primary accent (blue)
        public override Color CardBackground => Color.FromArgb(243, 243, 244);   // Card/Panel background
        
        public override Color SidebarBackground => Color.FromArgb(242, 242, 245);// Light sidebar
        public override Color SidebarHover => Color.FromArgb(230, 230, 235);     // Hover effect
        public override Color SidebarActive => Color.FromArgb(0, 102, 204);      // Active item
        public override Color BorderColor => Color.FromArgb(220, 220, 225);      // Subtle border
        public override Color DisabledColor => Color.FromArgb(140, 140, 145);    // Disabled state
        public override Color SuccessColor => Color.FromArgb(16, 155, 74);       // Green
        public override Color WarningColor => Color.FromArgb(255, 140, 0);       // Orange
        public override Color ErrorColor => Color.FromArgb(229, 57, 53);         // Red
        public override Color InfoColor => Color.FromArgb(3, 155, 229);          // Cyan
    }
}
