using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.Utils;
using GameOptimizer.UI.Controls;
using GameOptimizer.UI.UserControls;

namespace GameOptimizer.UI
{
    public class MainForm : Form
    {
        private Panel _root, _sidebar, _content, _pageHost, _titleBar, _logoPanel, _navPanel, _sidebarFooter;
        private Label _titleLabel, _crumbLabel, _footerLabel, _logoLabel, _logoSub;
        private FlatButton _minBtn, _maxBtn, _closeBtn;
        private NavItem[] _nav;
        private UserControl[] _pages;

        // sidebar is grouped: overview -> what you play -> what you tune -> protection.
        // one page per job, no duplicates: the old v5 "Дашборд" (same thing as
        // Главная) and the separate FACEIT / Vanguard pages are gone.
        private readonly (string Icon, string Name, string Group)[] _navItems =
        {
            ("home",     "Главная",      "ОБЗОР"),
            ("gamepad",  "Игры",         "ИГРЫ"),
            ("chart",    "Бенчмарк",     "ИГРЫ"),
            ("monitor",  "Монитор",      "СИСТЕМА"),
            ("bolt",     "Профили",      "СИСТЕМА"),
            ("wrench",   "Центр твиков", "СИСТЕМА"),
            ("network",  "Сеть",         "СИСТЕМА"),
            ("mouse",    "Мышь и клава", "СИСТЕМА"),
            ("broom",    "Очиститель",   "СИСТЕМА"),
            ("shield",   "Анти-чит",     "БЕЗОПАСНОСТЬ"),
        };

        private readonly string[] _crums =
        {
            "оценка системы, ограничители, конфигурация",
            "запуск и чистый игровой режим",
            "frametime, 1% low, история замеров",
            "телеметрия в реальном времени",
            "три профиля, откат и журнал проходов",
            "каждый твик отдельно: состояние, риск, откат",
            "адаптер, DNS, сброс стека",
            "инпут-лаг, мышь, клавиатура",
            "кэши, temp, список ожидания",
            "FACEIT Anti-Cheat и Riot Vanguard",
        };

        private int _active = -1;

        public MainForm(int startTab = 0)
        {
            InitializeForm();
            BuildLayout();
            NavigateTo(Math.Max(0, Math.Min(_navItems.Length - 1, startTab)));
            if (!SystemTweaks.IsAdmin())
                Toast.Show(this, "Нет прав администратора — твики не применятся", Theme.Danger, 6000);
        }

        private void InitializeForm()
        {
            Text            = "GAMEOPTIMIZ 1.0";
            Size            = new Size(1260, 820);
            MinimumSize     = new Size(1120, 720);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            AutoScaleMode   = AutoScaleMode.Dpi;
            Font            = new Font("Segoe UI", 9f);
            DoubleBuffered  = true;
            BackColor       = Theme.Background;

            // a borderless form maximises over the taskbar unless the working area is set
            Resize += (_, __) => ApplyMaximizedBounds();
            FormClosing += (_, __) => { try { _slideTimer?.Stop(); _slideTimer?.Dispose(); } catch { } };
            Shown += (_, __) =>
            {
                ApplyMaximizedBounds();
                // dark scrollbars / list headers for every control that owns system chrome
                DarkMode.ApplyWindow(this);
                DarkMode.ApplyRecursive(this);
            };
        }

        private void ApplyMaximizedBounds()
        {
            try
            {
                var area = Screen.FromControl(this).WorkingArea;
                if (MaximizedBounds != area) MaximizedBounds = area;
            }
            catch { }
        }

        private void BuildLayout()
        {
            _root = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            Controls.Add(_root);

            // ── sidebar ──────────────────────────────────────────────────
            _sidebar = new Panel { Dock = DockStyle.Left, Width = 218, BackColor = Theme.Sidebar, Padding = new Padding(10, 0, 10, 0) };

            _logoPanel = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.Transparent };
            _logoPanel.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var box = new Rectangle(10, 22, 30, 30);
                using (var b = new LinearGradientBrush(box, Theme.GradientStart, Theme.GradientEnd, 45f))
                    g.FillRoundedRectangle(b, box.X, box.Y, box.Width, box.Height, 9);
                IconPainter.Draw(g, new Rectangle(box.X + 7, box.Y + 7, 16, 16), "bolt",
                                 Color.FromArgb(10, 14, 22), 2f);
            };
            _logoLabel = new Label
            {
                Text = "GAMEOPTIMIZ",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.Text, BackColor = Color.Transparent,
                Location = new Point(48, 22), AutoSize = true
            };
            _logoSub = new Label
            {
                Text = "1.0  •  by zxcillaura",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.Accent, BackColor = Color.Transparent,
                Location = new Point(49, 42), AutoSize = true
            };
            _logoPanel.Controls.Add(_logoLabel);
            _logoPanel.Controls.Add(_logoSub);

            _navPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _nav = new NavItem[_navItems.Length];

            var stack = new List<Control>();
            string lastGroup = null;
            for (int i = 0; i < _navItems.Length; i++)
            {
                if (_navItems[i].Group != lastGroup)
                {
                    lastGroup = _navItems[i].Group;
                    stack.Add(new Label
                    {
                        Text = lastGroup, Dock = DockStyle.Top, Height = 26,
                        ForeColor = Theme.Muted, BackColor = Color.Transparent,
                        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                        TextAlign = ContentAlignment.BottomLeft,
                        Padding = new Padding(14, 0, 0, 5)
                    });
                }

                int idx = i;
                var item = new NavItem(_navItems[i].Name, _navItems[i].Icon)
                {
                    Dock = DockStyle.Top, Height = 40, Tag = idx
                };
                item.Click += (_, __) => NavigateTo(idx);
                _nav[i] = item;
                stack.Add(item);
            }
            // Dock.Top stacks in reverse add order
            for (int i = stack.Count - 1; i >= 0; i--) _navPanel.Controls.Add(stack[i]);

            _sidebarFooter = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = Color.Transparent };
            _sidebarFooter.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool admin = SystemTweaks.IsAdmin();
                using var b = new SolidBrush(Theme.Alpha(admin ? Theme.Success : Theme.Danger, 30));
                g.FillRoundedRectangle(b, 0, 8, _sidebarFooter.Width - 20, 26, 8);
                using var p = new Pen(Theme.Alpha(admin ? Theme.Success : Theme.Danger, 120));
                g.DrawRoundedRectangle(p, 0, 8, _sidebarFooter.Width - 20, 26, 8);
                using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
                using var tb = new SolidBrush(admin ? Theme.Success : Theme.Danger);
                g.DrawString(admin ? "АДМИНИСТРАТОР" : "НЕТ ПРАВ", f, tb, 10, 14);
            };
            _footerLabel = new Label
            {
                Text = "бэкап при каждом проходе",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Theme.Muted, BackColor = Color.Transparent,
                Location = new Point(10, 40), AutoSize = true
            };
            _sidebarFooter.Controls.Add(_footerLabel);

            _sidebar.Controls.Add(_navPanel);
            _sidebar.Controls.Add(_sidebarFooter);
            _sidebar.Controls.Add(_logoPanel);

            // ── content ──────────────────────────────────────────────────
            _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };

            _titleBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Background };
            _titleLabel = new Label
            {
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.Text, BackColor = Color.Transparent,
                Location = new Point(20, 8), AutoSize = true, Text = "Главная"
            };
            _crumbLabel = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.Muted, BackColor = Color.Transparent,
                Location = new Point(21, 31), AutoSize = true, Text = ""
            };

            _closeBtn = MakeChromeButton("✕", false);
            _maxBtn   = MakeChromeButton("▢", true);
            _minBtn   = MakeChromeButton("—", true);
            _closeBtn.Click += (_, __) => Application.Exit();
            _maxBtn.Click   += (_, __) => WindowState = WindowState == FormWindowState.Maximized
                                          ? FormWindowState.Normal : FormWindowState.Maximized;
            _minBtn.Click   += (_, __) => WindowState = FormWindowState.Minimized;

            _titleBar.Controls.Add(_titleLabel);
            _titleBar.Controls.Add(_crumbLabel);
            _titleBar.Controls.Add(_closeBtn);
            _titleBar.Controls.Add(_maxBtn);
            _titleBar.Controls.Add(_minBtn);
            LayoutChrome();
            _titleBar.Resize += (_, __) => LayoutChrome();

            _titleBar.MouseDown += DragStart;
            _titleBar.MouseMove += DragMove;
            _titleBar.MouseUp   += (_, __) => _dragging = false;
            _titleLabel.MouseDown += DragStart;
            _titleLabel.MouseMove += DragMove;
            _titleLabel.MouseUp   += (_, __) => _dragging = false;
            _titleBar.DoubleClick += (_, __) => WindowState = WindowState == FormWindowState.Maximized
                                                 ? FormWindowState.Normal : FormWindowState.Maximized;

            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };

            _pages = new UserControl[]
            {
                new HomeControl(),
                new GamesControl(),
                new BenchControl(),
                new MonitorControl(),
                new AutoControl(),
                new TweakCenterControl(),
                new NetworkControl(),
                new InputControl(),
                new CleanerControl(),
                new AntiCheatControl(),
            };

            // the overview page links into the action pages instead of duplicating their buttons
            if (_pages[0] is HomeControl home) home.NavigateRequested = NavigateTo;

            foreach (var p in _pages)
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                _pageHost.Controls.Add(p);
            }

            _content.Controls.Add(_pageHost);
            _content.Controls.Add(_titleBar);
            // dock order matters: the sidebar is added last so it claims the left
            // edge first and the fill panel takes the remainder. Never BringToFront
            // a docked sibling here — it reverses the layout order.
            _root.Controls.Add(_content);
            _root.Controls.Add(_sidebar);
        }

        private FlatButton MakeChromeButton(string glyph, bool secondary)
        {
            var b = new FlatButton
            {
                Text = glyph, Style = secondary ? FlatButtonStyle.Ghost : FlatButtonStyle.Ghost,
                Size = new Size(42, 30), Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                AccentColor = secondary ? Theme.TextSecondary : Theme.Danger
            };
            return b;
        }

        private void LayoutChrome()
        {
            int right = _titleBar.ClientSize.Width - 10;
            foreach (var b in new[] { _closeBtn, _maxBtn, _minBtn })
            {
                if (b == null) continue;
                right -= b.Width + 4;
                b.Location = new Point(right, 11);
            }
        }

        private bool _dragging;
        private Point _dragStart;

        private void DragStart(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragStart = e.Location;
        }

        private void DragMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                return;
            }
            Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y);
        }

        private Timer _slideTimer;

        private void NavigateTo(int index)
        {
            _active = index;

            for (int i = 0; i < _pages.Length; i++)
            {
                bool show = (i == index);
                _pages[i].Visible = show;
                if (show) _pages[i].BringToFront();
            }
            for (int i = 0; i < _nav.Length; i++)
            {
                _nav[i].Active = (i == index);
                _nav[i].Invalidate();
            }

            _titleLabel.Text = _navItems[index].Name;
            _crumbLabel.Text = index < _crums.Length ? _crums[index] : "";

            // subtle slide-in for the incoming page
            _slideTimer?.Stop();
            int pad = 22;
            _pageHost.Padding = new Padding(pad, 0, 0, 0);
            _slideTimer = new Timer { Interval = 16 };
            int frames = 0;
            _slideTimer.Tick += (_, __) =>
            {
                frames++;
                pad = Math.Max(0, pad - 4);
                _pageHost.Padding = new Padding(pad, 0, 0, 0);
                if (pad == 0 || frames > 12) _slideTimer.Stop();
            };
            _slideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Theme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                int x = unchecked((short)(long)m.LParam);
                int y = unchecked((short)((long)m.LParam >> 16));
                var p = PointToClient(new Point(x, y));
                const int grip = 7;

                bool left = p.X <= grip, right = p.X >= ClientSize.Width - grip;
                bool top = p.Y <= grip, bottom = p.Y >= ClientSize.Height - grip;

                if (left && top)         m.Result = (IntPtr)13;
                else if (right && top)   m.Result = (IntPtr)14;
                else if (left && bottom) m.Result = (IntPtr)16;
                else if (right && bottom)m.Result = (IntPtr)17;
                else if (left)           m.Result = (IntPtr)10;
                else if (right)          m.Result = (IntPtr)11;
                else if (top)            m.Result = (IntPtr)12;
                else if (bottom)         m.Result = (IntPtr)15;
                return;
            }
            base.WndProc(ref m);
        }

        /// <summary>Sidebar entry: vector icon, animated hover, accent pill when active.</summary>
        private sealed class NavItem : Control
        {
            private readonly Timer _anim;
            private double _hover;
            private bool _isHover;

            public bool Active { get; set; }
            public string IconKind { get; set; } = "home";

            public NavItem(string text, string icon)
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint  |
                         ControlStyles.UserPaint             |
                         ControlStyles.SupportsTransparentBackColor, true);
                Text = text;
                IconKind = icon;
                BackColor = Color.Transparent;
                Font = new Font("Segoe UI", 9.5f);
                Cursor = Cursors.Hand;

                _anim = new Timer { Interval = 16 };
                _anim.Tick += (_, __) =>
                {
                    double target = _isHover ? 1 : 0;
                    double diff = target - _hover;
                    if (Math.Abs(diff) < 0.05) { _hover = target; _anim.Stop(); }
                    else _hover += diff * 0.28;
                    Invalidate();
                };
            }

            protected override void OnMouseEnter(EventArgs e) { _isHover = true;  _anim.Start(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _isHover = false; _anim.Start(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var r = new Rectangle(2, 3, Width - 6, Height - 6);

                if (Active)
                {
                    using var b = new LinearGradientBrush(r,
                        Theme.Alpha(Theme.Accent, 34), Theme.Alpha(Theme.AccentAlt, 26), 0f);
                    g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, 9);
                    using var p = new Pen(Theme.Alpha(Theme.Accent, 90));
                    g.DrawRoundedRectangle(p, r.X, r.Y, r.Width, r.Height, 9);
                    using var bar = new SolidBrush(Theme.Accent);
                    g.FillRoundedRectangle(bar, r.X + 1, r.Y + 8, 3, r.Height - 16, 2);
                }
                else if (_hover > 0.02)
                {
                    using var b = new SolidBrush(Theme.Alpha(Theme.Surface, (int)(120 * _hover)));
                    g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, 9);
                }

                Color fg = Active ? Theme.Text
                          : Theme.Mix(Theme.TextSecondary, Theme.Text, _hover);
                IconPainter.Draw(g, new Rectangle(14, (Height - 17) / 2, 17, 17), IconKind,
                                 Active ? Theme.Accent : fg, 1.6f);

                using (var f = new Font("Segoe UI", 9.5f, Active ? FontStyle.Bold : FontStyle.Regular))
                using (var tb = new SolidBrush(fg))
                using (var fmt = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString(Text, f, tb, new RectangleF(42, 0, Width - 50, Height), fmt);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _anim?.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
