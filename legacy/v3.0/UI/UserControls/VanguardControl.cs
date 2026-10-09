using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.UserControls
{
    public partial class VanguardControl : UserControl
    {
        private Label _statusSecureBoot;
        private Label _statusTPM;
        private Label _statusService;
        private RichTextBox _logText;

        public VanguardControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(18, 19, 22);
            BuildUI();
            RefreshDiagnostics();
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.BackColor = Color.FromArgb(18, 19, 22);

            // Заголовок
            var lblTitle = new Label
            {
                Text = "❤️ УПРАВЛЕНИЕ ДЛЯ RIOT VANGUARD (VALORANT)",
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            // Статус Secure Boot
            _statusSecureBoot = new Label
            {
                Text = "🔒 Secure Boot в BIOS: ПРОВЕРКА...",
                ForeColor = Color.Yellow,
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(_statusSecureBoot);

            // Статус TPM
            _statusTPM = new Label
            {
                Text = "🔑 TPM 2.0 модуль чипа: ПРОВЕРКА...",
                ForeColor = Color.Yellow,
                Location = new Point(20, 90),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(_statusTPM);

            // Статус службы Vanguard
            _statusService = new Label
            {
                Text = "🛡️ Служба Vanguard (vgc): ПРОВЕРКА...",
                ForeColor = Color.Yellow,
                Location = new Point(20, 120),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(_statusService);

            // Блок предупреждения (как на скриншоте)
            var pnlWarning = new Panel
            {
                Location = new Point(20, 155),
                Size = new Size(620, 75),
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblWarning = new Label
            {
                Text = "⚠️ ВНИМАНИЕ: Secure Boot и TPM 2.0 невозможно принудительно включить из Windows!\n" +
                       "Это аппаратные узлы вашей материнской платы. Используйте кнопку перезапуска в BIOS ниже.",
                ForeColor = Color.FromArgb(255, 200, 80),
                Location = new Point(10, 8),
                Size = new Size(595, 55),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlWarning.Controls.Add(lblWarning);
            this.Controls.Add(pnlWarning);

            // Кнопка перезагрузки в BIOS
            var btnBios = new Button
            {
                Text = "⚡ ПЕРЕЗАГРУЗИТЬ ПК СРАЗУ В BIOS (UEFI)",
                Location = new Point(20, 245),
                Size = new Size(300, 45),
                BackColor = Color.FromArgb(200, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBios.FlatAppearance.BorderSize = 0;
            btnBios.Click += BtnBios_Click;
            this.Controls.Add(btnBios);

            // Кнопка инструкции
            var btnInstruction = new Button
            {
                Text = "📄 Показать инструкцию для BIOS",
                Location = new Point(335, 245),
                Size = new Size(305, 45),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstruction.FlatAppearance.BorderSize = 0;
            btnInstruction.Click += BtnInstruction_Click;
            this.Controls.Add(btnInstruction);

            // Кнопка управления службой Vanguard
            var btnService = new Button
            {
                Text = "🔄 Включить и запустить службу Vanguard (vgc)",
                Location = new Point(20, 305),
                Size = new Size(300, 45),
                BackColor = Color.FromArgb(40, 150, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnService.FlatAppearance.BorderSize = 0;
            btnService.Click += BtnService_Click;
            this.Controls.Add(btnService);

            // Кнопка остановки Vanguard
            var btnStop = new Button
            {
                Text = "⏹️ Остановить службу Vanguard",
                Location = new Point(335, 305),
                Size = new Size(305, 45),
                BackColor = Color.FromArgb(150, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            // Кнопка обновления статуса
            var btnRefresh = new Button
            {
                Text = "🔄 Обновить статус",
                Location = new Point(20, 365),
                Size = new Size(180, 30),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshDiagnostics();
            this.Controls.Add(btnRefresh);

            // Лог-консоль
            _logText = new RichTextBox
            {
                Location = new Point(20, 410),
                Size = new Size(620, 110),
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(_logText);

            Log("🛡️ Vanguard Control готов к работе.");
        }

        private void BtnBios_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Вы действительно хотите перезагрузить ПК для автоматического входа в BIOS (UEFI)?\n\nОбязательно сохраните все открытые файлы!",
                "Перезагрузка в BIOS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/r /fw /t 0",
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    Log("🔄 Перезагрузка в BIOS инициирована...");
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка: {ex.Message}");
                }
            }
        }

        private void BtnInstruction_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "🔧 КАК ВКЛЮЧИТЬ TPM 2.0 И SECURE BOOT:\n\n" +
                "1. Перезагрузите ПК в BIOS (кнопка выше поможет).\n" +
                "2. Перейдите в Advanced Mode (обычно F7).\n\n" +
                "🔐 TPM 2.0:\n" +
                "• Для Intel: найдите 'Intel PTT' и включите [Enabled]\n" +
                "• Для AMD: найдите 'AMD fTPM' и включите [Enabled]\n\n" +
                "🛡️ Secure Boot:\n" +
                "• Зайдите в Boot -> Secure Boot\n" +
                "• Установите Key Management в Default\n" +
                "• Переведите статус в [Standard] или [Enabled]\n\n" +
                "3. Сохраните настройки (F10) и перезагрузитесь.",
                "Инструкция по фиксу ошибок VAN 9001/9003",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void BtnService_Click(object sender, EventArgs e)
        {
            Log("🔄 Запуск службы Vanguard (vgc)...");
            await RunCommandAsync("sc config vgc start= system && sc start vgc", "✅ Служба Vanguard включена и запущена.");
            RefreshDiagnostics();
        }

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            Log("⏹️ Остановка службы Vanguard (vgc)...");
            await RunCommandAsync("sc stop vgc && sc config vgc start= disabled", "⏹️ Служба Vanguard остановлена и отключена.");
            RefreshDiagnostics();
        }

        private async System.Threading.Tasks.Task RunCommandAsync(string command, string successMessage)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        Log($"[{DateTime.Now:HH:mm:ss}] {successMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка выполнения команды:\n{ex.Message}\n\nУбедитесь, что программа запущена от имени администратора.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void RefreshDiagnostics()
        {
            // Проверка Secure Boot
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("UEFISecureBootEnabled");
                        if (val != null && Convert.ToInt32(val) == 1)
                        {
                            _statusSecureBoot.Text = "🔒 Secure Boot в BIOS: ✅ ВКЛЮЧЕН";
                            _statusSecureBoot.ForeColor = Color.Green;
                        }
                        else
                        {
                            _statusSecureBoot.Text = "🔒 Secure Boot в BIOS: ❌ ВЫКЛЮЧЕН (Критично!)";
                            _statusSecureBoot.ForeColor = Color.Red;
                        }
                    }
                    else
                    {
                        _statusSecureBoot.Text = "🔒 Secure Boot в BIOS: ❌ НЕ ОПРЕДЕЛЁН";
                        _statusSecureBoot.ForeColor = Color.Red;
                    }
                }
            }
            catch
            {
                _statusSecureBoot.Text = "🔒 Secure Boot в BIOS: ⚠️ ОШИБКА ЧТЕНИЯ";
                _statusSecureBoot.ForeColor = Color.Yellow;
            }

            // Проверка TPM
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TPM"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("Enabled");
                        if (val != null && Convert.ToInt32(val) == 1)
                        {
                            _statusTPM.Text = "🔑 TPM 2.0 модуль чипа: ✅ ВКЛЮЧЕН";
                            _statusTPM.ForeColor = Color.Green;
                        }
                        else
                        {
                            _statusTPM.Text = "🔑 TPM 2.0 модуль чипа: ❌ ВЫКЛЮЧЕН (Критично!)";
                            _statusTPM.ForeColor = Color.Red;
                        }
                    }
                    else
                    {
                        _statusTPM.Text = "🔑 TPM 2.0 модуль чипа: ❌ НЕ ОПРЕДЕЛЁН";
                        _statusTPM.ForeColor = Color.Red;
                    }
                }
            }
            catch
            {
                _statusTPM.Text = "🔑 TPM 2.0 модуль чипа: ⚠️ ОШИБКА ЧТЕНИЯ";
                _statusTPM.ForeColor = Color.Yellow;
            }

            // Проверка службы Vanguard
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "query vgc",
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
                            _statusService.Text = "🛡️ Служба Vanguard (vgc): ✅ РАБОТАЕТ";
                            _statusService.ForeColor = Color.Green;
                        }
                        else if (output.Contains("STOPPED"))
                        {
                            _statusService.Text = "🛡️ Служба Vanguard (vgc): ⏹️ ОСТАНОВЛЕНА";
                            _statusService.ForeColor = Color.Red;
                        }
                        else
                        {
                            _statusService.Text = "🛡️ Служба Vanguard (vgc): ❌ НЕ УСТАНОВЛЕНА";
                            _statusService.ForeColor = Color.Red;
                        }
                    }
                }
            }
            catch
            {
                _statusService.Text = "🛡️ Служба Vanguard (vgc): ⚠️ ОШИБКА ЧТЕНИЯ";
                _statusService.ForeColor = Color.Yellow;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "VanguardControl";
            this.Size = new Size(660, 540);
            this.ResumeLayout(false);
        }
    }
}