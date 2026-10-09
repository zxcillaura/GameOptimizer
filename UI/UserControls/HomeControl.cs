using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Health;
using GameOptimizer.Core.Hardware;
using GameOptimizer.Core.Profiles;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.UI;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>Landing page: system score, what limits it, and the three profiles.</summary>
    public class HomeControl : UserControl
    {
        private RingGauge _ring;
        private RichTextBox _issues;
        private Label _verdict, _gradeLabel, _rigLine;
        private FlowLayoutPanel _topFixes;
        private FlatButton _scanBtn;
        private Panel _profileRow;
        private RigReport _rig;
        private HealthReport _health;
        private DateTime _lastScan = DateTime.MinValue;
        private Label _profileStatus;

        /// <summary>Set by MainForm so this page can switch to another tab.</summary>
        public Action<int> NavigateRequested;

        public HomeControl()
        {
            Build();
            Load += (_, __) => { if (_rig == null) Scan(); };
        }

        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 286));  // health
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));  // top fixes
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));  // profiles
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // rig
            Controls.Add(root);

            // ── header ───────────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var title = new Label
            {
                Text = "Система под контролем",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.Text, AutoSize = true, Location = new Point(0, 0)
            };
            _rigLine = new Label
            {
                Text = "Сканирую конфигурацию...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.Muted, AutoSize = true, Location = new Point(2, 30)
            };
            _scanBtn = new FlatButton
            {
                Text = "Пересканировать", IconKind = "bolt", Style = FlatButtonStyle.Soft,
                AccentColor = Theme.Accent, Size = new Size(168, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _scanBtn.Click += (_, __) => Scan(true);
            header.Controls.Add(title);
            header.Controls.Add(_rigLine);
            header.Controls.Add(_scanBtn);
            header.Resize += (_, __) => _scanBtn.Location = new Point(header.Width - _scanBtn.Width, 6);
            root.Controls.Add(header, 0, 0);

            // ── health card ──────────────────────────────────────────────
            var healthCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Оценка системы",
                Subtitle = "что именно ограничивает FPS", ShowAccent = true, AccentColor = Theme.Accent
            };
            var healthGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent
            };
            healthGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            healthGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            healthCard.Controls.Add(healthGrid);

            var ringHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _ring = new RingGauge
            {
                Dock = DockStyle.Fill, Value = 0, MaxValue = 100, Unit = "из 100",
                Caption = "оценка", AccentFrom = Theme.GradientStart, AccentTo = Theme.GradientEnd
            };
            ringHost.Controls.Add(_ring);
            healthGrid.Controls.Add(ringHost, 0, 0);

            var rightSide = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _verdict = new Label
            {
                Dock = DockStyle.Top, Height = 26, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Theme.Text
            };
            _issues = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Theme.Card, ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8.8f), ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true, DetectUrls = false
            };
            rightSide.Controls.Add(_issues);
            rightSide.Controls.Add(_verdict);
            healthGrid.Controls.Add(rightSide, 1, 0);
            root.Controls.Add(healthCard, 0, 1);

            // ── top three limiters ────────────────────────────────────────
            var fixesCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "ТОП-3 ограничителя",
                Subtitle = "самые заметные проблемы по результатам сканирования",
                AccentColor = Theme.Warning
            };
            _topFixes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0),
                AutoScroll = true
            };
            fixesCard.Controls.Add(_topFixes);
            root.Controls.Add(fixesCard, 0, 2);

            // ── profiles ─────────────────────────────────────────────────
            _profileRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            root.Controls.Add(_profileRow, 0, 3);
            BuildProfiles();

            // ── rig card ─────────────────────────────────────────────────
            var rigCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Конфигурация",
                Subtitle = BackupManager.Root, AccentColor = Theme.AccentAlt
            };
            _gradeLabel = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.TopLeft,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Consolas", 9f), Padding = new Padding(0, 4, 0, 0)
            };
            rigCard.Controls.Add(_gradeLabel);
            root.Controls.Add(rigCard, 0, 4);
        }

        /// <summary>
        /// Overview only. Applying profiles happens on the dedicated page, so the
        /// two screens no longer offer the same three buttons.
        /// </summary>
        private void BuildProfiles()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Профили оптимизации",
                Subtitle = "применяются на странице «Профили»",
                AccentColor = Theme.Accent, Highlight = true
            };

            _profileStatus = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.TopLeft,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 8.4f),
                Text = "читаю состояние..."
            };

            var open = new FlatButton
            {
                Text = "Открыть профили", IconKind = "bolt", Style = FlatButtonStyle.Filled,
                AccentColor = Theme.Accent, Dock = DockStyle.Bottom, Height = 34
            };
            open.Click += (_, __) => NavigateRequested?.Invoke(4);

            card.Controls.Add(_profileStatus);
            card.Controls.Add(open);
            _profileRow.Controls.Add(card);
        }

        private void UpdateProfileStatus()
        {
            var (profile, at, _) = BackupManager.LastProfile();
            string last = string.IsNullOrWhiteSpace(profile)
                ? "Профиль ещё ни разу не применялся в этой версии."
                : $"Последний проход: {ProfileLabel(profile)} — {at:dd.MM.yyyy HH:mm}";

            _profileStatus.Text =
                "Безопасный — только твики уровня «безопасно», минимум вмешательства.\r\n" +
                "Баланс     — безопасные + умеренные; рекомендуемый профиль.\r\n" +
                "Экстрим    — + рискованные твики (защита, VBS). Для изолированной игровой машины.\r\n" +
                "В каждой категории работает dry-run: посмотри список действий до применения.\r\n\r\n" +
                last;
        }

        private static string ProfileLabel(string p) => p switch
        {
            "Безопасный"        => "Безопасный",
            "Баланс"            => "Баланс",
            "Экстрим"           => "Экстрим",
            "Вернуть к заводским" => "Откат к заводским",
            "SAFE"              => "Безопасный (старая версия)",
            "BALANCED"          => "Баланс (старая версия)",
            "EXTREME"           => "Экстрим (старая версия)",
            _                   => p
        };


        private void Scan(bool force = false)
        {
            // a full scan spawns PowerShell + WMI; do not repeat it on every tab switch
            if (!force && _rig != null && (DateTime.Now - _lastScan).TotalSeconds < 60) return;

            _scanBtn.Enabled = false;
            _verdict.Text = "Сканирую...";
            Task.Run(() =>
            {
                var rig = HardwareScanner.Scan();
                int startup = CountStartup();
                bool overlays = System.Diagnostics.Process.GetProcessesByName("NVIDIA Overlay").Length > 0 ||
                                System.Diagnostics.Process.GetProcessesByName("Discord").Length > 0;
                var health = HealthReport.Evaluate(rig, startup, overlays);
                BeginInvoke(new Action(() =>
                {
                    _rig = rig; _health = health;
                    _lastScan = DateTime.Now;
                    ApplyHealth(rig, health);
                    _scanBtn.Enabled = true;
                }));
            });
        }

        private void ApplyHealth(RigReport rig, HealthReport health)
        {
            _ring.Value = health.Score;
            _ring.AccentFrom = Theme.ForScore(health.Score);
            _ring.AccentTo = health.Score >= 70 ? Theme.Success : Theme.Warning;
            _ring.UseScaleColor = false;
            _ring.BigText = $"{health.Grade}";
            _ring.Unit = $"{health.Score} из 100";

            _verdict.Text = health.Verdict;
            _verdict.ForeColor = Theme.ForScore(health.Score);
            BuildTopFixes(health);

            _issues.Clear();
            if (health.Issues.Count == 0)
            {
                AppendIssue("Всё в порядке", "Критичных ограничителей не найдено.", Theme.Success);
            }
            else
            {
                foreach (var i in health.Issues)
                    AppendIssue($"{i.Title}  (−{i.Penalty})", i.Detail,
                                i.Critical ? Theme.Danger : Theme.Warning);
            }

            _rigLine.Text = $"{rig.Cpu}  •  {rig.Gpu}  •  {rig.RamSummary.Split('\n')[0]}  •  {rig.Os}";

            _gradeLabel.Text = string.Join(Environment.NewLine, new[]
            {
                $"Процессор   : {rig.Cpu}",
                $"Видеокарта  : {rig.Gpu}   драйвер {rig.GpuDriver} ({rig.GpuDriverDate})",
                $"Память      : {rig.RamSummary.Replace("\n", "\n              ")}",
                $"Монитор     : {rig.Display}",
                $"Система     : {rig.Os}",
                $"Накопители  : {rig.DiskSummary}",
                $"Сеть        : {rig.NicSummary.Replace("\n", "  |  ")}",
                "",
                $"VBS: {(rig.VbsRunning ? "вкл" : "выкл")}   HVCI: {(rig.HvciRunning ? "вкл" : "выкл")}   " +
                $"HAGS: {(rig.HagsEnabled ? "вкл" : "выкл")}   ReBAR: {(rig.ReBarEnabled ? "вкл" : "н/д")}   " +
                $"Питание: {rig.ActivePowerPlan}"
            });

            UpdateProfileStatus();
            DarkMode.ApplyRecursive(this);   // dark scrollbar on the findings list
        }

        private void BuildTopFixes(HealthReport health)
        {
            _topFixes.Controls.Clear();
            var issues = health.Issues.Take(3).ToList();
            if (issues.Count == 0)
            {
                _topFixes.Controls.Add(new Label
                {
                    Text = "Ограничителей не найдено — система выглядит хорошо.",
                    ForeColor = Theme.Success, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    AutoSize = true, Margin = new Padding(6, 14, 0, 0)
                });
                return;
            }

            foreach (var issue in issues)
            {
                var card = new Panel { Width = 300, Height = 72, BackColor = Theme.Surface, Margin = new Padding(0, 0, 8, 0) };
                var title = new Label
                {
                    Text = $"{issue.Title}  (−{issue.Penalty})", ForeColor = Theme.Warning,
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    Location = new Point(8, 7), Size = new Size(285, 20), AutoEllipsis = true
                };
                var detail = new Label
                {
                    Text = issue.Detail, ForeColor = Theme.Muted,
                    Font = new Font("Segoe UI", 7.3f), Location = new Point(8, 28),
                    Size = new Size(issue.FixTweakId.Length > 0 ? 198 : 280, 35), AutoEllipsis = true
                };
                card.Controls.Add(title);
                card.Controls.Add(detail);
                if (!string.IsNullOrWhiteSpace(issue.FixTweakId))
                {
                    var fix = new FlatButton
                    {
                        Text = "Исправить", Style = FlatButtonStyle.Soft,
                        AccentColor = Theme.Accent, Size = new Size(78, 26), Location = new Point(214, 38)
                    };
                    string id = issue.FixTweakId;
                    fix.Click += (_, __) => ApplyFix(id, issue.Title);
                    card.Controls.Add(fix);
                }
                _topFixes.Controls.Add(card);
            }
        }

        private void ApplyFix(string id, string title)
        {
            var item = TweakCatalog.Build().FirstOrDefault(t => t.Id == id);
            if (item == null) { Toast.Show(FindForm(), "Твик для этой проблемы не найден", Theme.Warning); return; }
            if (!SystemTweaks.IsAdmin()) { Toast.Show(FindForm(), "Для исправления нужны права администратора", Theme.Danger, 5000); return; }
            Toast.Show(FindForm(), $"Исправляю: {title}...", Theme.Accent);
            Task.Run(() =>
            {
                var result = TweakRunner.ApplyOne(item, s => { });
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        Toast.Show(FindForm(), result.ok ? $"Исправлено: {title}" : $"Не удалось исправить: {title}",
                                   result.ok ? Theme.Success : Theme.Danger, 4200);
                        Scan(true);
                    }));
                }
                catch { }
            });
        }

        private void AppendIssue(string title, string detail, Color color)
        {
            _issues.SelectionStart = _issues.TextLength;
            _issues.SelectionLength = 0;
            _issues.SelectionColor = color;
            _issues.SelectionFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            _issues.AppendText("■  " + title + Environment.NewLine);
            _issues.SelectionFont = new Font("Segoe UI", 8.5f);
            _issues.SelectionColor = Theme.TextSecondary;
            _issues.AppendText("     " + detail + Environment.NewLine + Environment.NewLine);
        }

        private static int CountStartup()
        {
            int n = 0;
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                foreach (var path in new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
                })
                {
                    try
                    {
                        using var k = Reg.Open(hive, path, false);
                        if (k != null) n += k.GetValueNames().Length;
                    }
                    catch { }
                }
            }
            return n;
        }
    }
}
