using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;
using GameOptimizer.UI;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    public class NetworkControl : UserControl
    {
        private ComboBox    _comboDns;
        private Button      _btnApplyDns, _btnResetDns, _btnTestDns;
        private Label       _lblPingResult, _lblComparison;
        private Sparkline   _pingSparkline;
        private Button      _btnCompare;
        private GameOptimizer.Core.Diagnostics.NetworkTestResult _beforeTest;
        private Button      _btnNoDelay, _btnThrottle, _btnUdp, _btnRss, _btnResetStack, _btnPingServers;
        private RichTextBox _log;

        private struct DnsItem { public string Name, Primary, Secondary; }
        private readonly DnsItem[] _dns =
        {
            new() { Name = "Cloudflare (проверить локально)", Primary = "1.1.1.1",         Secondary = "1.0.0.1"         },
            new() { Name = "Google Public DNS",           Primary = "8.8.8.8",         Secondary = "8.8.4.4"         },
            new() { Name = "Quad9 (Безопасный)",          Primary = "9.9.9.9",         Secondary = "149.112.112.112" },
            new() { Name = "OpenDNS Home",                Primary = "208.67.222.222",  Secondary = "208.67.220.220"  },
            new() { Name = "AdGuard (Без рекламы)",       Primary = "94.140.14.14",    Secondary = "94.140.15.15"    },
            new() { Name = "Яндекс Базовый",              Primary = "77.88.8.8",       Secondary = "77.88.8.1"       },
            new() { Name = "Яндекс Безопасный",           Primary = "77.88.8.88",      Secondary = "77.88.8.2"       },
            new() { Name = "Comodo Secure",               Primary = "8.26.56.26",      Secondary = "8.20.247.20"     },
            new() { Name = "Level3 DNS",                  Primary = "209.244.0.3",     Secondary = "209.244.0.4"     },
            new() { Name = "Verisign DNS",                Primary = "64.6.64.6",       Secondary = "64.6.65.6"       },
            new() { Name = "Alibaba DNS",                 Primary = "223.5.5.5",       Secondary = "223.6.6.6"       },
            new() { Name = "DNS.WATCH",                   Primary = "84.200.69.80",    Secondary = "84.200.70.40"    },
            new() { Name = "CleanBrowsing",               Primary = "185.228.168.9",   Secondary = "185.228.169.9"   },
            new() { Name = "Ростелеком",                  Primary = "81.200.64.50",    Secondary = "81.200.64.51"    },
            new() { Name = "МТС DNS",                     Primary = "212.188.13.10",   Secondary = "212.188.13.11"   },
        };

        public NetworkControl()
        {
            DoubleBuffered = true;
            Dock           = DockStyle.Fill;
            BackColor      = Theme.Background;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            var outer = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(24, 16, 24, 16)
            };
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // title
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));     // DNS card
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));     // tweak buttons card
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));     // log

            // ── Title ──────────────────────────────────────────────────────
            outer.Controls.Add(UIHelpers.MakeSectionTitle("Сеть — диагностика и DNS"), 0, 0);

            // ── DNS Card ───────────────────────────────────────────────────
            var dnsCard = new RoundedPanel
            {
                BackColor    = Theme.Card,
                BorderColor  = Theme.Border,
                CornerRadius = 6,
                Dock         = DockStyle.Fill,
                Padding      = new Padding(14, 0, 14, 0),
                Margin       = new Padding(0, 0, 0, 10)
            };

            // Use TableLayoutPanel so columns share space cleanly — no overflow
            var dnsRow = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 5,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            dnsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            dnsRow.ColumnCount = 6;
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));     // label
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // combo
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Apply DNS
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Reset
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Ping + result
            dnsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // sparkline

            var lblDns = new Label
            {
                Text      = "DNS Сервер:",
                ForeColor = Theme.Text,
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                Dock      = DockStyle.None,
                Anchor    = AnchorStyles.Left,
                Margin    = new Padding(0, 0, 8, 0)
            };

            _comboDns = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9f),
                BackColor     = Theme.Card,
                ForeColor     = Theme.Text,
                FlatStyle     = FlatStyle.Flat,
                Margin        = new Padding(0, 0, 8, 0)
            };
            foreach (var d in _dns) _comboDns.Items.Add(d.Name);
            _comboDns.SelectedIndex = 0;

            _btnApplyDns = MakeBtn("Применить DNS", Theme.Accent, BtnApplyDns_Click);
            _btnResetDns = MakeBtn("Сбросить (DHCP)", Theme.Card, BtnResetDns_Click);

            // Ping button + result in a small panel to stay in one column
            var pingPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _btnTestDns = MakeBtn("Тест ping/jitter", Theme.Card, BtnTestDns_Click);
            _btnTestDns.Dock = DockStyle.Left;
            _lblPingResult = new Label
            {
                Text      = "",
                ForeColor = Theme.Success,
                Font      = new Font("Consolas", 9f),
                AutoSize  = true,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(6, 0, 0, 0)
            };
            pingPanel.Controls.Add(_lblPingResult);
            pingPanel.Controls.Add(_btnTestDns);

            dnsRow.Controls.Add(lblDns,       0, 0);
            dnsRow.Controls.Add(_comboDns,    1, 0);
            dnsRow.Controls.Add(_btnApplyDns, 2, 0);
            dnsRow.Controls.Add(_btnResetDns, 3, 0);
            dnsRow.Controls.Add(pingPanel,    4, 0);
            _pingSparkline = new Sparkline { Dock = DockStyle.Fill, Margin = new Padding(6, 12, 0, 12) };
            dnsRow.Controls.Add(_pingSparkline, 5, 0);

            dnsCard.Controls.Add(dnsRow);
            _lblComparison = new Label
            {
                Text = "Сравнение: сначала выполни тест ДО, затем примени изменения и нажми «Тест ПОСЛЕ».",
                ForeColor = Theme.Muted, Font = new Font("Segoe UI", 8.2f),
                Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.MiddleLeft
            };
            _btnCompare = MakeBtn("Тест ПОСЛЕ", Theme.Card, BtnCompare_Click);
            _btnCompare.Dock = DockStyle.Right;
            _btnCompare.Width = 120;
            dnsCard.Controls.Add(_lblComparison);
            dnsCard.Controls.Add(_btnCompare);
            outer.Controls.Add(dnsCard, 0, 1);

            // ── Tweak Buttons Card ─────────────────────────────────────────
            var tweakCard = new RoundedPanel
            {
                BackColor    = Theme.Card,
                BorderColor  = Theme.Border,
                CornerRadius = 6,
                Dock         = DockStyle.Fill,
                Padding      = new Padding(14, 10, 14, 10),
                Margin       = new Padding(0, 0, 0, 10)
            };

            var tweakTitle = new Label
            {
                Text      = "Расширенные операции — только по диагностике",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                Dock      = DockStyle.Top,
                Height    = 20
            };

            // 2-row TableLayout for 6 buttons — 3 per row, fills width perfectly
            var btnGrid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 2,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0, 4, 0, 0)
            };
            btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _btnNoDelay    = MakeTweakBtn("TCP NoDelay",              "Только диагностика — не снижает ping универсально",       (s, e) => ConfirmNetworkAction("TCP NoDelay может ухудшить throughput. Продолжить?", () => NetworkOptimizer.ApplyTCPNoDelay(Log)));
            _btnThrottle   = MakeTweakBtn("Без троттлинга",           "Системный твик без гарантии снижения задержки",            (s, e) => ConfirmNetworkAction("Изменение системного throttling обратимо, но не гарантирует улучшение. Продолжить?", () => NetworkOptimizer.DisableNetworkThrottling(Log)));
            _btnUdp        = MakeTweakBtn("UDP Буферы",                "Только после подтверждения потерь пакетов",               (s, e) => ConfirmNetworkAction("Изменить UDP-буферы? Результат зависит от драйвера и игры.", () => NetworkOptimizer.OptimizeUDPBuffers(Log)));
            _btnRss        = MakeTweakBtn("Включить RSS",              "Распределяет сетевые прерывания по ядрам CPU",             (s, e) => ConfirmNetworkAction("Включить RSS для активного адаптера?", () => NetworkOptimizer.EnableRSS(Log)));
            _btnPingServers  = MakeTweakBtn("Пинг-тест серверов",          "Пинг к 6 публичным серверам — какой канал стабильнее",        (s, e) => BtnPingServers_Click());
            _btnResetStack = MakeTweakBtn("Сброс стека Winsock",         "Только для диагностики серьёзных сетевых проблем",        (s, e) => ConfirmNetworkAction("Сбросить Winsock/IP? Потребуется перезагрузка Windows.", () => NetworkOptimizer.ResetNetworkStack(Log)));

            btnGrid.Controls.Add(_btnNoDelay,    0, 0);
            btnGrid.Controls.Add(_btnThrottle,   1, 0);
            btnGrid.Controls.Add(_btnUdp,        2, 0);
            btnGrid.Controls.Add(_btnRss,        0, 1);
            btnGrid.Controls.Add(_btnPingServers, 1, 1);
            btnGrid.Controls.Add(_btnResetStack, 2, 1);

            tweakCard.Controls.Add(btnGrid);
            tweakCard.Controls.Add(tweakTitle);
            outer.Controls.Add(tweakCard, 0, 2);

            // ── Log ────────────────────────────────────────────────────────
            _log = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.LogBg,
                ForeColor   = Theme.LogFg,
                Font        = new Font("Consolas", 9f),
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };
            outer.Controls.Add(_log, 0, 3);
            Controls.Add(outer);
            Log("Сетевой модуль готов.");
        }

        private void BtnApplyDns_Click(object? s, EventArgs e)
        {
            int idx = _comboDns.SelectedIndex;
            if (idx < 0) return;
            var d = _dns[idx];
            Log($"Применяю DNS: {d.Name} ({d.Primary} / {d.Secondary})");
            Task.Run(() => NetworkOptimizer.SetCustomDNS(d.Primary, d.Secondary, Log));
        }

        private void BtnResetDns_Click(object? s, EventArgs e)
        {
            Log("Сброс DNS на автоматический (DHCP)...");
            Task.Run(() => NetworkOptimizer.ResetDNSToAuto(Log));
        }

        private void BtnTestDns_Click(object? s, EventArgs e)
        {
            Log("Проверяю адаптер, шлюз, DNS и потерю пакетов...");
            _lblPingResult.Text = "...";
            Task.Run(() =>
            {
                var result = GameOptimizer.Core.Diagnostics.NetworkDiagnostics.Run();
                this.Invoke(new Action(() =>
                {
                    _beforeTest = result;
                    _lblPingResult.Text = result.GatewayMs >= 0 ? $"GW {result.GatewayMs} / DNS {result.DnsMs} мс" : "Нет ответа";
                    _lblPingResult.ForeColor = result.PacketLoss == 0 ? Theme.Success : Theme.Warning;
                    _pingSparkline.SetSamples(result.Samples);
                    _lblComparison.Text = "Тест ДО сохранён. Примени настройки и нажми «Тест ПОСЛЕ».";
                    Log($"Адаптер: {result.Adapter}; шлюз: {result.Gateway}; потеря: {result.PacketLoss}%; jitter: {result.JitterMs} мс");
                    Log(result.StageSummary);
                    Log(result.Summary);
                }));
            });
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            if (_beforeTest == null)
            {
                Log("Сначала нажми «Тест ping/jitter» — это будет замер ДО.");
                return;
            }
            _btnCompare.Enabled = false;
            Log("Снимаю замер ПОСЛЕ для сравнения...");
            Task.Run(() =>
            {
                var after = GameOptimizer.Core.Diagnostics.NetworkDiagnostics.Run();
                var comparison = GameOptimizer.Core.Diagnostics.NetworkDiagnostics.Compare(_beforeTest, after);
                try
                {
                    Invoke(new Action(() =>
                    {
                        _btnCompare.Enabled = true;
                        _lblComparison.Text = "Вердикт: " + comparison.Verdict;
                        _lblComparison.ForeColor = comparison.Verdict.StartsWith("Стало лучше", StringComparison.OrdinalIgnoreCase)
                            ? Theme.Success : comparison.Verdict.StartsWith("Стало хуже", StringComparison.OrdinalIgnoreCase)
                                ? Theme.Warning : Theme.Muted;
                        Log("ПОСЛЕ: " + after.StageSummary);
                        Log(comparison.Verdict);
                    }));
                }
                catch { }
            });
        }

        private void BtnPingServers_Click()
        {
            Log("Пинг-тест публичных серверов (по одному пакету на каждый)...");
            Task.Run(() =>
            {
                var targets = new (string Name, string Ip)[]
                {
                    ("Cloudflare", "1.1.1.1"),
                    ("Google",     "8.8.8.8"),
                    ("AdGuard",    "94.140.14.14"),
                    ("Яндекс",     "77.88.8.8"),
                    ("Selectel",   "176.103.5.6"),
                    ("МТС",        "212.188.13.10"),
                };
                long bestMs = -1; string bestName = "";
                foreach (var t in targets)
                {
                    long ms = NetworkOptimizer.PingServer(t.Ip);
                    Log($"{t.Name} ({t.Ip}): {(ms >= 0 ? ms + " мс" : "нет ответа")}");
                    if (ms >= 0 && (bestMs < 0 || ms < bestMs)) { bestMs = ms; bestName = t.Name; }
                }
                if (bestMs >= 0) Log($"Лучший ответ: {bestName} — {bestMs} мс");
                else Log("Ни один сервер не ответил — проверь интернет/фаервол.");
            });
        }

        private void ConfirmNetworkAction(string message, Action action)
        {
            if (MessageBox.Show(message + "\n\nGAMEOPTIMIZ 4.0 не обещает универсального снижения ping.", "Сетевое действие", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Task.Run(action);
        }

        private Button MakeBtn(string text, Color bg, EventHandler click)
        {
            var btn = new Button
            {
                Text      = text,
                Dock      = DockStyle.Fill,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = bg == Theme.Card ? 1 : 0;
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += click;
            return btn;
        }

        private Button MakeTweakBtn(string title, string tooltip, EventHandler click)
        {
            var btn = new Button
            {
                Text      = title,
                Dock      = DockStyle.Fill,
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 6, 6)
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 90, 140, 255);
            btn.Click += click;
            new ToolTip().SetToolTip(btn, tooltip);
            return btn;
        }

        private void Log(string msg)
        {
            if (_log.InvokeRequired) { _log.Invoke(new Action(() => Log(msg))); return; }
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _log.ScrollToCaret();
        }
    }
}
