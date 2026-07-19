using System;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.UI.UserControls;

namespace GameOptimizer.UI
{
    public partial class MainForm : Form
    {
        private Panel _sidebar;
        private Panel _contentPanel;
        
        // Кнопки меню
        private Button _btnDashboard;
        private Button _btnGames; 
        private Button _btnSystem;
        private Button _btnNetwork;
        private Button _btnFaceit;
        private Button _btnVanguard;
        private Button _btnCleaner;
        private Button _btnThemeToggle;
        private Button _btnClose;

        private Button _currentActiveButton;

        // Перетаскивание окна мышкой
        private bool isDragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public MainForm()
        {
            this.Text = "Игровой помощник zxcillaura";
            this.Size = new Size(980, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;

            // Устанавливаем тему по умолчанию
            if (Theme.Current == null)
                Theme.Current = new DarkTheme();

            BuildUI();
            ShowControl(new DashboardControl(), _btnDashboard);
        }

        private void BuildUI()
        {
            this.BackColor = Theme.Current.Background;

            // Сайдбар (боковая панель)
            _sidebar = new Panel
            {
                Width = 200,
                Height = this.ClientSize.Height,
                BackColor = Color.FromArgb(20, 20, 22),
                Dock = DockStyle.Left
            };
            _sidebar.MouseDown += Form_MouseDown;
            _sidebar.MouseMove += Form_MouseMove;
            _sidebar.MouseUp += Form_MouseUp;
            this.Controls.Add(_sidebar);

            // Логотип
            Label logoLabel = new Label
            {
                Text = "zxcillaura",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 110, 240),
                Location = new Point(15, 12),
                AutoSize = true
            };
            Label subLogoLabel = new Label
            {
                Text = "игровой помощник",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(17, 38),
                AutoSize = true
            };
            _sidebar.Controls.Add(logoLabel);
            _sidebar.Controls.Add(subLogoLabel);

            // Главная панель для контента
            _contentPanel = new Panel
            {
                Location = new Point(200, 0),
                Size = new Size(this.ClientSize.Width - 200, this.ClientSize.Height),
                BackColor = Theme.Current.Background,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(_contentPanel);

            // Инициализация кнопок меню
            _btnDashboard = CreateSidebarButton("📊 Панель", 70, (s, e) => ShowControl(new DashboardControl(), _btnDashboard));
            _btnGames = CreateSidebarButton("🎮 Игры", 115, (s, e) => ShowControl(new GamesControl(), _btnGames));
            _btnSystem = CreateSidebarButton("⚙️ Система", 160, (s, e) => ShowControl(new SystemControl(), _btnSystem));
            _btnNetwork = CreateSidebarButton("🌐 Сеть", 205, (s, e) => ShowControl(new NetworkControl(), _btnNetwork));
            _btnFaceit = CreateSidebarButton("🛡️ FACEIT AC", 250, (s, e) => ShowControl(new FaceitControl(), _btnFaceit));
            _btnVanguard = CreateSidebarButton("❤️ Vanguard", 295, (s, e) => ShowControl(new VanguardControl(), _btnVanguard));
            _btnCleaner = CreateSidebarButton("🧹 Очистка", 340, (s, e) => ShowControl(new CleanerControl(), _btnCleaner));

            // Кнопка смены темы
            _btnThemeToggle = new Button
            {
                Text = "🌓 Сменить тему",
                Location = new Point(10, _sidebar.Height - 90),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnThemeToggle.FlatAppearance.BorderSize = 0;
            _btnThemeToggle.Click += ToggleTheme;
            _sidebar.Controls.Add(_btnThemeToggle);

            // Кнопка закрытия приложения
            _btnClose = new Button
            {
                Text = "❌ Выход",
                Location = new Point(10, _sidebar.Height - 45),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(50, 20, 20),
                ForeColor = Color.FromArgb(240, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Application.Exit();
            _sidebar.Controls.Add(_btnClose);

            this.MouseDown += Form_MouseDown;
            this.MouseMove += Form_MouseMove;
            this.MouseUp += Form_MouseUp;
        }

        private Button CreateSidebarButton(string text, int yOffset, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, yOffset),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(20, 20, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 35);
            btn.Click += clickHandler;
            _sidebar.Controls.Add(btn);
            return btn;
        }

        private void ShowControl(UserControl control, Button activeButton = null)
        {
            // Очищаем панель
            _contentPanel.Controls.Clear();
            
            // Добавляем новый контрол
            control.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(control);

            // Подсвечиваем активную кнопку
            if (_currentActiveButton != null)
            {
                _currentActiveButton.BackColor = Color.FromArgb(20, 20, 22);
                _currentActiveButton.ForeColor = Color.White;
            }

            if (activeButton != null)
            {
                _currentActiveButton = activeButton;
                _currentActiveButton.BackColor = Theme.Current.Accent;
                _currentActiveButton.ForeColor = Color.White;
            }
        }

        private void ToggleTheme(object sender, EventArgs e)
        {
            if (Theme.Current is DarkTheme)
                Theme.Current = new LightTheme();
            else
                Theme.Current = new DarkTheme();

            // Обновляем цвета формы
            this.BackColor = Theme.Current.Background;
            _contentPanel.BackColor = Theme.Current.Background;

            // Обновляем цвета всех контролов на активной вкладке
            if (_currentActiveButton != null)
            {
                // Пересоздаём контрол для обновления цветов
                if (_currentActiveButton == _btnDashboard) ShowControl(new DashboardControl(), _btnDashboard);
                else if (_currentActiveButton == _btnGames) ShowControl(new GamesControl(), _btnGames);
                else if (_currentActiveButton == _btnSystem) ShowControl(new SystemControl(), _btnSystem);
                else if (_currentActiveButton == _btnNetwork) ShowControl(new NetworkControl(), _btnNetwork);
                else if (_currentActiveButton == _btnFaceit) ShowControl(new FaceitControl(), _btnFaceit);
                else if (_currentActiveButton == _btnVanguard) ShowControl(new VanguardControl(), _btnVanguard);
                else if (_currentActiveButton == _btnCleaner) ShowControl(new CleanerControl(), _btnCleaner);
            }
        }

        // ============================================================
        // ПЕРЕТАСКИВАНИЕ ОКНА
        // ============================================================

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
    }
}