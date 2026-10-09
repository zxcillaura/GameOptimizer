using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using Microsoft.Win32;

namespace GameOptimizer.UI.UserControls
{
    public partial class FaceitControl : UserControl
    {
        private Label _statusLabel;
        private RichTextBox _logText;

        public FaceitControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(18, 19, 22);
            BuildUI();
            CheckServiceStatus();
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.BackColor = Color.FromArgb(18, 19, 22);

            // Заголовок
            var title = new Label
            {
                Text = "🧡 УПРАВЛЕНИЕ ДЛЯ FACEIT ANTI-CHEAT",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(title);

            // Статус службы
            _statusLabel = new Label
            {
                Text = "🔄 Статус FACEIT AC: ПРОВЕРКА...",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(20, 55),
                AutoSize = true
            };
            this.Controls.Add(_statusLabel);

            // Блок: Управление службой
            var groupService = new GroupBox
            {
                Text = "⚙️ Управление службой FACEIT",
                ForeColor = Color.LightGray,
                Location = new Point(20, 85),
                Size = new Size(620, 80),
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(groupService);

            // Кнопка Включить автозапуск
            var btnStart = new Button
            {
                Text = "✅ Включить автозапуск AC",
                Size = new Size(220, 35),
                Location = new Point(15, 25),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => RunCommand("sc config FACEIT start= auto && sc start FACEIT", "✅ FACEIT AC включен в автозапуск и запущен.");
            groupService.Controls.Add(btnStart);

            // Кнопка Отключить автозапуск
            var btnStop = new Button
            {
                Text = "❌ Отключить автозапуск AC",
                Size = new Size(220, 35),
                Location = new Point(250, 25),
                BackColor = Color.FromArgb(200, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += (s, e) => RunCommand("sc config FACEIT start= demand && sc stop FACEIT", "⏹️ FACEIT AC остановлен и переведён в ручной режим.");
            groupService.Controls.Add(btnStop);

            // Блок: Решение проблем
            var groupFix = new GroupBox
            {
                Text = "🛠️ Решение проблем FACEIT AC",
                ForeColor = Color.LightGray,
                Location = new Point(20, 175),
                Size = new Size(620, 100),
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(groupFix);

            // Кнопка: Исправление Forbidden Driver
            var btnFixDriver = new Button
            {
                Text = "⚠️ Сбросить блокировки драйверов (HVCI)",
                Size = new Size(280, 35),
                Location = new Point(15, 25),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFixDriver.FlatAppearance.BorderSize = 0;
            btnFixDriver.Click += FixForbiddenDriver;
            groupFix.Controls.Add(btnFixDriver);

            // Кнопка: Сброс сети
            var btnResetNet = new Button
            {
                Text = "🌐 Решить ошибку подключения (Winsock)",
                Size = new Size(280, 35),
                Location = new Point(310, 25),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnResetNet.FlatAppearance.BorderSize = 0;
            btnResetNet.Click += (s, e) => RunCommand("netsh winsock reset && ipconfig /flushdns", "🌐 Сетевой стек сброшен и DNS кэш очищен.");
            groupFix.Controls.Add(btnResetNet);

            // Кнопка: Обновить статус
            var btnRefresh = new Button
            {
                Text = "🔄 Обновить статус",
                Size = new Size(180, 30),
                Location = new Point(20, 285),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => CheckServiceStatus();
            this.Controls.Add(btnRefresh);

            // Лог-консоль
            _logText = new RichTextBox
            {
                Location = new Point(20, 330),
                Size = new Size(620, 150),
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(_logText);

            Log("🧡 FACEIT Control готов к работе.");
        }

        private void CheckServiceStatus()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "query FACEIT",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (output.Contains("RUNNING"))
                        {
                            _statusLabel.Text = "🟢 Статус FACEIT AC: ✅ РАБОТАЕТ (Защита активна)";
                            _statusLabel.ForeColor = Color.LightGreen;
                        }
                        else if (output.Contains("STOPPED"))
                        {
                            _statusLabel.Text = "🔴 Статус FACEIT AC: ⏹️ ОСТАНОВЛЕН (Защита выключена)";
                            _statusLabel.ForeColor = Color.Tomato;
                        }
                        else
                        {
                            _statusLabel.Text = "⚪ Статус FACEIT AC: ❌ НЕ УСТАНОВЛЕН";
                            _statusLabel.ForeColor = Color.Gray;
                        }
                    }
                }
            }
            catch
            {
                _statusLabel.Text = "⚪ Статус FACEIT AC: ⚠️ ОШИБКА ЧТЕНИЯ";
                _statusLabel.ForeColor = Color.Yellow;
            }
        }

        private void RunCommand(string command, string successMessage)
        {
            Log($"▶️ Выполнение: {command}");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    Verb = "runas",
                    UseShellExecute = true,  // <-- КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        Log($"✅ {successMessage}");
                        CheckServiceStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ ОШИБКА: {ex.Message}");
                MessageBox.Show(
                    $"Ошибка выполнения команды:\n{ex.Message}\n\n" +
                    "Убедитесь, что программа запущена от имени администратора.",
                    "Ошибка FACEIT AC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FixForbiddenDriver(object sender, EventArgs e)
        {
            Log("🔧 Запуск исправления блокировок драйверов...");
            Log("ℹ️ FACEIT часто блокирует драйвера из-за HVCI или устаревших антивирусов.");

            try
            {
                // Очищаем потенциально опасные хуки реестра
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager", true))
                {
                    if (key != null)
                    {
                        key.SetValue("ExcludeFromKnownDlls", new string[] { }, RegistryValueKind.MultiString);
                        Log("✅ Фильтры DLL очищены.");
                        Log("⚠️ Рекомендуется отключить сторонние антивирусы для 100% совместимости с FACEIT AC.");
                    }
                    else
                    {
                        Log("❌ Не удалось найти ключ реестра.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ ОШИБКА: {ex.Message}");
                Log("⚠️ Для выполнения операции требуется запуск от имени администратора.");
            }
        }

        private void Log(string message)
        {
            if (_logText.InvokeRequired)
            {
                _logText.Invoke(new Action(() => Log(message)));
                return;
            }

            _logText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            _logText.ScrollToCaret();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FaceitControl";
            this.Size = new Size(660, 540);
            this.ResumeLayout(false);
        }
    }
}