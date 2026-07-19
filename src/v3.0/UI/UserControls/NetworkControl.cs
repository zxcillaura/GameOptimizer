using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Threading;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Managers;
using GameOptimizer.Core.Models;

namespace GameOptimizer.UI.UserControls
{
    public partial class NetworkControl : UserControl
    {
        private ComboBox _comboDNS;
        private Button _btnApplyDNS, _btnTestDNS;
        private ListView _listDns;
        private RichTextBox _logText;
        private Button _btnOptimizeAll;

        private TweakEngine _tweakEngine;
        private TableLayoutPanel _networkTweaksTable;
        private List<TweakDefinition> _networkTweaks;

        // ============================================================
        // DNS СПИСОК (35+ СЕРВЕРОВ)
        // ============================================================
        private struct DnsItem
        {
            public string Name;
            public string Primary;
            public string Secondary;
        }

        private DnsItem[] _dnsList = new DnsItem[]
        {
            // === ПОПУЛЯРНЫЕ ===
            new DnsItem { Name = "Cloudflare DNS", Primary = "1.1.1.1", Secondary = "1.0.0.1" },
            new DnsItem { Name = "Cloudflare Malware Blocking", Primary = "1.1.1.2", Secondary = "1.0.0.2" },
            new DnsItem { Name = "Cloudflare Adult Blocking", Primary = "1.1.1.3", Secondary = "1.0.0.3" },
            new DnsItem { Name = "Google Public DNS", Primary = "8.8.8.8", Secondary = "8.8.4.4" },
            new DnsItem { Name = "Quad9 Secure", Primary = "9.9.9.9", Secondary = "149.112.112.112" },
            new DnsItem { Name = "Quad9 Malware Blocking", Primary = "9.9.9.10", Secondary = "149.112.112.10" },
            new DnsItem { Name = "OpenDNS Home", Primary = "208.67.222.222", Secondary = "208.67.220.220" },
            new DnsItem { Name = "OpenDNS Family Shield", Primary = "208.67.222.123", Secondary = "208.67.220.123" },
            new DnsItem { Name = "Comodo Secure", Primary = "8.26.56.26", Secondary = "8.20.247.20" },

            // === РОССИЙСКИЕ ===
            new DnsItem { Name = "Yandex DNS (Basic)", Primary = "77.88.8.8", Secondary = "77.88.8.1" },
            new DnsItem { Name = "Yandex DNS (Safe)", Primary = "77.88.8.88", Secondary = "77.88.8.2" },
            new DnsItem { Name = "Yandex DNS (Family)", Primary = "77.88.8.7", Secondary = "77.88.8.3" },
            new DnsItem { Name = "Rostelecom DNS", Primary = "81.200.64.50", Secondary = "81.200.64.51" },
            new DnsItem { Name = "MTS DNS", Primary = "212.188.13.10", Secondary = "212.188.13.11" },

            // === ЕВРОПЕЙСКИЕ ===
            new DnsItem { Name = "AdGuard AdBlock", Primary = "94.140.14.14", Secondary = "94.140.15.15" },
            new DnsItem { Name = "AdGuard Family", Primary = "94.140.14.15", Secondary = "94.140.15.16" },
            new DnsItem { Name = "Qwant DNS", Primary = "176.31.202.100", Secondary = "176.31.202.101" },
            new DnsItem { Name = "Level3 DNS", Primary = "209.244.0.3", Secondary = "209.244.0.4" },
            new DnsItem { Name = "Verisign DNS", Primary = "64.6.64.6", Secondary = "64.6.65.6" },
            new DnsItem { Name = "OpenNIC DNS", Primary = "96.90.175.167", Secondary = "193.183.98.66" },

            // === АЗИАТСКИЕ ===
            new DnsItem { Name = "Alibaba DNS", Primary = "223.5.5.5", Secondary = "223.6.6.6" },
            new DnsItem { Name = "114DNS (China)", Primary = "114.114.114.114", Secondary = "114.114.115.115" },

            // === НЕМЕЦКИЕ ===
            new DnsItem { Name = "DNS.WATCH", Primary = "84.200.69.80", Secondary = "84.200.70.40" },
            new DnsItem { Name = "Freenom World", Primary = "80.80.80.80", Secondary = "80.80.81.81" },

            // === ПРОЧИЕ ===
            new DnsItem { Name = "Alternate DNS", Primary = "76.76.19.19", Secondary = "76.223.122.150" },
            new DnsItem { Name = "CleanBrowsing", Primary = "185.228.168.9", Secondary = "185.228.169.9" },
            new DnsItem { Name = "SafeDNS", Primary = "195.46.39.39", Secondary = "195.46.39.40" },
            new DnsItem { Name = "Neustar UltraDNS", Primary = "156.154.70.1", Secondary = "156.154.71.1" }
        };

        public NetworkControl()
        {
            InitializeComponent();
            this.BackColor = Theme.Current.Background;
            BuildUI();

            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tweaks.json");
            if (!File.Exists(jsonPath))
                jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tweaks.json");
            _tweakEngine = new TweakEngine(jsonPath);
            LoadNetworkTweaks();
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.BackColor = Theme.Current.Background;
            this.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 15, 20, 15)
            };
            mainPanel.RowStyles.Clear();
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 250)); // DNS часть
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Сетевые твики
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Лог

            // Заголовок
            var title = new Label
            {
                Text = "🌐 Сетевые настройки и DNS Jumper",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.Current.Foreground,
                AutoSize = true
            };
            mainPanel.Controls.Add(title, 0, 0);

            // ---- Блок DNS ----
            var dnsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Левая панель: выбор DNS
            var leftPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(320, 240),
                BackColor = Color.FromArgb(30, 30, 35)
            };
            dnsPanel.Controls.Add(leftPanel);

            var lblSelect = new Label
            {
                Text = "Выберите DNS сервер:",
                Location = new Point(15, 15),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            leftPanel.Controls.Add(lblSelect);

            _comboDNS = new ComboBox
            {
                Location = new Point(15, 40),
                Size = new Size(290, 23),
                BackColor = Color.FromArgb(20, 20, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var dns in _dnsList)
                _comboDNS.Items.Add(dns.Name);
            _comboDNS.Items.Add("Автоматический DNS (Сброс)");
            _comboDNS.SelectedIndex = 0;
            leftPanel.Controls.Add(_comboDNS);

            // КНОПКА ОБЩЕЙ ОПТИМИЗАЦИИ СЕТИ
            _btnOptimizeAll = new Button
            {
                Text = "⚡ ОПТИМИЗИРОВАТЬ СЕТЬ (все твики)",
                Location = new Point(15, 195),
                Size = new Size(290, 35),
                BackColor = Color.FromArgb(23, 162, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnOptimizeAll.Click += BtnOptimizeAll_Click;
            leftPanel.Controls.Add(_btnOptimizeAll);

            _btnApplyDNS = new Button
            {
                Text = "✅ Применить DNS",
                Location = new Point(15, 80),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnApplyDNS.Click += BtnApplyDNS_Click;
            leftPanel.Controls.Add(_btnApplyDNS);

            _btnTestDNS = new Button
            {
                Text = "⚡ Тест пинга",
                Location = new Point(165, 80),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnTestDNS.Click += BtnTestDNS_Click;
            leftPanel.Controls.Add(_btnTestDNS);

            var infoLabel = new Label
            {
                Text = "💡 Инструкция:\n1. Нажмите 'Тест пинга'\n2. Выберите лучший DNS\n3. Нажмите 'Применить DNS'",
                Location = new Point(15, 130),
                Size = new Size(290, 60),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            leftPanel.Controls.Add(infoLabel);

            // Правая панель: таблица DNS
            var rightPanel = new Panel
            {
                Location = new Point(330, 0),
                Size = new Size(340, 240),
                BackColor = Color.FromArgb(30, 30, 35)
            };
            dnsPanel.Controls.Add(rightPanel);

            _listDns = new ListView
            {
                Location = new Point(10, 10),
                Size = new Size(320, 220),
                BackColor = Color.FromArgb(20, 20, 22),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _listDns.Columns.Add("Сервер", 160);
            _listDns.Columns.Add("Основной IP", 100);
            _listDns.Columns.Add("Пинг", 60);
            rightPanel.Controls.Add(_listDns);

            foreach (var dns in _dnsList)
            {
                var item = new ListViewItem(dns.Name);
                item.SubItems.Add(dns.Primary);
                item.SubItems.Add("не проверено");
                _listDns.Items.Add(item);
            }

            mainPanel.Controls.Add(dnsPanel, 0, 1);

            // ---- Блок сетевых твиков ----
            var tweaksPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 22),
                Padding = new Padding(5)
            };

            var tweaksTitle = new Label
            {
                Text = "⚡ Сетевые твики (оптимизация пинга и сети)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30
            };
            tweaksPanel.Controls.Add(tweaksTitle);

            _networkTweaksTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 2,
                Padding = new Padding(0, 5, 0, 5)
            };
            _networkTweaksTable.ColumnStyles.Clear();
            _networkTweaksTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _networkTweaksTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            _networkTweaksTable.RowStyles.Clear();
            _networkTweaksTable.RowCount = 0;
            tweaksPanel.Controls.Add(_networkTweaksTable);

            mainPanel.Controls.Add(tweaksPanel, 0, 2);

            // ---- Лог ----
            var logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 17),
                Padding = new Padding(5)
            };
            _logText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 17),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            logPanel.Controls.Add(_logText);
            mainPanel.Controls.Add(logPanel, 0, 3);

            this.Controls.Add(mainPanel);
            Log("🌐 Network Control готов к работе.");
        }

        private void LoadNetworkTweaks()
        {
            _networkTweaks = _tweakEngine.GetTweaksByCategory("Network");
            _networkTweaksTable.Controls.Clear();
            _networkTweaksTable.RowStyles.Clear();
            _networkTweaksTable.RowCount = 0;

            if (_networkTweaks.Count == 0)
            {
                var empty = new Label
                {
                    Text = "Нет сетевых твиков в tweaks.json",
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _networkTweaksTable.Controls.Add(empty, 0, 0);
                _networkTweaksTable.SetColumnSpan(empty, 2);
                _networkTweaksTable.RowCount = 1;
                _networkTweaksTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
                return;
            }

            int rowIndex = 0;
            foreach (var tweak in _networkTweaks)
            {
                _networkTweaksTable.RowCount++;
                _networkTweaksTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // УВЕЛИЧЕНО С 50 ДО 65

                // Левая панель: название + описание
                var leftPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(28, 28, 32),
                    Margin = new Padding(0, 2, 3, 2)
                };

                // Название
                var nameLabel = new Label
                {
                    Text = tweak.DisplayName,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(10, 4),
                    AutoSize = true
                };
                leftPanel.Controls.Add(nameLabel);

                // Описание (с переносом)
                var descLabel = new Label
                {
                    Text = tweak.Description ?? "",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(160, 160, 170),
                    Location = new Point(10, 24),
                    AutoSize = true,
                    MaximumSize = new Size(500, 0)  // Ограничиваем ширину для переноса
                };
                leftPanel.Controls.Add(descLabel);

                _networkTweaksTable.Controls.Add(leftPanel, 0, rowIndex);

                // Правая панель: кнопки
                var rightPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(28, 28, 32),
                    Margin = new Padding(3, 2, 0, 2)
                };

                bool isApplied = _tweakEngine.IsTweakApplied(tweak);
                int btnY = 16; // кнопки по центру (65 - 32 / 2 = 16.5)

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
                    Log($"Применяем сетевой твик: {tweak.DisplayName}...");
                    if (_tweakEngine.ApplyTweak(tweak))
                    {
                        Log($"[УСПЕХ] {tweak.DisplayName} применён!");
                        btnApply.BackColor = Color.FromArgb(40, 110, 40);
                    }
                    else
                        Log($"[ОШИБКА] Не удалось применить {tweak.DisplayName}.");
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
                    Log($"Откатываем сетевой твик: {tweak.DisplayName}...");
                    if (_tweakEngine.RevertTweak(tweak))
                    {
                        Log($"[ОТКАТ] {tweak.DisplayName} возвращён.");
                        btnApply.BackColor = Color.FromArgb(50, 50, 60);
                    }
                    else
                        Log($"[ОШИБКА] Не удалось откатить {tweak.DisplayName}.");
                };
                rightPanel.Controls.Add(btnRevert);

                _networkTweaksTable.Controls.Add(rightPanel, 1, rowIndex);

                rowIndex++;
            }
        }

        // ============================================================
        // ОБЩАЯ ОПТИМИЗАЦИЯ СЕТИ (ВСЕ ТВИКИ)
        // ============================================================
        private async void BtnOptimizeAll_Click(object sender, EventArgs e)
        {
            Log("⚡ ЗАПУСК ОБЩЕЙ ОПТИМИЗАЦИИ СЕТИ...");
            _btnOptimizeAll.Enabled = false;
            _btnOptimizeAll.Text = "⏳ Применение...";

            int successCount = 0;
            int totalCount = _networkTweaks.Count;

            foreach (var tweak in _networkTweaks)
            {
                Log($"Применяем: {tweak.DisplayName}...");
                if (_tweakEngine.ApplyTweak(tweak))
                {
                    successCount++;
                    Log($"[УСПЕХ] {tweak.DisplayName}");
                }
                else
                {
                    Log($"[ОШИБКА] {tweak.DisplayName} — нужен запуск от Администратора!");
                }
            }

            // Применяем дополнительные сетевые оптимизации (netsh)
            Log("Применение дополнительных сетевых оптимизаций (netsh)...");
            await CommandExecutor.ExecuteAsync("netsh", "int tcp set global autotuninglevel=normal");
            await CommandExecutor.ExecuteAsync("netsh", "int tcp set global chimney=enabled");
            await CommandExecutor.ExecuteAsync("netsh", "int tcp set global dca=enabled");
            await CommandExecutor.ExecuteAsync("netsh", "int tcp set global netdma=enabled");

            Log($"✅ ОПТИМИЗАЦИЯ ЗАВЕРШЕНА! Применено {successCount}/{totalCount} твиков.");
            _btnOptimizeAll.Enabled = true;
            _btnOptimizeAll.Text = "⚡ ОПТИМИЗИРОВАТЬ СЕТЬ (все твики)";

            // Обновляем статус кнопок в таблице
            LoadNetworkTweaks();

            MessageBox.Show($"Сетевая оптимизация завершена!\nПрименено {successCount}/{totalCount} твиков.\nРекомендуется перезагрузить ПК.",
                "Оптимизация сети", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ============================================================
        // DNS МЕТОДЫ
        // ============================================================
        private async void BtnApplyDNS_Click(object sender, EventArgs e)
        {
            int idx = _comboDNS.SelectedIndex;
            Log($"🔄 Применение DNS: {_comboDNS.Text}...");

            if (idx == _comboDNS.Items.Count - 1)
            {
                await CommandExecutor.ExecuteAsync("powershell",
                    "-Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ResetServerAddresses\"");
                Log("✅ DNS сброшен на автоматический (DHCP)");
            }
            else
            {
                string primary = _dnsList[idx].Primary;
                string secondary = _dnsList[idx].Secondary;
                string cmd = $"Get-NetAdapter | Where-Object {{$_.Status -eq 'Up'}} | Set-DnsClientServerAddress -ServerAddresses ('{primary}','{secondary}')";
                await CommandExecutor.ExecuteAsync("powershell", $"-Command \"{cmd}\"");
                Log($"✅ DNS применён: {_dnsList[idx].Name} ({primary})");
            }

            await CommandExecutor.ExecuteAsync("ipconfig", "/flushdns");
            Log("✅ DNS кэш сброшен");
        }

        private void BtnTestDNS_Click(object sender, EventArgs e)
        {
            _btnTestDNS.Enabled = false;
            _btnTestDNS.Text = "⏳ Тестирование...";
            Log($"📡 Запуск теста пинга {_dnsList.Length} DNS серверов...");

            Thread dnsTestThread = new Thread(() =>
            {
                for (int i = 0; i < _dnsList.Length; i++)
                {
                    string ip = _dnsList[i].Primary;
                    long pingMs = -1;
                    try
                    {
                        using (Ping pingSender = new Ping())
                        {
                            PingReply reply = pingSender.Send(ip, 1000);
                            if (reply.Status == IPStatus.Success)
                                pingMs = reply.RoundtripTime;
                        }
                    }
                    catch { }

                    int index = i;
                    this.Invoke(new Action(() =>
                    {
                        if (pingMs >= 0)
                        {
                            _listDns.Items[index].SubItems[2].Text = pingMs + " ms";
                            if (pingMs < 25) _listDns.Items[index].SubItems[2].ForeColor = Color.Green;
                            else if (pingMs < 75) _listDns.Items[index].SubItems[2].ForeColor = Color.Orange;
                            else _listDns.Items[index].SubItems[2].ForeColor = Color.Red;
                        }
                        else
                        {
                            _listDns.Items[index].SubItems[2].Text = "❌ Таймаут";
                            _listDns.Items[index].SubItems[2].ForeColor = Color.Gray;
                        }
                    }));
                }

                this.Invoke(new Action(() =>
                {
                    long bestPing = long.MaxValue;
                    int bestIndex = -1;

                    for (int i = 0; i < _dnsList.Length; i++)
                    {
                        string text = _listDns.Items[i].SubItems[2].Text;
                        if (text.Contains("ms") && long.TryParse(text.Replace(" ms", ""), out long ping))
                        {
                            if (ping < bestPing)
                            {
                                bestPing = ping;
                                bestIndex = i;
                            }
                        }
                    }

                    _btnTestDNS.Enabled = true;
                    _btnTestDNS.Text = "⚡ Тест пинга";

                    if (bestIndex != -1)
                    {
                        _comboDNS.SelectedIndex = bestIndex;
                        Log($"✅ Лучший DNS: {_dnsList[bestIndex].Name} - {bestPing} ms");
                        MessageBox.Show($"Самый быстрый DNS: {_dnsList[bestIndex].Name} ({bestPing} мс)\nОн автоматически выбран в списке.",
                            "DNS Jumper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Log("⚠️ Все DNS серверы не ответили");
                    }
                }));
            });

            dnsTestThread.IsBackground = true;
            dnsTestThread.Start();
        }

        private void Log(string message)
        {
            if (_logText != null && !_logText.IsDisposed)
                _logText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "NetworkControl";
            this.Size = new Size(760, 540);
            this.ResumeLayout(false);
        }
    }
}