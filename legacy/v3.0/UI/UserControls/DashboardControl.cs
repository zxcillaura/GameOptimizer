using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using GameOptimizer.Core;

namespace GameOptimizer.UI.UserControls
{
    public partial class DashboardControl : UserControl
    {
        // Элементы управления
        private Label _lblHeader;
        private Panel _pnlSpecs;
        private Label _lblCpu;
        private Label _lblGpu;
        private Label _lblRam;
        private Label _lblOs;
        private Label _lblVanguard;
        private Label _lblFaceit;
        private RichTextBox _txtConsole;
        private TableLayoutPanel _pnlButtons;
        private Timer _statusTimer;

        public DashboardControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(18, 19, 22);
            BuildUI();
            LoadSystemSpecs();
            CheckAntiCheats();

            // Таймер для обновления статуса каждые 5 секунд
            _statusTimer = new Timer { Interval = 5000 };
            _statusTimer.Tick += (s, e) => CheckAntiCheats();
            _statusTimer.Start();

            Log("📊 Панель диагностики инициализирована. Выберите режим оптимизации...");
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.Dock = DockStyle.Fill;

            // ============================================================
            // 1. ЗАГОЛОВОК
            // ============================================================
            _lblHeader = new Label
            {
                Text = "📊 Панель состояния системы",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(_lblHeader);

            // ============================================================
            // 2. БЛОК ХАРАКТЕРИСТИК ПК (КАРТОЧКА)
            // ============================================================
            _pnlSpecs = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(620, 140),
                BackColor = Color.FromArgb(30, 30, 35),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(_pnlSpecs);

            // Заголовок карточки
            var lblSpecTitle = new Label
            {
                Text = "💻 ХАРАКТЕРИСТИКИ ПК",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 149, 237),
                Location = new Point(10, 5),
                AutoSize = true
            };
            _pnlSpecs.Controls.Add(lblSpecTitle);

            // Параметры
            _lblCpu = CreateSpecLabel("🖥️ Процессор: Загрузка...", 30);
            _lblGpu = CreateSpecLabel("🎮 Видеокарта: Загрузка...", 55);
            _lblRam = CreateSpecLabel("💾 Оперативная память: Загрузка...", 80);
            _lblOs = CreateSpecLabel("💿 Операционная система: Загрузка...", 105);

            _pnlSpecs.Controls.AddRange(new Control[] { _lblCpu, _lblGpu, _lblRam, _lblOs });

            // ============================================================
            // 3. СТАТУС АНТИЧИТОВ
            // ============================================================
            _lblVanguard = new Label
            {
                Text = "🔒 Vanguard: ПРОВЕРКА...",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(20, 215),
                AutoSize = true
            };
            this.Controls.Add(_lblVanguard);

            _lblFaceit = new Label
            {
                Text = "🔒 FaceIT AC: ПРОВЕРКА...",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(20, 240),
                AutoSize = true
            };
            this.Controls.Add(_lblFaceit);

            // ============================================================
            // 4. СЕТКА КНОПОК ОПТИМИЗАЦИИ
            // ============================================================
            _pnlButtons = new TableLayoutPanel
            {
                Location = new Point(20, 275),
                Size = new Size(620, 110),
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            _pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _pnlButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _pnlButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Создаём кнопки
            var btnHard = CreateOptButton("🔥 ЖЕСТКАЯ ОПТИМИЗАЦИЯ", Color.FromArgb(180, 40, 40), 
                "Макс. FPS, мин. задержка ввода (для киберспорта)", 
                () => RunOptimization(PresetType.Hard));

            var btnSoft = CreateOptButton("🛡️ МЯГКАЯ ОПТИМИЗАЦИЯ", Color.FromArgb(40, 120, 180), 
                "Безопасный буст без отключения важных служб", 
                () => RunOptimization(PresetType.Soft));

            var btnVisual = CreateOptButton("🎨 КРАСИВАЯ КАРТИНКА", Color.FromArgb(110, 40, 180), 
                "Максимальный сок и плавность (для сингл-игр)", 
                () => RunOptimization(PresetType.Visual));

            var btnRollback = CreateOptButton("↩️ ОТКАТИТЬ ИЗМЕНЕНИЯ", Color.FromArgb(70, 70, 70), 
                "Вернуть систему к исходному состоянию", 
                RunRollback);

            _pnlButtons.Controls.Add(btnHard, 0, 0);
            _pnlButtons.Controls.Add(btnSoft, 1, 0);
            _pnlButtons.Controls.Add(btnVisual, 0, 1);
            _pnlButtons.Controls.Add(btnRollback, 1, 1);

            this.Controls.Add(_pnlButtons);

            // ============================================================
            // 5. ЛОГ-КОНСОЛЬ
            // ============================================================
            _txtConsole = new RichTextBox
            {
                Location = new Point(20, 400),
                Size = new Size(620, 130),
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 9.5F),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(_txtConsole);
        }

        // ============================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ UI
        // ============================================================

        private Label CreateSpecLabel(string text, int yPos)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(15, yPos),
                AutoSize = true
            };
        }

        private Button CreateOptButton(string text, Color baseColor, string tooltipText, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = baseColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(baseColor.R + 30, 255),
                Math.Min(baseColor.G + 30, 255),
                Math.Min(baseColor.B + 30, 255)
            );

            var toolTip = new ToolTip();
            toolTip.SetToolTip(btn, tooltipText);

            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void Log(string message)
        {
            if (_txtConsole.InvokeRequired)
            {
                _txtConsole.Invoke(new Action(() => Log(message)));
                return;
            }
            _txtConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            _txtConsole.SelectionStart = _txtConsole.Text.Length;
            _txtConsole.ScrollToCaret();
        }

        // ============================================================
        // ЛОГИКА ЗАГРУЗКИ ХАРАКТЕРИСТИК
        // ============================================================

        private void LoadSystemSpecs()
        {
            try
            {
                _lblCpu.Text = $"🖥️ Процессор: {GetRegistryValue(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString")}";
                _lblGpu.Text = $"🎮 Видеокарта: {GetGpuName()}";
                _lblRam.Text = $"💾 Оперативная память: {GetRamSizeGB()} GB";
                _lblOs.Text = $"💿 ОС: {Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";
            }
            catch (Exception ex)
            {
                Log($"⚠️ Ошибка загрузки характеристик: {ex.Message}");
                _lblCpu.Text = "🖥️ Процессор: Не удалось определить";
                _lblGpu.Text = "🎮 Видеокарта: Не удалось определить";
            }
        }

        private string GetRegistryValue(string path, string key)
        {
            try
            {
                using (var regKey = Registry.LocalMachine.OpenSubKey(path))
                {
                    return regKey?.GetValue(key)?.ToString().Trim() ?? "Неизвестно";
                }
            }
            catch { return "Неизвестно"; }
        }

        private string GetGpuName()
        {
            try
            {
                string[] gpuPaths = {
                    @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
                    @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0001"
                };

                foreach (var path in gpuPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            string desc = key.GetValue("DriverDesc")?.ToString();
                            if (!string.IsNullOrEmpty(desc))
                                return desc;
                        }
                    }
                }
            }
            catch { }
            return "NVIDIA / AMD Graphic Card";
        }

        private string GetRamSizeGB()
        {
            try
            {
                // Пытаемся получить объём памяти через WMI
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        double memory = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                        return Math.Round(memory / 1048576, 1).ToString("0.0");
                    }
                }
            }
            catch { }
            return "~16.0";
        }

        // ============================================================
        // ПРОВЕРКА АНТИЧИТОВ
        // ============================================================

        private void CheckAntiCheats()
        {
            try
            {
                // Проверка Vanguard (процесс vgc)
                bool isVanguardRunning = false;
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName("vgc");
                    if (processes.Length > 0) isVanguardRunning = true;
                    else
                    {
                        var vanguardProcesses = System.Diagnostics.Process.GetProcessesByName("vanguard");
                        if (vanguardProcesses.Length > 0) isVanguardRunning = true;
                    }
                }
                catch { }

                // Проверка FaceIT (процесс FACEITService)
                bool isFaceitRunning = false;
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName("FACEITService");
                    if (processes.Length > 0) isFaceitRunning = true;
                    else
                    {
                        var faceitProcesses = System.Diagnostics.Process.GetProcessesByName("FaceitClient");
                        if (faceitProcesses.Length > 0) isFaceitRunning = true;
                    }
                }
                catch { }

                // Обновляем UI
                if (isVanguardRunning)
                {
                    _lblVanguard.Text = "🟢 Vanguard: ✅ ЗАПУЩЕН";
                    _lblVanguard.ForeColor = Color.Green;
                }
                else
                {
                    _lblVanguard.Text = "🔴 Vanguard: ❌ ВЫКЛЮЧЕН";
                    _lblVanguard.ForeColor = Color.Red;
                }

                if (isFaceitRunning)
                {
                    _lblFaceit.Text = "🟢 FaceIT AC: ✅ ЗАПУЩЕН";
                    _lblFaceit.ForeColor = Color.Green;
                }
                else
                {
                    _lblFaceit.Text = "🔴 FaceIT AC: ❌ ВЫКЛЮЧЕН";
                    _lblFaceit.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                _lblVanguard.Text = "⚠️ Vanguard: ОШИБКА ПРОВЕРКИ";
                _lblFaceit.Text = "⚠️ FaceIT AC: ОШИБКА ПРОВЕРКИ";
                Log($"⚠️ Ошибка проверки античитов: {ex.Message}");
            }
        }

        // ============================================================
        // ЗАПУСК ОПТИМИЗАЦИИ И ОТКАТА
        // ============================================================

        private void RunOptimization(PresetType preset)
        {
            ToggleButtons(false);
            Log($"🚀 Запуск пресета: {preset.ToString().ToUpper()}...");

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    OptimizerEngine.ApplyPreset(preset, Log);
                }
                catch (Exception ex)
                {
                    Log($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                }
                finally
                {
                    this.Invoke(new Action(() => ToggleButtons(true)));
                }
            });
        }

        private void RunRollback()
        {
            ToggleButtons(false);
            Log("↩️ Запуск отката изменений...");

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    OptimizerEngine.Rollback(Log);
                }
                catch (Exception ex)
                {
                    Log($"❌ ОШИБКА ОТКАТА: {ex.Message}");
                }
                finally
                {
                    this.Invoke(new Action(() => ToggleButtons(true)));
                }
            });
        }

        private void ToggleButtons(bool enabled)
        {
            if (_pnlButtons.InvokeRequired)
            {
                _pnlButtons.Invoke(new Action(() => ToggleButtons(enabled)));
                return;
            }

            foreach (Control control in _pnlButtons.Controls)
            {
                if (control is Button btn)
                    btn.Enabled = enabled;
            }
        }

        // ============================================================
        // ИНИЦИАЛИЗАЦИЯ КОМПОНЕНТОВ (для WinForms Designer)
        // ============================================================

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "DashboardControl";
            this.Size = new Size(660, 540);
            this.ResumeLayout(false);
        }
    }
}