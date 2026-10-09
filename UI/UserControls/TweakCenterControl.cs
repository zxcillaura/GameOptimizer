using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.UI;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// Control centre: every change listed individually with its live state,
    /// its own switch and its own undo — no bundled "one button" magic.
    /// </summary>
    public class TweakCenterControl : UserControl
    {
        private FlowLayoutPanel _list, _chips;
        private RichTextBox _log;
        private TextBox _search;
        private Label _counter;
        private FlatButton _refreshBtn, _onAllBtn, _offAllBtn;

        private readonly List<TweakRow> _rows = new();
        private string _activeGroup = "Все";
        private bool _busy;
        private bool _loaded;

        private static readonly string[] Groups =
        {
            "Все", "Питание", "Латентность", "Память", "Графика", "Мышь и клава", "Сеть", "GPU", "Игры", "Система", "Службы", "Задачи"
        };

        public TweakCenterControl()
        {
            Build();
            Load += (_, __) => { if (!_loaded) { _loaded = true; LoadAll(); } };
        }

        // ── layout ──────────────────────────────────────────────────────
        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            Controls.Add(root);

            // ── toolbar ──────────────────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var topLine = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            _search = new TextBox
            {
                Width = 250, Height = 28, Location = new Point(0, 3),
                BackColor = Theme.Surface, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8.8f), PlaceholderText = "поиск по названию или описанию..."
            };
            _search.TextChanged += (_, __) => ApplyFilter();

            _refreshBtn = new FlatButton
            {
                Text = "Обновить состояние", IconKind = "bolt", Style = FlatButtonStyle.Soft,
                AccentColor = Theme.Accent, Size = new Size(186, 30), Location = new Point(260, 2)
            };
            _refreshBtn.Click += (_, __) => RefreshStates();

            _onAllBtn = new FlatButton
            {
                Text = "Включить показанные", Style = FlatButtonStyle.Soft,
                AccentColor = Theme.Success, Size = new Size(180, 30)
            };
            _onAllBtn.Click += (_, __) => ApplyVisible();

            _offAllBtn = new FlatButton
            {
                Text = "Откатить показанные", Style = FlatButtonStyle.Outline,
                AccentColor = Theme.Warning, Size = new Size(180, 30)
            };
            _offAllBtn.Click += (_, __) => RevertVisible();

            _counter = new Label
            {
                Dock = DockStyle.Right, Width = 190, ForeColor = Theme.Muted, AutoSize = false,
                Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleRight, Text = "загрузка…"
            };

            topLine.Controls.Add(_search);
            topLine.Controls.Add(_refreshBtn);
            topLine.Controls.Add(_onAllBtn);
            topLine.Controls.Add(_offAllBtn);
            topLine.Controls.Add(_counter);
            topLine.Resize += (_, __) =>
            {
                _offAllBtn.Location = new Point(topLine.Width - _offAllBtn.Width - _counter.Width, 2);
                _onAllBtn.Location  = new Point(_offAllBtn.Left - _onAllBtn.Width - 8, 2);
            };

            _chips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, WrapContents = true, FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0), Margin = new Padding(0)
            };
            foreach (var grp in Groups)
            {
                var chip = new FlatButton
                {
                    Text = grp, Style = FlatButtonStyle.Ghost, Size = new Size(ChipWidth(grp), 26),
                    Margin = new Padding(0, 0, 6, 4), AccentColor = Theme.Accent
                };
                string captured = grp;
                chip.Click += (_, __) => { _activeGroup = captured; ApplyFilter(); };
                chip.Tag = grp;
                _chips.Controls.Add(chip);
            }

            toolbar.Controls.Add(_chips);
            toolbar.Controls.Add(topLine);
            root.Controls.Add(toolbar, 0, 0);

            // ── list ─────────────────────────────────────────────────────
            var host = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Все изменения",
                Subtitle = "каждый пункт включается и откатывается отдельно", ShowAccent = true,
                AccentColor = Theme.Accent, Margin = new Padding(0, 0, 0, 8)
            };
            _list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 2)
            };
            _list.Resize += (_, __) => ResizeRows();
            host.Controls.Add(_list);
            root.Controls.Add(host, 0, 1);

            // ── log ──────────────────────────────────────────────────────
            var logCard = new CardPanel { Dock = DockStyle.Fill, Title = "Журнал", AccentColor = Theme.SoftColor };
            _log = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Theme.LogBg, ForeColor = Theme.LogFg,
                Font = new Font("Consolas", 8.6f), WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            logCard.Controls.Add(_log);
            root.Controls.Add(logCard, 0, 2);
        }

        /// <summary>Measured, not estimated — otherwise Cyrillic labels wrap mid-word.</summary>
        private static int ChipWidth(string s)
        {
            using var f = new Font("Segoe UI", 9f, FontStyle.Bold);
            return Math.Max(56, TextRenderer.MeasureText(s, f).Width + 30);
        }

        private void ResizeRows()
        {
            int w = Math.Max(320, _list.ClientSize.Width - 26);
            foreach (var r in _rows) r.Width = w;
        }

        // ── loading ─────────────────────────────────────────────────────
        private void LoadAll()
        {
            RunAsync("Загружаю список...", () =>
            {
                Log("── чтение состояния системы ──");
                TweakCatalogSystem.Refresh(s => Log(s));

                var items = TweakCatalog.Build();
                items.AddRange(TweakCatalogSystem.BuildServices());
                items.AddRange(TweakCatalogSystem.BuildTasks());

                string byGroup = string.Join(", ", items.GroupBy(i => i.Group)
                                                          .Select(g => $"{g.Key} {g.Count()}"));
                Log($"── собрано пунктов: {items.Count} ({byGroup}) ──");

                Invoke(new Action(() => BuildRows(items)));
                DetectAll();   // same background operation — a nested RunAsync would be refused
            });
        }

        private void BuildRows(List<TweakItem> items)
        {
            foreach (var r in _rows) { _list.Controls.Remove(r); r.Dispose(); }
            _rows.Clear();
            _list.Controls.Clear();

            int width = Math.Max(320, _list.ClientSize.Width - 26);
            foreach (var item in items)
            {
                var row = new TweakRow(item) { Width = width };
                row.ApplyClicked  += OnApply;
                row.RevertClicked += OnRevert;
                _rows.Add(row);
            }
            _list.Controls.AddRange(_rows.ToArray());
            _list.PerformLayout();
            ResizeRows();
            ApplyFilter();

            // paint the previous run's states immediately, then refresh in the background
            var cached = StateCache.Load(TimeSpan.FromMinutes(15));
            if (cached.Count > 0)
            {
                int known = 0;
                foreach (var row in _rows)
                    if (cached.TryGetValue(row.Item.Id, out var st)) { row.SetState(st); known++; }
                if (known > 0) Log($"── из кэша подставлено состояний: {known} — сейчас обновлю ──");
            }

            UpdateCounter();
            DarkMode.ApplyRecursive(this);
            Log($"── строк в списке: {_rows.Count} ──");
        }

        private void RefreshStates()
        {
            if (_rows.Count == 0) { Log("Список ещё не построен."); return; }
            RunAsync("Читаю текущее состояние...", DetectAll);
        }

        /// <summary>
        /// Reads the live state of every row. Runs on a background thread.
        /// A handful run in parallel because many detections spawn a process —
        /// done strictly serially, 66 items took over a minute.
        /// </summary>
        private void DetectAll()
        {
            var snapshot = _rows.ToList();
            int done = 0, applied = 0, unsupported = 0;

            Parallel.ForEach(snapshot,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                row =>
                {
                    TweakState st;
                    try { st = row.Item.DetectSafe(); }
                    catch { st = new TweakState { Status = TweakStatus.Unknown }; }

                    if (st.Status == TweakStatus.Applied) Interlocked.Increment(ref applied);
                    if (st.Status == TweakStatus.Unsupported) Interlocked.Increment(ref unsupported);

                    int n = Interlocked.Increment(ref done);
                    try { Invoke(new Action(() => row.SetState(st))); } catch { }
                    if (n % 15 == 0) Log($"  проверено {n} / {snapshot.Count}");
                });

            Log($"── состояние обновлено: включено {applied}, недоступно {unsupported}, всего {done} ──");

            try
            {
                StateCache.Save(_rows.Select(r => new KeyValuePair<string, TweakState>(r.Item.Id, r.State)));
            }
            catch { }

            try { Invoke(new Action(() => { UpdateCounter(); DarkMode.ApplyRecursive(this); })); } catch { }
        }

        // ── per-row actions ─────────────────────────────────────────────
        private void OnApply(TweakRow row)
        {
            if (!ConfirmRisky(row.Item)) return;
            string title = row.Item.Title;

            RunAsync($"Включаю «{title}»...", () =>
            {
                try { Invoke(new Action(() => row.SetBusy("…"))); } catch { }
                Log($"▶ {title}");
                var (ok, _) = TweakRunner.ApplyOne(row.Item, s => Log(s));

                var st = row.Item.DetectSafe();
                try { Invoke(new Action(() => { row.SetState(st); UpdateCounter(); })); } catch { }

                Log(ok ? $"✓ {title}" : $"✗ {title} — не применилось");
                Toast.Show(FindForm(), ok ? $"{title}: включено" : $"{title}: ошибка",
                           ok ? Theme.Success : Theme.Danger, 3200);
            });
        }

        private void OnRevert(TweakRow row)
        {
            string title = row.Item.Title;
            RunAsync($"Откатываю «{title}»...", () =>
            {
                try { Invoke(new Action(() => row.SetBusy("…"))); } catch { }
                Log($"◀ откат: {title}");
                bool ok = TweakRunner.RevertOne(row.Item, s => Log(s));

                var st = row.Item.DetectSafe();
                try { Invoke(new Action(() => { row.SetState(st); UpdateCounter(); })); } catch { }
                Toast.Show(FindForm(), ok ? $"{title}: возвращено как было" : $"{title}: нет бэкапа",
                           ok ? Theme.Accent : Theme.Warning, 3200);
            });
        }

        private bool ConfirmRisky(TweakItem item)
        {
            if (item.Risk != TweakRisk.Risky) return true;
            string extra = item.Id == "net-reset-stack"
                ? "\n\nСброс стека требует перезагрузки и откату не подлежит."
                : "\n\nЭто агрессивное изменение: возможен нагрев и шум, откат — кнопкой «Откатить».";
            return MessageBox.Show($"{item.Title}\n\n{item.Desc}{extra}\n\nПродолжить?",
                       "GAMEOPTIMIZ 1.0 — рискованный твик",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        // ── bulk actions ────────────────────────────────────────────────
        private List<TweakRow> VisibleRows() => _rows.Where(r => r.Visible).ToList();

        private void ApplyVisible()
        {
            var targets = VisibleRows()
                .Where(r => r.State.Status != TweakStatus.Applied && r.State.Status != TweakStatus.Unsupported)
                .ToList();
            if (targets.Count == 0) { Log("Все показанные пункты уже включены."); return; }

            if (MessageBox.Show($"Включить {targets.Count} пункт(ов) из текущего отбора?\n\n" +
                                string.Join("\n", targets.Take(12).Select(t => "• " + t.Item.Title)) +
                                (targets.Count > 12 ? $"\n… и ещё {targets.Count - 12}" : ""),
                    "GAMEOPTIMIZ 1.0 — массовое включение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            RunAsync($"Включаю {targets.Count} пункт(ов)...", () =>
            {
                int ok = 0, bad = 0;
                foreach (var row in targets)
                {
                    Log($"▶ {row.Item.Title}");
                    var (success, _) = TweakRunner.ApplyOne(row.Item, s => Log(s));
                    if (success) ok++; else bad++;
                    var st = row.Item.DetectSafe();
                    try { Invoke(new Action(() => row.SetState(st))); } catch { }
                }
                Log($"── массовое включение: успешно {ok}, с ошибками {bad} ──");
                try { Invoke(new Action(UpdateCounter)); } catch { }
                Toast.Show(FindForm(), $"Включено: {ok}, ошибок: {bad}",
                           bad == 0 ? Theme.Success : Theme.Warning, 4200);
            });
        }

        private void RevertVisible()
        {
            var targets = VisibleRows().Where(r => r.State.RevertAvailable).ToList();
            if (targets.Count == 0) { Log("Для показанных пунктов нет сохранённых бэкапов."); return; }

            if (MessageBox.Show($"Откатить {targets.Count} пункт(ов) до состояния «как было»?",
                    "GAMEOPTIMIZ 1.0 — массовый откат", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            RunAsync($"Откатываю {targets.Count} пункт(ов)...", () =>
            {
                int ok = 0;
                foreach (var row in targets)
                {
                    Log($"◀ {row.Item.Title}");
                    if (TweakRunner.RevertOne(row.Item, s => Log(s))) ok++;
                    var st = row.Item.DetectSafe();
                    try { Invoke(new Action(() => row.SetState(st))); } catch { }
                }
                Log($"── возвращено пунктов: {ok} ──");
                try { Invoke(new Action(UpdateCounter)); } catch { }
                Toast.Show(FindForm(), $"Откачено: {ok}", Theme.Accent, 4200);
            });
        }

        // ── filtering ───────────────────────────────────────────────────
        private void ApplyFilter()
        {
            string q = (_search?.Text ?? "").Trim().ToLowerInvariant();

            foreach (var chip in _chips.Controls.OfType<FlatButton>())
            {
                bool active = (chip.Tag as string) == _activeGroup;
                chip.Style = active ? FlatButtonStyle.Soft : FlatButtonStyle.Ghost;
                chip.Invalidate();
            }

            int shown = 0;
            foreach (var row in _rows)
            {
                bool groupOk = _activeGroup == "Все" || row.Item.Group == _activeGroup;
                bool searchOk = q.Length == 0
                    || row.Item.Title.ToLowerInvariant().Contains(q)
                    || row.Item.Desc.ToLowerInvariant().Contains(q)
                    || row.Item.Group.ToLowerInvariant().Contains(q)
                    || (row.Item.Note ?? "").ToLowerInvariant().Contains(q);
                row.Visible = groupOk && searchOk;
                if (row.Visible) shown++;
            }
            _list.PerformLayout();
            UpdateCounter(shown);
        }

        private void UpdateCounter() => UpdateCounter(_rows.Count(r => r.Visible));

        private void UpdateCounter(int shown)
        {
            if (shown < 0) shown = _rows.Count(r => r.Visible);
            int on = _rows.Count(r => r.State.Status == TweakStatus.Applied);
            _counter.Text = $"показано {shown} из {_rows.Count}  •  включено {on}";
        }

        // ── helpers ─────────────────────────────────────────────────────
        private void RunAsync(string title, Action work)
        {
            if (_busy) { Log("Дождись завершения текущей операции."); return; }
            _busy = true;
            _counter.Text = title;
            SetEnabled(false);

            Task.Run(() =>
            {
                try { work(); }
                catch (Exception ex) { Log($"[ОШИБКА] {ex.Message}"); }
                finally
                {
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            _busy = false;
                            SetEnabled(true);
                            UpdateCounter();
                        }));
                    }
                    catch { }
                }
            });
        }

        private void SetEnabled(bool on)
        {
            _refreshBtn.Enabled = on;
            _onAllBtn.Enabled = on;
            _offAllBtn.Enabled = on;
        }

        private void Log(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (IsHandleCreated && InvokeRequired)
            {
                try { BeginInvoke(new Action(() => Log(line))); } catch { }
                return;
            }
            try
            {
                _log.SelectionStart = _log.TextLength;
                _log.SelectionColor = line.StartsWith("▶") ? Theme.Accent
                                    : line.StartsWith("◀") ? Theme.Warning
                                    : line.StartsWith("✓") ? Theme.Success
                                    : line.StartsWith("✗") ? Theme.Danger
                                    : line.StartsWith("──") ? Theme.AccentAlt
                                    : Theme.LogFg;
                _log.AppendText(line + Environment.NewLine);
                _log.ScrollToCaret();
            }
            catch { }
        }
    }
}
