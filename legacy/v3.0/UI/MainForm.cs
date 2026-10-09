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

            // Сайдбар (боковая панель) - современный дизайн
            _sidebar = new Panel
            {
                Width = 200,
                Height = this.ClientSize.Height,
                BackColor = Theme.Current.SidebarBackground,
                Dock = DockStyle.Left
            };
            _sidebar.MouseDown += Form_MouseDown;
            _sidebar.MouseMove += Form_MouseMove;
            _sidebar.MouseUp += Form_MouseUp;
            this.Controls.Add(_sidebar);

            // Логотип - обновленный стиль
            Label logoLabel = new Label
            {
                Text = "GAME\nOPTIMIZER",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Theme.Current.Accent,
                Location = new Point(15, 15),
                AutoSize = false,
                Size = new Size(170, 40),
                TextAlign = ContentAlignment.TopLeft
            };
            Label subLogoLabel = new Label
            {
                Text = "by zxcillaura",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Theme.Current.DisabledColor,
                Location = new Point(17, 55),
                AutoSize = true
            };
            _sidebar.Controls.Add(logoLabel);
            _sidebar.Controls.Add(subLogoLabel);

            // Разделитель
            Panel separator = new Panel
            {
                Location = new Point(10, 70),
                Size = new Size(180, 1),
                BackColor = Theme.Current.BorderColor
            };
            _sidebar.Controls.Add(separator);

            // Главная панель для контента
            _contentPanel = new Panel
            {
                Location = new Point(200, 0),
                Size = new Size(this.ClientSize.Width - 200, this.ClientSize.Height),
                BackColor = Theme.Current.Background,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(_contentPanel);

            // Инициализация кнопок меню с новыми иконками и стилем
            _btnDashboard = CreateSidebarButton("Панель", 90, (s, e) => ShowControl(new DashboardControl(), _btnDashboard));
            _btnGames = CreateSidebarButton("Игры", 135, (s, e) => ShowControl(new GamesControl(), _btnGames));
            _btnSystem = CreateSidebarButton("Система", 180, (s, e) => ShowControl(new SystemControl(), _btnSystem));
            _btnNetwork = CreateSidebarButton("Сеть", 225, (s, e) => ShowControl(new NetworkControl(), _btnNetwork));
            _btnFaceit = CreateSidebarButton("FACEIT", 270, (s, e) => ShowControl(new FaceitControl(), _btnFaceit));
            _btnVanguard = CreateSidebarButton("Vanguard", 315, (s, e) => ShowControl(new VanguardControl(), _btnVanguard));
            _btnCleaner = CreateSidebarButton("Очистка", 360, (s, e) => ShowControl(new CleanerControl(), _btnCleaner));

            // Разделитель перед кнопками снизу
            Panel bottomSeparator = new Panel
            {
                Location = new Point(10, _sidebar.Height - 100),
                Size = new Size(180, 1),
                BackColor = Theme.Current.BorderColor
            };
            _sidebar.Controls.Add(bottomSeparator);

            // Кнопка смены темы
            _btnThemeToggle = new Button
            {
                Text = "Смена темы",
                Location = new Point(10, _sidebar.Height - 85),
                Size = new Size(180, 35),
                BackColor = Theme.Current.CardBackground,
                ForeColor = Theme.Current.Foreground,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnThemeToggle.FlatAppearance.BorderSize = 1;
            _btnThemeToggle.FlatAppearance.BorderColor = Theme.Current.BorderColor;
            _btnThemeToggle.Click += ToggleTheme;
            _sidebar.Controls.Add(_btnThemeToggle);

            // Кнопка закрытия приложения
            _btnClose = new Button
            {
                Text = "Выход",
                Location = new Point(10, _sidebar.Height - 40),
                Size = new Size(180, 30),
                BackColor = Theme.Current.ErrorColor,
                ForeColor = Color.White,
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
                BackColor = Theme.Current.SidebarBackground,
                ForeColor = Theme.Current.Foreground,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.Current.SidebarHover;
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
                _currentActiveButton.BackColor = Theme.Current.SidebarBackground;
                _currentActiveButton.ForeColor = Theme.Current.Foreground;
            }

            if (activeButton != null)
            {
                _currentActiveButton = activeButton;
                _currentActiveButton.BackColor = Theme.Current.SidebarActive;
                _currentActiveButton.ForeColor = Color.White;
            }
        }

        private void ToggleTheme(object sender, EventArgs e)
        {
            if (Theme.Current is DarkTheme)
                Theme.Current = new LightTheme();
            else
                Theme.Current = new DarkTheme();

            // Обновляем цвета формы и боковой панели
            this.BackColor = Theme.Current.Background;
            _contentPanel.BackColor = Theme.Current.Background;
            _sidebar.BackColor = Theme.Current.SidebarBackground;

            // Обновляем цвета кнопок
            _btnThemeToggle.BackColor = Theme.Current.CardBackground;
            _btnThemeToggle.ForeColor = Theme.Current.Foreground;
            _btnThemeToggle.FlatAppearance.BorderColor = Theme.Current.BorderColor;
            _btnClose.BackColor = Theme.Current.ErrorColor;

            // Обновляем цвета кнопок меню
            foreach (Control ctrl in _sidebar.Controls)
            {
                if (ctrl is Button btn && ctrl != _btnThemeToggle && ctrl != _btnClose)
                {
                    btn.BackColor = Theme.Current.SidebarBackground;
                    btn.ForeColor = Theme.Current.Foreground;
                    btn.FlatAppearance.MouseOverBackColor = Theme.Current.SidebarHover;
                }
                else if (ctrl is Panel pnl && pnl.Height == 1) // Разделители
                {
                    pnl.BackColor = Theme.Current.BorderColor;
                }
                else if (ctrl is Label lbl)
                {
                    if (lbl.Font.Size > 10) // Логотип
                        lbl.ForeColor = Theme.Current.Accent;
                    else // Подзаголовок
                        lbl.ForeColor = Theme.Current.DisabledColor;
                }
            }

            // Пересоздаём контрол для обновления цветов
            if (_currentActiveButton != null)
            {
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
