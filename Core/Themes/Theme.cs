using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameOptimizer.Core.Themes
{
    /// <summary>
    /// GAMEOPTIMIZ v7 visual language.
    /// Every member name used by the v5/v6 pages is preserved, so all existing
    /// screens pick up the new palette automatically.
    /// </summary>
    public static class Theme
    {
        // ── surfaces (depth order: darker = further back) ────────────────
        public static readonly Color Background = Color.FromArgb(9,   11,  16);
        public static readonly Color Sidebar    = Color.FromArgb(13,  16,  22);
        public static readonly Color TitleBar   = Color.FromArgb(11,  13,  19);
        public static readonly Color Card       = Color.FromArgb(20,  24,  33);
        public static readonly Color Elevated   = Color.FromArgb(26,  31,  43);
        public static readonly Color Surface    = Color.FromArgb(34,  40,  54);
        public static readonly Color CardHover  = Color.FromArgb(30,  36,  49);
        public static readonly Color NavActive  = Color.FromArgb(22,  35,  58);
        public static readonly Color Hairline   = Color.FromArgb(30,  36,  48);

        // ── text ─────────────────────────────────────────────────────────
        public static readonly Color Text          = Color.FromArgb(238, 242, 248);
        public static readonly Color TextSecondary = Color.FromArgb(154, 166, 188);
        public static readonly Color Muted         = Color.FromArgb(107, 118, 136);

        // ── accents ──────────────────────────────────────────────────────
        public static readonly Color Accent        = Color.FromArgb(56,  189, 248);   // cyan
        public static readonly Color AccentHover   = Color.FromArgb(125, 211, 252);
        public static readonly Color AccentPressed = Color.FromArgb(14,  165, 233);
        public static readonly Color AccentAlt     = Color.FromArgb(167, 139, 250);   // violet
        public static readonly Color AccentPink    = Color.FromArgb(244, 114, 182);

        public static readonly Color HardColor = Color.FromArgb(239, 68,  68);
        public static readonly Color HardHover = Color.FromArgb(252, 105, 105);
        public static readonly Color SoftColor = Color.FromArgb(16,  185, 129);
        public static readonly Color SoftHover = Color.FromArgb(52,  211, 153);
        public static readonly Color GoldColor = Color.FromArgb(251, 191, 36);

        // ── status ───────────────────────────────────────────────────────
        public static readonly Color Success = Color.FromArgb(52,  211, 153);
        public static readonly Color Warning = Color.FromArgb(251, 191, 36);
        public static readonly Color Danger  = Color.FromArgb(248, 113, 113);
        public static readonly Color Info    = Color.FromArgb(56,  189, 248);

        // ── misc ─────────────────────────────────────────────────────────
        public static readonly Color Border  = Color.FromArgb(35,  42,  56);
        public static readonly Color LogBg   = Color.FromArgb(11,  15,  22);
        public static readonly Color LogFg   = Color.FromArgb(74,  222, 128);

        // ── gradients ────────────────────────────────────────────────────
        public static readonly Color GradientStart = Color.FromArgb(34,  211, 238);
        public static readonly Color GradientMid   = Color.FromArgb(129, 140, 248);
        public static readonly Color GradientEnd   = Color.FromArgb(167, 139, 250);

        // ── helpers ──────────────────────────────────────────────────────
        public static Color Alpha(Color c, int a) => Color.FromArgb(Math.Max(0, Math.Min(255, a)), c);

        public static Color Mix(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>Three-stop sweep used by every gauge and chart in v7.</summary>
        public static LinearGradientBrush Sweep(Rectangle r, float angle = 45f)
            => new LinearGradientBrush(r, GradientStart, GradientEnd, angle);

        /// <summary>Colour for a 0..100 load value.</summary>
        public static Color ForLoad(double percent)
        {
            if (percent < 55) return GradientStart;
            if (percent < 80) return Color.FromArgb(250, 204, 21);
            return HardColor;
        }

        public static Color ForScore(int score)
        {
            if (score >= 85) return Success;
            if (score >= 70) return Color.FromArgb(163, 230, 53);
            if (score >= 55) return Warning;
            return Danger;
        }

        public static void StyleButton(Button btn, bool secondary = false)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Cursor    = Cursors.Hand;

            if (secondary)
            {
                btn.BackColor = Surface;
                btn.ForeColor = TextSecondary;
                btn.FlatAppearance.BorderColor        = Border;
                btn.FlatAppearance.BorderSize         = 1;
                btn.FlatAppearance.MouseOverBackColor = Elevated;
                btn.FlatAppearance.MouseDownBackColor = Card;
            }
            else
            {
                btn.BackColor = Accent;
                btn.ForeColor = Color.FromArgb(6, 12, 20);
                btn.FlatAppearance.BorderSize         = 0;
                btn.FlatAppearance.MouseOverBackColor = AccentHover;
                btn.FlatAppearance.MouseDownBackColor = AccentPressed;
            }
        }

        // Backward-compat proxy
        public static readonly ThemeProxy Current = new();
    }

    public sealed class ThemeProxy
    {
        public Color Background        => Theme.Background;
        public Color Foreground        => Theme.Text;
        public Color Accent            => Theme.Accent;
        public Color CardBackground    => Theme.Card;
        public Color SidebarBackground => Theme.Sidebar;
        public Color SidebarHover      => Theme.Surface;
        public Color SidebarActive     => Theme.Accent;
        public Color BorderColor       => Theme.Border;
        public Color DisabledColor     => Theme.Muted;
        public Color SuccessColor      => Theme.Success;
        public Color WarningColor      => Theme.Warning;
        public Color ErrorColor        => Theme.Danger;
        public Color InfoColor         => Theme.Info;
        public Color LogBackground     => Theme.LogBg;
        public Color LogForeground     => Theme.LogFg;
        public Color HeaderBackground  => Theme.TitleBar;
    }
}
