using System;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.UI;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// FACEIT and Vanguard are the same kind of screen (status + checks + one
    /// fix button), so they live on one page behind a switch instead of eating
    /// two sidebar slots.
    /// </summary>
    public class AntiCheatControl : UserControl
    {
        private readonly FaceitControl _faceit = new();
        private readonly VanguardControl _vanguard = new();
        private FlatButton _faceitTab, _vanguardTab;
        private Panel _host;

        public AntiCheatControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var switcher = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _faceitTab = MakeTab("FACEIT Anti-Cheat", Theme.Warning, 0, 200);
            _vanguardTab = MakeTab("Riot Vanguard", Theme.Danger, 208, 176);
            switcher.Controls.Add(_faceitTab);
            switcher.Controls.Add(_vanguardTab);
            root.Controls.Add(switcher, 0, 0);

            _host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            _host.Controls.Add(_faceit);
            _host.Controls.Add(_vanguard);
            root.Controls.Add(_host, 0, 1);

            Select(0);
        }

        private FlatButton MakeTab(string text, Color accent, int left, int width)
        {
            var b = new FlatButton
            {
                Text = text, Style = FlatButtonStyle.Ghost, AccentColor = accent,
                Size = new Size(width, 32), Location = new Point(left, 4)
            };
            b.Click += (_, __) => Select(left == 0 ? 0 : 1);
            return b;
        }

        private void Select(int index)
        {
            _faceit.Visible = index == 0;
            _vanguard.Visible = index == 1;
            if (_faceit.Visible) _faceit.BringToFront(); else _vanguard.BringToFront();

            _faceitTab.Style   = index == 0 ? FlatButtonStyle.Soft : FlatButtonStyle.Ghost;
            _vanguardTab.Style = index == 1 ? FlatButtonStyle.Soft : FlatButtonStyle.Ghost;
            _faceitTab.Invalidate();
            _vanguardTab.Invalidate();

            DarkMode.ApplyRecursive(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) DarkMode.ApplyRecursive(this);
        }
    }
}
