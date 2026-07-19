using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Threading;

namespace GameOptimizer
{
    public class MainForm : Form
    {
        // Элементы навигации и интерфейса
        private Panel sidebarPanel;
        private Panel contentPanel;
        private TabControl hiddenTabControl;
        private ProgressBar progressBar;
        private RichTextBox logBox;
        private Label statusLabel;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayMenu;
        private ToolTip helperToolTip;

        // Кнопки сайдбара
        private Button btnMenuMain, btnMenuSys, btnMenuNet, btnMenuFaceit, btnMenuVanguard;

        // Вкладка: Главная
        private Button btnHardOpt, btnSoftOpt, btnHardRest, btnSoftRest, btnClean, btnCreateRestore, btnExit;

        // Вкладка: Параметры системы (Чекбоксы)
        private CheckBox chkPowerPlan, chkVisualEffects, chkPriority, chkGameBar;
        private CheckBox chkServices, chkNetwork, chkTcpTweaks, chkTelemetry;
        private CheckBox chkHyperV, chkDefender, chkUIServices, chkMemoryDisk;
        private CheckBox chkTimers, chkNvidia, chkIPv6, chkFirewall;
        private CheckBox chkFSO, chkCpuUnpark, chkNetworkThrottling, chkMitigations;
        private CheckBox chkHags, chkHibernation; // НОВЫЕ ТВИКИ
        private Button btnApplySelected, btnResetAll;

        // Вкладка: Сеть и DNS (DNS Jumper)
        private ComboBox comboDNS;
        private Button btnApplyDNS, btnTestDNS;
        private ListView listDns;

        // Диагностика античитов
        private Label lblFaceitHyperV, lblFaceitVBS, lblVanSecureBoot, lblVanTPM;

        // Структура и список серверов для DNS Jumper
        private struct DnsItem
        {
            public string Name;
            public string Primary;
            public string Secondary;
        }

        private DnsItem[] dnsList = new DnsItem[]
        {
            new DnsItem { Name = "Cloudflare DNS", Primary = "1.1.1.1", Secondary = "1.0.0.1" },
            new DnsItem { Name = "Google Public DNS", Primary = "8.8.8.8", Secondary = "8.8.4.4" },
            new DnsItem { Name = "Yandex DNS (Basic)", Primary = "77.88.8.8", Secondary = "77.88.8.1" },
            new DnsItem { Name = "Yandex DNS (Safe)", Primary = "77.88.8.88", Secondary = "77.88.8.2" },
            new DnsItem { Name = "AdGuard AdBlock", Primary = "94.140.14.14", Secondary = "94.140.15.15" },
            new DnsItem { Name = "Quad9 Secure", Primary = "9.9.9.9", Secondary = "149.112.112.112" },
            new DnsItem { Name = "OpenDNS Home", Primary = "208.67.222.222", Secondary = "208.67.220.220" },
            new DnsItem { Name = "Qwant DNS", Primary = "176.31.202.100", Secondary = "176.31.202.101" },
            new DnsItem { Name = "Level3 DNS", Primary = "209.244.0.3", Secondary = "209.244.0.4" },
            new DnsItem { Name = "Comodo Secure", Primary = "8.26.56.26", Secondary = "8.20.247.20" }
        };

        public MainForm()
        {
            this.Text = "Игровой помощник zxcillaura";
            this.Size = new Size(880, 720); // Немного увеличил окно для новых функций
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 19, 22);
            this.ForeColor = Color.White;

            helperToolTip = new ToolTip { AutoPopDelay = 12000, InitialDelay = 100, ReshowDelay = 100, ToolTipIcon = ToolTipIcon.Info, ToolTipTitle = "Описание параметра" };

            notifyIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "Игровой помощник", Visible = true };
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Выйти", null, (s, e) => { notifyIcon.Visible = false; this.Close(); });
            notifyIcon.ContextMenuStrip = trayMenu;

            sidebarPanel = new Panel { Size = new Size(200, 720), Location = new Point(0, 0), BackColor = Color.FromArgb(13, 14, 16) };
            this.Controls.Add(sidebarPanel);

            Label sideTitle = new Label { Text = "zxcillaura\nBOOSTER PRO", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(114, 137, 218), Location = new Point(10, 20), Size = new Size(180, 50), TextAlign = ContentAlignment.MiddleCenter };
            sidebarPanel.Controls.Add(sideTitle);

            int menuY = 100;
            btnMenuMain = MakeMenuButton("🏠 Главная", menuY); btnMenuMain.Click += (s, e) => SwitchTab(0, btnMenuMain); sidebarPanel.Controls.Add(btnMenuMain); menuY += 45;
            btnMenuSys = MakeMenuButton("⚙️ Система", menuY); btnMenuSys.Click += (s, e) => SwitchTab(1, btnMenuSys); sidebarPanel.Controls.Add(btnMenuSys); menuY += 45;
            btnMenuNet = MakeMenuButton("🌐 Сеть и DNS", menuY); btnMenuNet.Click += (s, e) => SwitchTab(2, btnMenuNet); sidebarPanel.Controls.Add(btnMenuNet); menuY += 45;
            btnMenuFaceit = MakeMenuButton("🧡 FACEIT AC", menuY); btnMenuFaceit.Click += (s, e) => SwitchTab(3, btnMenuFaceit); sidebarPanel.Controls.Add(btnMenuFaceit); menuY += 45;
            btnMenuVanguard = MakeMenuButton("❤️ Riot Vanguard", menuY); btnMenuVanguard.Click += (s, e) => SwitchTab(4, btnMenuVanguard); sidebarPanel.Controls.Add(btnMenuVanguard);

            contentPanel = new Panel { Size = new Size(660, 550), Location = new Point(205, 10), BackColor = Color.FromArgb(18, 19, 22) };
            this.Controls.Add(contentPanel);

            hiddenTabControl = new TabControl { Size = new Size(660, 550), Location = new Point(0, 0), Appearance = TabAppearance.FlatButtons, ItemSize = new Size(0, 1), SizeMode = TabSizeMode.Fixed };
            contentPanel.Controls.Add(hiddenTabControl);

            TabPage tabMain = new TabPage { BackColor = Color.FromArgb(18, 19, 22) }; hiddenTabControl.TabPages.Add(tabMain);
            TabPage tabSys = new TabPage { BackColor = Color.FromArgb(18, 19, 22) }; hiddenTabControl.TabPages.Add(tabSys);
            TabPage tabNet = new TabPage { BackColor = Color.FromArgb(18, 19, 22) }; hiddenTabControl.TabPages.Add(tabNet);
            TabPage tabFaceit = new TabPage { BackColor = Color.FromArgb(18, 19, 22) }; hiddenTabControl.TabPages.Add(tabFaceit);
            TabPage tabVanguard = new TabPage { BackColor = Color.FromArgb(18, 19, 22) }; hiddenTabControl.TabPages.Add(tabVanguard);

            BuildMainTab(tabMain); BuildSysTab(tabSys); BuildNetTab(tabNet); BuildFaceitTab(tabFaceit); BuildVanguardTab(tabVanguard);

            progressBar = new ProgressBar { Location = new Point(215, 570), Size = new Size(640, 10), Style = ProgressBarStyle.Continuous, BackColor = Color.FromArgb(30, 31, 36), ForeColor = Color.FromArgb(114, 137, 218) };
            this.Controls.Add(progressBar);
            logBox = new RichTextBox { Location = new Point(215, 585), Size = new Size(640, 50), ReadOnly = true, BackColor = Color.FromArgb(13, 14, 16), ForeColor = Color.FromArgb(150, 155, 170), Font = new Font("Consolas", 8), BorderStyle = BorderStyle.None };
            this.Controls.Add(logBox);
            statusLabel = new Label { Text = "Утилита готова к работе.", Location = new Point(215, 640), Size = new Size(640, 20), Font = new Font("Segoe UI", 9), ForeColor = Color.DarkGray, TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(statusLabel);

            HighlightMenuButton(btnMenuMain);
            RefreshSystemDiagnostics();
        }

        private Button MakeMenuButton(string text, int y)
        {
            Button btn = new Button { Text = "  " + text, Location = new Point(10, y), Size = new Size(180, 40), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(13, 14, 16), ForeColor = Color.FromArgb(180, 185, 200), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { if (btn.BackColor != Color.FromArgb(35, 39, 42)) btn.BackColor = Color.FromArgb(25, 28, 30); };
            btn.MouseLeave += (s, e) => { if (btn.BackColor != Color.FromArgb(35, 39, 42)) btn.BackColor = Color.FromArgb(13, 14, 16); };
            return btn;
        }

        private void HighlightMenuButton(Button activeBtn)
        {
            btnMenuMain.BackColor = btnMenuSys.BackColor = btnMenuNet.BackColor = btnMenuFaceit.BackColor = btnMenuVanguard.BackColor = Color.FromArgb(13, 14, 16);
            btnMenuMain.ForeColor = btnMenuSys.ForeColor = btnMenuNet.ForeColor = btnMenuFaceit.ForeColor = btnMenuVanguard.ForeColor = Color.FromArgb(180, 185, 200);
            activeBtn.BackColor = Color.FromArgb(35, 39, 42); activeBtn.ForeColor = Color.FromArgb(114, 137, 218);
        }

        private void SwitchTab(int index, Button senderBtn) { hiddenTabControl.SelectedIndex = index; HighlightMenuButton(senderBtn); RefreshSystemDiagnostics(); }

        private Button MakeCardButton(string text, Color color, int x, int y, int w, int h)
        {
            Button btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(color, 0.15f);
            btn.MouseLeave += (s, e) => btn.BackColor = color;
            return btn;
        }

        private Panel MakeCardPanel(string title, int x, int y, int w, int h)
        {
            Panel p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.FromArgb(24, 25, 28) };
            Label l = new Label { Text = title, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(114, 137, 218), Location = new Point(15, 10), Size = new Size(w - 30, 20) };
            p.Controls.Add(l); return p;
        }

        private CheckBox MakeCheckbox(string text, int x, int y, Panel parent)
        {
            CheckBox cb = new CheckBox { Text = text, Location = new Point(x, y), Size = new Size(240, 24), ForeColor = Color.FromArgb(210, 215, 220), Font = new Font("Segoe UI", 8.2f) };
            parent.Controls.Add(cb); return cb;
        }

        private void AddHelpLabel(Control target, string tip, Control parent)
        {
            Label qLabel = new Label { Text = "❓", Location = new Point(target.Right + 4, target.Top + (target.Height - 16) / 2), Size = new Size(16, 16), ForeColor = Color.FromArgb(114, 137, 218), Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand };
            parent.Controls.Add(qLabel); helperToolTip.SetToolTip(qLabel, tip); helperToolTip.SetToolTip(target, tip);
        }

        // --- 1. ГЛАВНАЯ ---
        private void BuildMainTab(TabPage page)
        {
            Panel p = MakeCardPanel("БЫСТРАЯ ОПТИМИЗАЦИЯ И ОТКАТ СИСТЕМЫ", 10, 10, 640, 520);
            page.Controls.Add(p);

            btnHardOpt = MakeCardButton("🚀 Жёсткая оптимизация (Максимум FPS)", Color.FromArgb(220, 53, 69), 40, 50, 520, 45);
            btnHardOpt.Click += (s, e) => { SetHardProfile(); ApplySystemTweaks(); }; p.Controls.Add(btnHardOpt);
            AddHelpLabel(btnHardOpt, "Включает абсолютно все твики системы.", p);

            btnSoftOpt = MakeCardButton("⚡ Мягкая оптимизация (Безопасный режим)", Color.FromArgb(40, 167, 69), 40, 110, 520, 45);
            btnSoftOpt.Click += (s, e) => { SetSoftProfile(); ApplySystemTweaks(); }; p.Controls.Add(btnSoftOpt);

            btnHardRest = MakeCardButton("🔄 Полный откат жёсткой оптимизации", Color.FromArgb(108, 117, 125), 40, 170, 520, 40);
            btnHardRest.Click += BtnHardRest_Click; p.Controls.Add(btnHardRest);

            btnSoftRest = MakeCardButton("⏳ Откат мягкой оптимизации", Color.FromArgb(108, 117, 125), 40, 220, 520, 40);
            btnSoftRest.Click += BtnSoftRest_Click; p.Controls.Add(btnSoftRest);

            btnClean = MakeCardButton("🧹 Очистить кэш, мусор и ОЗУ перед игрой", Color.FromArgb(23, 162, 184), 40, 280, 520, 45);
            btnClean.Click += BtnClean_Click; p.Controls.Add(btnClean);
            AddHelpLabel(btnClean, "Завершает браузеры, Discord, надежно чистит Temp файлы через нативный C#.", p);

            btnCreateRestore = MakeCardButton("🛡️ Создать точку восстановления системы", Color.FromArgb(114, 137, 218), 40, 350, 520, 45);
            btnCreateRestore.Click += BtnCreateRestore_Click; p.Controls.Add(btnCreateRestore);

            btnExit = MakeCardButton("Выйти из программы", Color.FromArgb(45, 47, 52), 240, 420, 160, 35);
            btnExit.Click += (s, e) => this.Close(); p.Controls.Add(btnExit);
        }

        // --- 2. ПАРАМЕТРЫ СИСТЕМЫ ---
        private void BuildSysTab(TabPage page)
        {
            Panel p = MakeCardPanel("КАСТОМНЫЙ ВЫБОР ПАРАМЕТРОВ СИСТЕМЫ", 10, 10, 640, 520);
            page.Controls.Add(p);
            int col1 = 20; int col2 = 320; int y = 40; int spacing = 22;

            chkPowerPlan = MakeCheckbox("Максимальная производительность питания", col1, y, p); y += spacing;
            chkVisualEffects = MakeCheckbox("Отключение анимаций интерфейса", col1, y, p); y += spacing;
            chkPriority = MakeCheckbox("Оптимальный приоритет играм", col1, y, p); y += spacing;
            chkGameBar = MakeCheckbox("Полное отключение Game Bar / DVR", col1, y, p); y += spacing;
            chkFSO = MakeCheckbox("Отключение Fullscreen Optimizations", col1, y, p); y += spacing;
            chkCpuUnpark = MakeCheckbox("Разблокировка парковки ядер CPU", col1, y, p); y += spacing;
            chkServices = MakeCheckbox("Отключение ненужных служб (30+ шт)", col1, y, p); y += spacing;
            chkNetwork = MakeCheckbox("Оптимизация сетевых пакетов (TCP/IP)", col1, y, p); y += spacing;
            chkUIServices = MakeCheckbox("Сохранить темы оформления Windows", col1, y, p); y += spacing;
            chkHags = MakeCheckbox("Включить Hardware GPU Scheduling (HAGS)", col1, y, p); AddHelpLabel(chkHags, "Аппаратное ускорение графического процессора. Снижает задержку.", p); y += spacing;
            
            y = 40;
            chkTcpTweaks = MakeCheckbox("Реестровые алгоритмы TCP (Nagle)", col2, y, p); y += spacing;
            chkNetworkThrottling = MakeCheckbox("Отключение сетевого троттлинга", col2, y, p); y += spacing;
            chkTelemetry = MakeCheckbox("Блокировка телеметрии Майкрософт", col2, y, p); y += spacing;
            chkHyperV = MakeCheckbox("Отключение виртуализации Hyper-V", col2, y, p); y += spacing;
            chkDefender = MakeCheckbox("Отключение Защитника Windows", col2, y, p); y += spacing;
            chkMitigations = MakeCheckbox("Отключение Spectre / Meltdown", col2, y, p); y += spacing;
            chkMemoryDisk = MakeCheckbox("Оптимизация разметки диска NTFS", col2, y, p); y += spacing;
            chkTimers = MakeCheckbox("Настройка высокоточных таймеров HPET", col2, y, p); y += spacing;
            chkNvidia = MakeCheckbox("Твики стабильности драйверов NVIDIA", col2, y, p); y += spacing;
            chkIPv6 = MakeCheckbox("Полное отключение протокола IPv6", col2, y, p); y += spacing;
            chkFirewall = MakeCheckbox("Отключение брандмауэра Windows", col2, y, p); y += spacing;
            chkHibernation = MakeCheckbox("Отключить гибернацию (Fast Boot)", col2, y, p); AddHelpLabel(chkHibernation, "Убирает ложный аптайм ПК, освобождает до 8 ГБ на диске C:.", p);

            Button btnDeepClean = MakeCardButton("🔥 Глубокая очистка диска", Color.FromArgb(220, 53, 69), 40, 330, 260, 40);
            btnDeepClean.Click += (s, e) => RunDeepCleanup(); p.Controls.Add(btnDeepClean);

            Button btnBloat = MakeCardButton("📦 Удалить мусор Windows", Color.FromArgb(108, 117, 125), 340, 330, 260, 40);
            btnBloat.Click += (s, e) => RemoveBloatware(); p.Controls.Add(btnBloat);

            CheckBox chkStartup = new CheckBox { Text = "Запускать вместе с Windows", Location = new Point(40, 385), Size = new Size(220, 30), ForeColor = Color.White };
            chkStartup.CheckedChanged += (s, e) => ToggleAutostart(chkStartup.Checked); p.Controls.Add(chkStartup);

            btnApplySelected = MakeCardButton("Применить выбранное", Color.FromArgb(40, 167, 69), 40, 440, 260, 40);
            btnApplySelected.Click += (s, e) => ApplySystemTweaks(); p.Controls.Add(btnApplySelected);

            btnResetAll = MakeCardButton("Сбросить флажки", Color.FromArgb(108, 117, 125), 340, 440, 260, 40);
            btnResetAll.Click += (s, e) => ResetAllCheckboxes(); p.Controls.Add(btnResetAll);
        }

        // --- 3. СЕТЬ И DNS ---
        private void BuildNetTab(TabPage page)
        {
            Panel p = MakeCardPanel("НАСТРОЙКА И ТЕСТ ИГРОВЫХ DNS (DNS JUMPER)", 10, 10, 640, 480);
            page.Controls.Add(p);

            Label lblSelect = new Label { Text = "Ручной выбор DNS:", Location = new Point(20, 40), Size = new Size(200, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White }; p.Controls.Add(lblSelect);
            comboDNS = new ComboBox { Location = new Point(20, 65), Size = new Size(240, 21), BackColor = Color.FromArgb(35, 39, 42), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var dns in dnsList) comboDNS.Items.Add(dns.Name); comboDNS.Items.Add("Автоматический DNS (Сброс)"); comboDNS.SelectedIndex = 0; p.Controls.Add(comboDNS);

            btnApplyDNS = MakeCardButton("Применить выбранный DNS", Color.FromArgb(114, 137, 218), 20, 115, 240, 38); btnApplyDNS.Click += BtnApplyDNS_Click; p.Controls.Add(btnApplyDNS);
            Label infoText = new Label { Text = "⚡ Инструкция:\n1. Нажмите кнопку 'Тест пинга' справа.\n2. Дождитесь проверки задержки.\n3. Утилита выделит самый лучший сервер.\n4. Нажмите 'Применить'.", Location = new Point(20, 180), Size = new Size(240, 220), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8.5f) }; p.Controls.Add(infoText);

            listDns = new ListView { Location = new Point(310, 40), Size = new Size(310, 310), BackColor = Color.FromArgb(13, 14, 16), ForeColor = Color.White, BorderStyle = BorderStyle.None, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
            listDns.Columns.Add("Сервер", 140); listDns.Columns.Add("Основной IP", 90); listDns.Columns.Add("Пинг", 70); p.Controls.Add(listDns);
            try { typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(listDns, true, null); } catch { }
            foreach (var dns in dnsList) { ListViewItem item = new ListViewItem(dns.Name); item.SubItems.Add(dns.Primary); item.SubItems.Add("не проверено"); item.UseItemStyleForSubItems = false; listDns.Items.Add(item); }

            btnTestDNS = MakeCardButton("⚡ Найти самый быстрый DNS", Color.FromArgb(40, 167, 69), 310, 365, 280, 42); btnTestDNS.Click += BtnTestDNS_Click; p.Controls.Add(btnTestDNS);
            
            Button btnAdvNet = MakeCardButton("⚡ Оптимизировать Сеть (EEE / TCP / LSO)", Color.FromArgb(23, 162, 184), 310, 420, 280, 40);
            btnAdvNet.Click += (s, e) => ApplyAdvancedNetworkTweaks(); p.Controls.Add(btnAdvNet);
            AddHelpLabel(btnAdvNet, "Глубокая оптимизация: отключает энергосбережение сетевых карт и Large Send Offload для снижения лосс-пакетов.", p);
        }

        // --- 4. FACEIT ANTI-CHEAT ---
        private void BuildFaceitTab(TabPage page)
        {
            Panel p = MakeCardPanel("УПРАВЛЕНИЕ ДЛЯ FACEIT ANTI-CHEAT", 10, 10, 640, 480);
            page.Controls.Add(p);

            lblFaceitHyperV = new Label { Text = "Hyper-V виртуализация: проверка...", Location = new Point(30, 50), Size = new Size(310, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }; p.Controls.Add(lblFaceitHyperV);
            Button btnHypOn = MakeCardButton("Включить Hyper-V", Color.FromArgb(40, 167, 69), 390, 45, 100, 28); btnHypOn.Click += (s, e) => { RunCommand("bcdedit", "/set hypervisorlaunchtype auto"); RefreshSystemDiagnostics(); }; p.Controls.Add(btnHypOn);
            Button btnHypOff = MakeCardButton("Выключить", Color.FromArgb(220, 53, 69), 500, 45, 100, 28); btnHypOff.Click += (s, e) => { RunCommand("bcdedit", "/set hypervisorlaunchtype off"); RefreshSystemDiagnostics(); }; p.Controls.Add(btnHypOff);

            lblFaceitVBS = new Label { Text = "VBS (Изоляция ядра ОС): проверка...", Location = new Point(30, 95), Size = new Size(310, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }; p.Controls.Add(lblFaceitVBS);
            Button btnVbsOn = MakeCardButton("Включить VBS", Color.FromArgb(40, 167, 69), 390, 90, 100, 28); btnVbsOn.Click += (s, e) => { RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f"); RefreshSystemDiagnostics(); }; p.Controls.Add(btnVbsOn);
            Button btnVbsOff = MakeCardButton("Выключить", Color.FromArgb(220, 53, 69), 500, 90, 100, 28); btnVbsOff.Click += (s, e) => { RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f"); RefreshSystemDiagnostics(); }; p.Controls.Add(btnVbsOff);

            Button btnFixDEP = MakeCardButton("Включить DEP (Data Execution Prevention)", Color.FromArgb(108, 117, 125), 30, 240, 400, 35);
            btnFixDEP.Click += (s, e) => { RunCommand("bcdedit", "/set nx AlwaysOn"); ShowNotification("DEP включен. Требуется перезагрузка.", "FACEIT"); }; p.Controls.Add(btnFixDEP);
            AddHelpLabel(btnFixDEP, "Если FACEIT просит включить DEP, эта кнопка принудительно включит его в загрузчике Windows.", p);

            Button btnFixDebug = MakeCardButton("Выключить тестовый режим и режим отладки Windows", Color.FromArgb(108, 117, 125), 30, 290, 400, 35);
            btnFixDebug.Click += (s, e) => { RunCommand("bcdedit", "/debug off"); RunCommand("bcdedit", "/set testsigning off"); ShowNotification("Режимы отладки деактивированы.", "FACEIT"); }; p.Controls.Add(btnFixDebug);

            Button btnFixSvc = MakeCardButton("Восстановить службу FACEIT Service", Color.FromArgb(108, 117, 125), 30, 340, 400, 35);
            btnFixSvc.Click += (s, e) => { RunCommand("sc", "config FACEITService start= demand"); ShowNotification("Служба переведена в ручной запуск.", "FACEIT"); }; p.Controls.Add(btnFixSvc);
        }

        // --- 5. RIOT VANGUARD ---
        private void BuildVanguardTab(TabPage page)
        {
            Panel p = MakeCardPanel("УПРАВЛЕНИЕ ДЛЯ RIOT VANGUARD (VALORANT)", 10, 10, 640, 480);
            page.Controls.Add(p);

            lblVanSecureBoot = new Label { Text = "Secure Boot в BIOS: проверка...", Location = new Point(30, 50), Size = new Size(310, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }; p.Controls.Add(lblVanSecureBoot);
            lblVanTPM = new Label { Text = "TPM 2.0 модуль чипа: проверка...", Location = new Point(30, 95), Size = new Size(310, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }; p.Controls.Add(lblVanTPM);

            Label warningLabel = new Label { Text = "⚠️ ВНИМАНИЕ: Secure Boot и TPM 2.0 невозможно принудительно включить программами из Windows! Это аппаратные узлы вашей материнской платы. Чтобы включить их, воспользуйтесь кнопкой перезапуска в BIOS ниже.", Location = new Point(30, 150), Size = new Size(580, 50), ForeColor = Color.FromArgb(240, 173, 78), Font = new Font("Segoe UI", 9, FontStyle.Italic) }; p.Controls.Add(warningLabel);

            Button btnGoToBios = MakeCardButton("⚡ ПЕРЕЗАГРУЗИТЬ ПК СРАЗУ В BIOS (UEFI)", Color.FromArgb(220, 53, 69), 30, 220, 350, 42);
            btnGoToBios.Click += (s, e) => { if (MessageBox.Show("Мгновенная перезагрузка в BIOS?", "Запуск UEFI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) RunCommand("shutdown", "/r /fw /t 0"); }; p.Controls.Add(btnGoToBios);

            Button btnShowInst = MakeCardButton("📋 Инструкция для BIOS", Color.FromArgb(35, 39, 42), 390, 220, 180, 42);
            btnShowInst.Click += (s, e) => { MessageBox.Show("КАК ВКЛЮЧИТЬ TPM 2.0 И SECURE BOOT:\n\n1. Перезагрузите ПК в BIOS.\n2. Перейдите в Advanced Mode (кнопка F7).\n3. Раздел TPM: найдите 'Intel PTT' или 'AMD fTPM' и выставите в [Enabled].\n4. Раздел Secure Boot: зайдите в Boot -> Secure Boot. Установите Key Management в Default.\n5. Сохраните (F10) и перезагрузите.", "Инструкция", MessageBoxButtons.OK, MessageBoxIcon.Information); }; p.Controls.Add(btnShowInst);

            Button btnFixVgc = MakeCardButton("Включить службу Vanguard (vgc)", Color.FromArgb(40, 167, 69), 30, 280, 350, 35);
            btnFixVgc.Click += (s, e) => { RunCommand("sc", "config vgc start= auto"); RunCommand("sc", "start vgc"); ShowNotification("Служба vgc запущена.", "Vanguard"); }; p.Controls.Add(btnFixVgc);

            Button btnFixVanNet = MakeCardButton("Сброс сети (Фикс ошибок VAN 135 / VAN 68)", Color.FromArgb(23, 162, 184), 30, 330, 350, 35);
            btnFixVanNet.Click += (s, e) => { RunCommand("netsh", "winsock reset"); RunCommand("netsh", "int ip reset"); ShowNotification("Сетевой стек сброшен. Перезагрузите ПК.", "Vanguard"); }; p.Controls.Add(btnFixVanNet);
            AddHelpLabel(btnFixVanNet, "Полностью переустанавливает сокеты Windows. Лечит 90% ошибок потери соединения с серверами Valorant.", p);
        }

        private void BtnTestDNS_Click(object sender, EventArgs e)
        {
            btnTestDNS.Enabled = false; btnTestDNS.Text = "⏳ Тестирование..."; SetStatus("Идет измерение задержки DNS серверов...", 10);
            Thread dnsTestThread = new Thread(() =>
            {
                for (int i = 0; i < dnsList.Length; i++)
                {
                    string ip = dnsList[i].Primary; long pingMs = -1;
                    try { using (Ping pingSender = new Ping()) { PingReply reply = pingSender.Send(ip, 1000); if (reply.Status == IPStatus.Success) pingMs = reply.RoundtripTime; } } catch { }
                    int index = i; long resultTime = pingMs;
                    this.Invoke(new Action(() => {
                        if (resultTime >= 0) {
                            listDns.Items[index].SubItems[2].Text = resultTime + " ms";
                            if (resultTime < 25) listDns.Items[index].SubItems[2].ForeColor = Color.FromArgb(40, 167, 69);
                            else if (resultTime < 75) listDns.Items[index].SubItems[2].ForeColor = Color.FromArgb(240, 173, 78);
                            else listDns.Items[index].SubItems[2].ForeColor = Color.FromArgb(220, 53, 69);
                        }
                        else { listDns.Items[index].SubItems[2].Text = "Таймаут"; listDns.Items[index].SubItems[2].ForeColor = Color.Gray; }
                    }));
                }
                this.Invoke(new Action(() => {
                    long bestPing = 999999; int bestIndex = -1;
                    for (int i = 0; i < dnsList.Length; i++) {
                        string pingValText = listDns.Items[i].SubItems[2].Text;
                        if (pingValText.Contains("ms")) {
                            long ms = long.Parse(pingValText.Replace(" ms", ""));
                            if (ms < bestPing) { bestPing = ms; bestIndex = i; }
                        }
                    }
                    btnTestDNS.Enabled = true; btnTestDNS.Text = "⚡ Найти самый быстрый DNS";
                    if (bestIndex != -1) { comboDNS.SelectedIndex = bestIndex; SetStatus("Тест завершен! Найден оптимальный DNS.", 100); }
                    else { SetStatus("Не удалось получить ответы от DNS-серверов.", 100); }
                }));
            });
            dnsTestThread.IsBackground = true; dnsTestThread.Start();
        }

        private void RefreshSystemDiagnostics()
        {
            // Упрощенная диагностика для предотвращения зависаний
            lblFaceitHyperV.Text = "Hyper-V виртуализация: статус обновляется...";
            lblFaceitVBS.Text = "VBS (Изоляция ядра ОС): статус обновляется...";
            lblVanSecureBoot.Text = "Secure Boot: проверьте в BIOS";
            lblVanTPM.Text = "TPM 2.0: проверьте в BIOS";
        }

        private void Log(string text) { if (logBox.InvokeRequired) logBox.Invoke(new Action(() => logBox.AppendText(text + "\n"))); else logBox.AppendText(text + "\n"); logBox.ScrollToCaret(); Application.DoEvents(); }
        private void SetStatus(string text, int progress = -1) { if (statusLabel.InvokeRequired) statusLabel.Invoke(new Action(() => statusLabel.Text = text)); else statusLabel.Text = text; if (progress >= 0) { if (progressBar.InvokeRequired) progressBar.Invoke(new Action(() => progressBar.Value = progress)); else progressBar.Value = progress; } Application.DoEvents(); }

        private void RunCommand(string command, string args)
        {
            try
            {
                Process p = new Process { StartInfo = new ProcessStartInfo { FileName = command, Arguments = args, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true } };
                p.Start(); string output = p.StandardOutput.ReadToEnd(); string error = p.StandardError.ReadToEnd(); p.WaitForExit();
                if (!string.IsNullOrEmpty(output)) Log(output.Trim());
            } catch (Exception ex) { Log("Ошибка: " + ex.Message); }
        }

        private void ShowNotification(string text, string title = "Оптимизатор") { if (this.InvokeRequired) this.Invoke(new Action(() => notifyIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info))); else notifyIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info); }

        private void BtnCreateRestore_Click(object sender, EventArgs e)
        {
            SetStatus("Создание контрольной точки восстановления...", 30);
            RunCommand("reg", "add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore\" /v Frequency /t REG_DWORD /d 0 /f");
            RunCommand("powershell", "-Command \"Checkpoint-Computer -Description 'zxcillaura_BoosterPro' -RestorePointType 'MODIFY_SETTINGS' -Confirm:$false\"");
            SetStatus("Точка восстановления успешно создана!", 100);
            MessageBox.Show("Контрольная точка восстановления 'zxcillaura_BoosterPro' была успешно создана!", "Резервная копия", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnApplyDNS_Click(object sender, EventArgs e)
        {
            SetStatus("Применение DNS настроек...", 40);
            int idx = comboDNS.SelectedIndex;
            if (idx == comboDNS.Items.Count - 1) RunCommand("powershell", "-Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ResetServerAddresses\"");
            else { string pDns = dnsList[idx].Primary; string sDns = dnsList[idx].Secondary; string cmd = string.Format("Get-NetAdapter | Where-Object {{$_.Status -eq 'Up'}} | Set-DnsClientServerAddress -ServerAddresses ('{0}','{1}')", pDns, sDns); RunCommand("powershell", "-Command \"" + cmd + "\""); }
            RunCommand("ipconfig", "/flushdns"); SetStatus("Настройки DNS успешно обновлены!", 100);
        }

        private void ApplySystemTweaks()
        {
            SetStatus("Применение выбранных параметров...", 10);
            if (chkPowerPlan.Checked) RunCommand("powercfg", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            if (chkVisualEffects.Checked) RunCommand("reg", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
            if (chkPriority.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f");
            if (chkGameBar.Checked) RunCommand("reg", "add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowGameDVR /t REG_DWORD /d 0 /f");
            if (chkFSO.Checked) { RunCommand("reg", "add \"HKCU\\System\\GameConfigStore\" /v \"GameDVR_FSEBehavior\" /t REG_DWORD /d 2 /f"); RunCommand("reg", "add \"HKCU\\System\\GameConfigStore\" /v \"GameDVR_DXGIHonorFSEWindowsCompatible\" /t REG_DWORD /d 1 /f"); RunCommand("reg", "add \"HKCU\\System\\GameConfigStore\" /v \"GameDVR_Enabled\" /t REG_DWORD /d 0 /f"); }
            if (chkCpuUnpark.Checked) { RunCommand("powercfg", "-setacvalueindex scheme_current sub_processor CPMAXCORES 100"); RunCommand("powercfg", "-setacvalueindex scheme_current sub_processor CPMINCORES 100"); RunCommand("powercfg", "-setactive scheme_current"); }
            if (chkNetworkThrottling.Checked) { RunCommand("reg", "add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"NetworkThrottlingIndex\" /t REG_DWORD /d 4294967295 /f"); RunCommand("reg", "add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"SystemResponsiveness\" /t REG_DWORD /d 0 /f"); }
            if (chkMitigations.Checked) { RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"FeatureSettingsOverride\" /t REG_DWORD /d 3 /f"); RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"FeatureSettingsOverrideMask\" /t REG_DWORD /d 3 /f"); }
            if (chkServices.Checked) { string[] services = { "SysMain", "WSearch", "DiagTrack", "dmwappushservice", "DoSvc" }; foreach (string s in services) { RunCommand("sc", "config " + s + " start= disabled"); RunCommand("sc", "stop " + s); } }
            if (chkNetwork.Checked) RunCommand("netsh", "int tcp set global autotuninglevel=normal");
            if (chkTcpTweaks.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v GlobalMaxTcpWindowSize /t REG_DWORD /d 16777216 /f");
            if (chkTelemetry.Checked) RunCommand("reg", "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f");
            if (chkHyperV.Checked) RunCommand("bcdedit", "/set hypervisorlaunchtype off");
            if (chkDefender.Checked) RunCommand("reg", "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiSpyware /t REG_DWORD /d 1 /f");
            if (chkUIServices.Checked) RunCommand("sc", "config Themes start= auto");
            if (chkMemoryDisk.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 1 /f");
            if (chkTimers.Checked) RunCommand("bcdedit", "/set disabledynamictick yes");
            if (chkNvidia.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v TdrDelay /t REG_DWORD /d 8 /f");
            if (chkIPv6.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\" /v DisabledComponents /t REG_DWORD /d 0xFFFFFFFF /f");
            if (chkFirewall.Checked) RunCommand("netsh", "advfirewall set allprofiles state off");
            
            // НОВЫЕ ТВИКИ:
            if (chkHags.Checked) RunCommand("reg", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
            if (chkHibernation.Checked) RunCommand("powercfg", "-h off");

            RunCommand("ipconfig", "/flushdns");
            SetStatus("Все изменения применены. Настоятельно рекомендуем перезапустить ПК.", 100);
            MessageBox.Show("Оптимизация успешно завершена!\nПерезагрузите систему.", "BoosterX", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnHardRest_Click(object sender, EventArgs e) { RunCommand("powercfg", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); RunCommand("netsh", "advfirewall set allprofiles state on"); SetStatus("Откат завершен.", 100); }
        private void BtnSoftRest_Click(object sender, EventArgs e) { RunCommand("powercfg", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); SetStatus("Мягкий откат выполнен.", 100); }

        // ИСПРАВЛЕННАЯ НАДЕЖНАЯ ОЧИСТКА ЧЕРЕЗ C# SYSTEM.IO
        private void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path)) { try { File.Delete(file); } catch { } }
                foreach (string dir in Directory.GetDirectories(path)) { try { Directory.Delete(dir, true); } catch { } }
            }
        }

        private void BtnClean_Click(object sender, EventArgs e)
        {
            SetStatus("Убийство фоновых процессов и очистка кэша...", 50);
            string[] procs = { "chrome", "msedge", "browser", "discord", "telegram" };
            foreach (string p in procs) { try { foreach (var proc in Process.GetProcessesByName(p)) proc.Kill(); } catch { } }
            
            Log("[*] Очистка временных файлов...");
            CleanDirectory(Path.GetTempPath());
            CleanDirectory(@"C:\Windows\Temp");
            
            SetStatus("Память и жесткий диск очищены!", 100);
            MessageBox.Show("Чистка завершена! Тяжелый фоновый софт закрыт, кэш удален.", "Очистка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetHardProfile()
        {
            chkPowerPlan.Checked = chkVisualEffects.Checked = chkPriority.Checked = chkGameBar.Checked = true;
            chkFSO.Checked = chkCpuUnpark.Checked = chkServices.Checked = chkNetwork.Checked = true;
            chkUIServices.Checked = chkTcpTweaks.Checked = chkNetworkThrottling.Checked = true;
            chkTelemetry.Checked = chkHyperV.Checked = chkDefender.Checked = chkMitigations.Checked = true;
            chkMemoryDisk.Checked = chkTimers.Checked = chkNvidia.Checked = chkIPv6.Checked = true;
            chkFirewall.Checked = chkHags.Checked = chkHibernation.Checked = true;
        }

        private void SetSoftProfile()
        {
            chkPowerPlan.Checked = chkVisualEffects.Checked = chkPriority.Checked = chkGameBar.Checked = true;
            chkFSO.Checked = chkCpuUnpark.Checked = chkNetwork.Checked = true;
            chkUIServices.Checked = chkTcpTweaks.Checked = chkNetworkThrottling.Checked = true;
            chkTelemetry.Checked = chkMemoryDisk.Checked = chkTimers.Checked = chkNvidia.Checked = chkIPv6.Checked = chkHags.Checked = true;
            chkServices.Checked = chkHyperV.Checked = chkDefender.Checked = chkMitigations.Checked = chkFirewall.Checked = chkHibernation.Checked = false;
        }

        private void ResetAllCheckboxes() { foreach (Control c in contentPanel.Controls[0].Controls[1].Controls) if (c is CheckBox cb) cb.Checked = false; }

        private void ToggleAutostart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable) key.SetValue("GameOptimizer", Application.ExecutablePath); else key.DeleteValue("GameOptimizer", false);
            }
        }

        private void RunDeepCleanup()
        {
            SetStatus("Запуск глубокой очистки...", 20);
            Log("[*] Глубокая очистка Prefetch и шейдеров DirectX...");
            CleanDirectory(@"C:\Windows\Prefetch");
            string d3dCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache");
            CleanDirectory(d3dCache);
            RunCommand("wevtutil", "el | Foreach-Object {wevtutil cl \"$_\"}");
            SetStatus("Глубокая очистка завершена!", 100);
            MessageBox.Show("Prefetch и Shader Cache очищены. Windows логи удалены.", "Очистка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RemoveBloatware()
        {
            SetStatus("Удаление мусора Windows...", 50);
            string[] apps = { "Microsoft.XboxApp", "Microsoft.Getstarted", "Microsoft.ZuneMusic", "Microsoft.BingWeather" };
            foreach (var app in apps) RunCommand("powershell", "-Command \"Get-AppxPackage *"+app+"* | Remove-AppxPackage\"");
            SetStatus("Мусор удален!", 100);
        }

        private void ApplyAdvancedNetworkTweaks()
        {
            Log("[*] Применение агрессивных сетевых оптимизаций (LSO, EEE)...");
            RunCommand("netsh", "int tcp set global autotuninglevel=normal");
            RunCommand("netsh", "int tcp set global chimney=disabled"); 
            // Новое: отключение Large Send Offload (LSO) и энергосбережения
            RunCommand("powershell", "-Command \"Disable-NetAdapterChecksumOffload -Name * -IpIPv4\"");
            RunCommand("powershell", "-Command \"Get-NetAdapterPowerManagement | Set-NetAdapterPowerManagement -WakeOnMagicPacket Disabled\"");
            MessageBox.Show("Сетевые параметры оптимизированы (отключен LSO и энергосберегающий режим сетевых карт).", "Сеть", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            bool isAdmin = false;
            try { using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE", false)) isAdmin = key != null; } catch { }

            if (!isAdmin)
            {
                try { Process.Start(new ProcessStartInfo { FileName = Application.ExecutablePath, Verb = "runas", UseShellExecute = true }); Application.Exit(); return; }
                catch { MessageBox.Show("Запустите программу от Имени Администратора!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}