using System;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core;

namespace GameOptimizer.UI.UserControls
{
    public class GamesControl : UserControl
    {
        private Label _lblHeader;
        private TableLayoutPanel _mainLayout;
        private RichTextBox _txtConsole;

        public GamesControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Dock = DockStyle.Fill;

            _lblHeader = new Label
            {
                Text = "🎮 Оптимизация игр",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            _mainLayout = new TableLayoutPanel
            {
                Location = new Point(25, 75),
                Size = new Size(600, 280),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Карточка 1: Игровой режим Windows
            Panel cardGameMode = CreateCard(
                "РЕЖИМ WINDOWS",
                "Включает официальный игровой режим Microsoft и активирует HAGS для лучшего распределения ресурсов видеокарты.",
                "АКТИВИРОВАТЬ",
                Color.FromArgb(40, 120, 180),
                (s, e) => RunAction(() => OptimizerEngine.EnableWindowsGameMode(Log))
            );

            // Карточка 2: Очистка кэша шейдеров
            Panel cardShader = CreateCard(
                "КЭШ ШЕЙДЕРОВ",
                "Удаляет забитый кэш шейдеров NVIDIA и AMD. Помогает избавиться от внезапных микрофризов и статтеров в играх.",
                "ОЧИСТИТЬ КЭШ",
                Color.FromArgb(180, 110, 40),
                (s, e) => RunAction(() => OptimizerEngine.ClearShaderCache(Log))
            );

            _mainLayout.Controls.Add(cardGameMode, 0, 0);
            _mainLayout.Controls.Add(cardShader, 1, 0);

            _txtConsole = new RichTextBox
            {
                Location = new Point(25, 375),
                Size = new Size(600, 160),
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 9.5F),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            this.Controls.AddRange(new Control[] { _lblHeader, _mainLayout, _txtConsole });
            Log("🎮 Игровой оптимизатор готов к работе.");
        }

        private Panel CreateCard(string title, string description, string buttonText, Color themeColor, EventHandler onClick)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 28, 30),
                Margin = new Padding(6),
                Padding = new Padding(10)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = themeColor,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopCenter
            };

            Label lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(5)
            };

            Button btnAction = new Button
            {
                Text = buttonText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = themeColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 35
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += onClick;

            card.Controls.Add(lblDesc);
            card.Controls.Add(lblTitle);
            card.Controls.Add(btnAction);

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(35, 35, 38);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(28, 28, 30);

            return card;
        }

        private void Log(string message)
        {
            if (_txtConsole.InvokeRequired)
            {
                _txtConsole.Invoke(new Action(() => Log(message)));
                return;
            }
            _txtConsole.AppendText(message + Environment.NewLine);
            _txtConsole.SelectionStart = _txtConsole.Text.Length;
            _txtConsole.ScrollToCaret();
        }

        private void RunAction(Action action)
        {
            _mainLayout.Enabled = false;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log($"[ОШИБКА]: {ex.Message}");
                }
                finally
                {
                    this.Invoke(new Action(() => _mainLayout.Enabled = true));
                }
            });
        }
    }
}