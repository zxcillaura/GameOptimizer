using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Hardware;
using GameOptimizer.Core.Latency;
using GameOptimizer.Core.Profiles;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// 1.0 — вкладка «Профили». Три профиля (Заводские / Баланс / Экстрим)
    /// × выбор категорий галочками: ПК, Сеть, Мышь и клава, Система.
    /// Можно оптимизировать только то, что нужно — например, одну мышь.
    /// </summary>
    public class AutoControl : UserControl
    {
        private RichTextBox _log;
        private Label       _status;
        private FlowLayoutPanel _profileActions;
        private Button      _factoryBtn, _balancedBtn, _extremeBtn, _previewBtn, _latencyBtn, _purgeBtn;
        private bool        _busy;

        private readonly Dictionary<string, CheckBox> _cats = new();

        public AutoControl()
        {
            BuildUi();
            Append("Optimization by zxcillaura — профили оптимизации.", Theme.Accent);
            Append("Выбери, ЧТО оптимизировать (галочки ниже), затем профиль: Баланс или Экстрим.", Theme.TextSecondary);
            Append("Перед применением создаётся точка восстановления и бэкап — всё обратимо кнопкой «Вернуть к заводским».", Theme.TextSecondary);
            RefreshLatencyLabel();
        }

        // ── layout ───────────────────────────────────────────────────────
        private void BuildUi()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding   = new Padding(16);

            // hero
            var hero = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Theme.Card, Padding = new Padding(14, 10, 14, 10) };
            hero.Controls.Add(new Label
            {
                Text = "Оптимизация по профилям",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.Text, Dock = DockStyle.Top, Height = 26
            });
            hero.Controls.Add(new Label
            {
                Text = "Сначала выбери категории, потом профиль. Каждый профиль применит только отмеченные разделы.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill
            });
            // Dock.Top added after Fill so the title stays on top
            var heroTitle = hero.Controls[0];
            hero.Controls.SetChildIndex(heroTitle, 0);

            // category cards (checkboxes with per-section captions)
            var catWrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 150, BackColor = Theme.Background,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 10, 0, 4)
            };
            foreach (var c in ProfileRunner.Categories)
            {
                var card = new Panel { Width = 288, Height = 64, BackColor = Theme.Card, Margin = new Padding(0, 0, 10, 10) };
                var cb = new CheckBox
                {
                    Text = c.Title, Checked = true, Tag = c.Key,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Theme.Text, BackColor = Theme.Card,
                    FlatStyle = FlatStyle.Flat, AutoSize = false,
                    Location = new Point(10, 8), Size = new Size(268, 22)
                };
                cb.FlatAppearance.BorderColor = Theme.Border;
                var desc = new Label
                {
                    Text = c.Desc, Font = new Font("Segoe UI", 8f),
                    ForeColor = Theme.Muted, BackColor = Theme.Card,
                    Location = new Point(12, 32), Size = new Size(268, 28)
                };
                card.Controls.Add(cb);
                card.Controls.Add(desc);
                catWrap.Controls.Add(card);
                _cats[c.Key] = cb;
            }

            // profile actions wrap on narrow/high-DPI windows instead of clipping.
            _profileActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 8, 0, 4),
                BackColor = Theme.Background, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, AutoScroll = true
            };
            _balancedBtn = MakeButton("БАЛАНС",             132, Theme.SoftColor, Theme.SoftHover);
            _extremeBtn  = MakeButton("ЭКСТРИМ",            132, Theme.HardColor, Theme.HardHover);
            _previewBtn  = MakeButton("ПРЕДПРОСМОТР",       150, Theme.AccentPressed, Theme.AccentHover);
            _factoryBtn  = MakeButton("ВЕРНУТЬ К ЗАВОДСКИМ",190, Theme.Surface,   Theme.Border);
            _latencyBtn  = MakeButton("LATENCY",            112, Theme.Surface,   Theme.Border);
            _purgeBtn    = MakeButton("PURGE RAM",          112, Theme.Surface,   Theme.Border);

            foreach (var b in new[] { _balancedBtn, _extremeBtn, _previewBtn, _factoryBtn, _latencyBtn, _purgeBtn })
            {
                b.Margin = new Padding(0, 0, 8, 4);
                _profileActions.Controls.Add(b);
            }

            _status = new Label
            {
                Dock = DockStyle.Top, Height = 24, ForeColor = Theme.Muted,
                Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleLeft
            };

            _log = new RichTextBox
            {
                Dock = DockStyle.Fill, BackColor = Theme.LogBg, ForeColor = Theme.LogFg,
                Font = new Font("Consolas", 9f), ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle, WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            Controls.Add(_log);
            Controls.Add(_status);
            Controls.Add(_profileActions);
            Controls.Add(catWrap);
            Controls.Add(hero);

            _balancedBtn.Click += (_, __) => Apply(ProfileKind.Balanced);
            _extremeBtn.Click  += (_, __) => Apply(ProfileKind.Extreme);
            _previewBtn.Click  += (_, __) => ShowPreview(ProfileKind.Balanced);
            _factoryBtn.Click  += (_, __) => Apply(ProfileKind.Factory);
            _latencyBtn.Click  += (_, __) => ToggleLatency();
            _purgeBtn.Click    += (_, __) => Purge();
        }

        private Button MakeButton(string text, int width, Color back, Color hover)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 30, Top = 8,
                FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            return b;
        }

        // ── actions ──────────────────────────────────────────────────────
        private string[] SelectedCategories() =>
            _cats.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToArray();

        private void Apply(ProfileKind profile)
        {
            var keys = SelectedCategories();
            if (keys.Length == 0)
            {
                Append("Не выбрано ни одной категории — отметь галочкой, что оптимизировать.", Theme.Warning);
                return;
            }
            if (!SystemTweaks.IsAdmin())
            {
                Append("Профиль не запущен: нужны права администратора. Так система не останется изменённой только наполовину.", Theme.Danger);
                return;
            }

            string catNames = string.Join(", ",
                ProfileRunner.Categories.Where(c => keys.Contains(c.Key)).Select(c => c.Title));
            var preview = BuildPreview(profile, keys);
            string details = PreviewSummary(preview);

            string warn = profile switch
            {
                ProfileKind.Extreme => $"ЭКСТРИМ для категорий:\n{catNames}\n\n{details}\n\n" +
                    "Экстрим отключает отдельные защитные механизмы и может нарушить работу WSL2, виртуальных машин и античитов. Используй только после сравнения бенчмарков.\n\nПродолжить?",
                ProfileKind.Balanced => $"БАЛАНС для категорий:\n{catNames}\n\n{details}\n\n" +
                    "Будет создан бэкап и запрошена точка восстановления. Эффект зависит от железа и игры — сравни замеры до/после.\n\nПродолжить?",
                _ => $"ВЕРНУТЬ К ЗАВОДСКИМ для категорий:\n{catNames}\n\n{details}\n\n" +
                    "Будут восстановлены исходные значения только тех твиков, для которых есть бэкап.\n\nПродолжить?"
            };

            if (MessageBox.Show(warn, $"Optimization by zxcillaura — {ProfileRunner.Title(profile)}",
                    MessageBoxButtons.YesNo, profile == ProfileKind.Extreme ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes) return;

            RunAsync($"{ProfileRunner.Title(profile)}: {catNames}...", () =>
            {
                ProfileRunner.Run(profile, keys, withRestorePoint: profile != ProfileKind.Factory, s => Append(s));
            });
        }

        private ProfilePreview BuildPreview(ProfileKind profile, string[] keys)
            => ProfileRunner.Preview(profile, keys, HardwareScanner.Scan());

        private static string PreviewSummary(ProfilePreview preview) =>
            $"Будет обработано: {preview.Items.Count}\n" +
            $"• безопасно: {preview.SafeCount}\n" +
            $"• осторожно: {preview.CautionCount}\n" +
            $"• высокий риск: {preview.RiskyCount}\n" +
            $"• требуют перезагрузки: {preview.RebootCount}\n" +
            $"• пропущено как неприменимое: {preview.Skipped.Count}";

        private void ShowPreview(ProfileKind profile)
        {
            var keys = SelectedCategories();
            if (keys.Length == 0)
            {
                Append("Для предпросмотра выбери хотя бы одну категорию.", Theme.Warning);
                return;
            }

            RunAsync("Формирую безопасный предпросмотр без изменений...", () =>
            {
                var preview = BuildPreview(profile, keys);
                Append("", Theme.TextSecondary);
                Append($"ПРЕДПРОСМОТР «{ProfileRunner.Title(profile)}»", Theme.Accent);
                Append(PreviewSummary(preview), Theme.TextSecondary);
                foreach (var group in preview.Items.GroupBy(t => t.Group))
                {
                    Append($"\n{group.Key.ToUpperInvariant()}:", Theme.AccentAlt);
                    foreach (var t in group)
                    {
                        string risk = t.Risk == TweakRisk.Safe ? "безопасно" : t.Risk == TweakRisk.Caution ? "осторожно" : "РИСК";
                        Append($"  • {t.Title} [{risk}]{(t.RequiresReboot ? " · перезагрузка" : "")}");
                    }
                }
                if (preview.Skipped.Count > 0)
                {
                    Append("\nПРОПУЩЕНО:", Theme.Warning);
                    foreach (var reason in preview.Skipped) Append("  • " + reason, Theme.Muted);
                }
                Append("\nПредпросмотр ничего не изменил в системе.", Theme.Success);
            });
        }

        private void ToggleLatency()
        {
            if (LatencyKeeper.IsRunning) LatencyKeeper.Stop(s => Append(s));
            else
            {
                LatencyKeeper.Start(s => Append(s));
                Append("Держу таймер 0.5 мс и чищу standby, пока запущены игры.", Theme.TextSecondary);
            }
            RefreshLatencyLabel();
        }

        private void Purge()
        {
            bool ok = LatencyKeeper.PurgeStandby();
            Append(ok ? "Список ожидания памяти очищен." : "Не удалось (нужны права администратора).",
                   ok ? Theme.Success : Theme.Danger);
        }

        private void RefreshLatencyLabel()
        {
            _status.Text = $"LATENCY: {LatencyKeeper.Status}   |   бэкапы: {BackupManager.BackupDir}";
        }

        // ── helpers ──────────────────────────────────────────────────────
        private void RunAsync(string title, Action work)
        {
            if (_busy) { Append("Дождись завершения текущей операции.", Theme.Warning); return; }
            _busy = true;
            _status.Text = title;
            SetButtons(false);

            Task.Run(() =>
            {
                try { work(); }
                catch (Exception ex) { Append($"[ОШИБКА] {ex.Message}", Theme.Danger); }
                finally
                {
                    BeginInvoke(new Action(() =>
                    {
                        _busy = false;
                        SetButtons(true);
                        RefreshLatencyLabel();
                    }));
                }
            });
        }

        private void SetButtons(bool enabled)
        {
            foreach (var b in new[] { _factoryBtn, _balancedBtn, _extremeBtn, _previewBtn, _latencyBtn, _purgeBtn })
                b.Enabled = enabled;
        }

        private void Append(string text, Color? color = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => Append(text, color))); return; }

            _log.SelectionStart  = _log.TextLength;
            _log.SelectionLength = 0;
            _log.SelectionColor  = color ?? Theme.LogFg;
            _log.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
            _log.SelectionColor  = _log.ForeColor;
            _log.ScrollToCaret();
        }

        public static bool AdminOk => SystemTweaks.IsAdmin();
    }
}
