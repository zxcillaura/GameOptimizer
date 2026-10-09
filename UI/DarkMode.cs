using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameOptimizer.UI
{
    /// <summary>
    /// Turns the Windows-drawn chrome of individual controls dark: scrollbars,
    /// ListView headers and edit borders. Without this a dark app still shows
    /// white scrollbars everywhere.
    /// </summary>
    public static class DarkMode
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DwmwaUseImmersiveDarkMode       = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;

        /// <summary>Applies the dark explorer theme to a control's own window chrome.</summary>
        public static void Apply(Control control)
        {
            if (control == null) return;
            try
            {
                if (!control.IsHandleCreated) return;
                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch { }
        }

        /// <summary>Dark title bar for the window itself (works on dialogs too).</summary>
        public static void ApplyWindow(Form form)
        {
            if (form == null) return;
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref on, sizeof(int)) != 0)
                    DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkModeLegacy, ref on, sizeof(int));
            }
            catch { }
        }

        /// <summary>Walks the tree and darkens everything that owns system chrome.</summary>
        public static void ApplyRecursive(Control root, int depth = 0)
        {
            if (root == null || depth > 8) return;
            Apply(root);
            try
            {
                foreach (Control child in root.Controls)
                    ApplyRecursive(child, depth + 1);
            }
            catch { }
        }
    }
}
