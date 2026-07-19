using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GameOptimizer.Core.Managers;
using GameOptimizer.Core.Models;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.UserControls
{
    public partial class SystemControl : UserControl
    {
        private TweakEngine _tweakEngine;
        private List<TweakDefinition> _tweaks;
        private TableLayoutPanel _mainTable;
        private TextBox _logTextBox;

        public SystemControl()
        {
            InitializeComponent();
            this.BackColor = Theme.Current.Background;

            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tweaks.json");
            if (!File.Exists(jsonPath))
                jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tweaks.json");

            _tweakEngine = new TweakEngine(jsonPath);
            BuildUI();
            LoadAndRenderTweaks();
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.Current.Background;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 15, 20, 15)
            };
            mainPanel.RowStyles.Clear();
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

            // Заголовок
            var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Height = 80 };
            var title = new Label
            {
                Text = "⚙️ Оптимизация системы",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.Current.Foreground,
                Location = new Point(0, 0),
                AutoSize = true
            };
            headerPanel.Controls.Add(title);
            var desc = new Label
            {
                Text = "Включение и отключение системных параметров для повышения FPS.",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(0, 35),
                AutoSize = true
            };
            headerPanel.Controls.Add(desc);
            mainPanel.Controls.Add(headerPanel, 0, 0);

            // Таблица твиков
            _mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 5)
            };
            _mainTable.ColumnCount = 2;
            _mainTable.ColumnStyles.Clear();
            _mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            _mainTable.RowStyles.Clear();
            _mainTable.RowCount = 0;
            mainPanel.Controls.Add(_mainTable, 0, 1);

            // Лог
            var logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 17),
                Padding = new Padding(5)
            };
            _logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 15, 17),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };
            logPanel.Controls.Add(_logTextBox);
            mainPanel.Controls.Add(logPanel, 0, 2);

            this.Controls.Add(mainPanel);
            Log("Система твиков инициализирована. Готова к работе.");
        }

        private void LoadAndRenderTweaks()
        {
            _tweaks = _tweakEngine.LoadTweaks();
            var filtered = _tweaks.FindAll(t => t.Category != "Network");

            _mainTable.Controls.Clear();
            _mainTable.RowStyles.Clear();
            _mainTable.RowCount = 0;

            if (filtered.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "❌ Твики не найдены. Проверьте файл tweaks.json!",
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _mainTable.Controls.Add(emptyLabel, 0, 0);
                _mainTable.SetColumnSpan(emptyLabel, 2);
                _mainTable.RowCount = 1;
                _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
                return;
            }

            int rowIndex = 0;
            foreach (var tweak in filtered)
            {
                _mainTable.RowCount++;
                _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // высота строки 60px

                // Левая панель: название + категория + описание
                var leftPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(28, 28, 32),
                    Margin = new Padding(0, 2, 3, 2)
                };

                // Название
                var titleLabel = new Label
                {
                    Text = tweak.DisplayName,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(12, 4),
                    AutoSize = true
                };
                leftPanel.Controls.Add(titleLabel);

                // Категория + описание в одной строке
                var descLabel = new Label
                {
                    Text = $"[{tweak.Category}] {tweak.Description}",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(160, 160, 170),
                    Location = new Point(12, 26),
                    AutoSize = true,
                    MaximumSize = new Size(500, 0)
                };
                leftPanel.Controls.Add(descLabel);

                _mainTable.Controls.Add(leftPanel, 0, rowIndex);

                // Правая панель: кнопки
                var rightPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(28, 28, 32),
                    Margin = new Padding(3, 2, 0, 2)
                };

                bool isApplied = _tweakEngine.IsTweakApplied(tweak);
                int btnY = 14; // кнопки по центру (60 - 32 / 2)

                var btnApply = new Button
                {
                    Text = "Включить",
                    Size = new Size(85, 30),
                    Location = new Point(5, btnY),
                    BackColor = isApplied ? Color.FromArgb(40, 110, 40) : Color.FromArgb(50, 50, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold)
                };
                btnApply.FlatAppearance.BorderSize = 0;
                btnApply.Click += (s, e) =>
                {
                    Log($"Применяем твик: {tweak.DisplayName}...");
                    if (_tweakEngine.ApplyTweak(tweak))
                    {
                        Log($"[УСПЕХ] Твик \"{tweak.DisplayName}\" применён!");
                        btnApply.BackColor = Color.FromArgb(40, 110, 40);
                    }
                    else
                        Log($"[ОШИБКА] Не удалось применить твик. Нужен запуск от Администратора!");
                };
                rightPanel.Controls.Add(btnApply);

                var btnRevert = new Button
                {
                    Text = "Откатить",
                    Size = new Size(85, 30),
                    Location = new Point(95, btnY),
                    BackColor = Color.FromArgb(100, 35, 35),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold)
                };
                btnRevert.FlatAppearance.BorderSize = 0;
                btnRevert.Click += (s, e) =>
                {
                    Log($"Откатываем твик: {tweak.DisplayName}...");
                    if (_tweakEngine.RevertTweak(tweak))
                    {
                        Log($"[ОТКАТ] Твик \"{tweak.DisplayName}\" возвращён.");
                        btnApply.BackColor = Color.FromArgb(50, 50, 60);
                    }
                    else
                        Log($"[ОШИБКА] Не удалось откатить твик.");
                };
                rightPanel.Controls.Add(btnRevert);

                _mainTable.Controls.Add(rightPanel, 1, rowIndex);

                rowIndex++;
            }
        }

        private void Log(string message)
        {
            if (_logTextBox != null && !_logTextBox.IsDisposed)
                _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "SystemControl";
            this.Size = new Size(760, 540);
            this.ResumeLayout(false);
        }
    }
}