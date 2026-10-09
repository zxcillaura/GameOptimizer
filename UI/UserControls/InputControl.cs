using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// Dedicated input page. It reuses the catalog's mk-* tweaks so applying
    /// a setting here is identical to applying it from the tweak centre.
    /// </summary>
    public sealed class InputControl : UserControl
    {
        private FlowLayoutPanel _list;
        private RichTextBox _log;
        private Label _latencyLabel;
        private FlatButton _testButton;
        private readonly List<TweakRow> _rows = new();
        private bool _busy;

        public InputControl()
        {
            Build();
            Load += (_, __) => LoadTweaks();
        }

        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            Controls.Add(root);

            var hero = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Мышь и клавиатура",
                Subtitle = "настройки ввода без лишних системных твиков",
                AccentColor = Theme.Accent, ShowAccent = true
            };
            _latencyLabel = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false,
                Text = "Инпут-лаг — это время от нажатия до реакции игры. Сначала применяй безопасные пункты, затем проверь игру своим бенчмарком.",
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 8.7f),
                Padding = new Padding(0, 4, 0, 0)
            };
            _testButton = new FlatButton
            {
                Text = "Тест очереди ввода", Style = FlatButtonStyle.Soft,
                AccentColor = Theme.Accent, Size = new Size(176, 30), Dock = DockStyle.Bottom
            };
            _testButton.Click += (_, __) => TestInputLatency();
            hero.Controls.Add(_latencyLabel);
            hero.Controls.Add(_testButton);
            root.Controls.Add(hero, 0, 0);

            var listCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Твики ввода",
                Subtitle = "каждый пункт можно включить и откатить отдельно",
                AccentColor = Theme.AccentAlt
            };
            _list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true,
                BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 2)
            };
            _list.Resize += (_, __) => ResizeRows();
            listCard.Controls.Add(_list);
            root.Controls.Add(listCard, 0, 1);

            var logCard = new CardPanel { Dock = DockStyle.Fill, Title = "Журнал", AccentColor = Theme.SoftColor };
            _log = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Theme.LogBg, ForeColor = Theme.LogFg,
                Font = new Font("Consolas", 8.6f), WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            logCard.Controls.Add(_log);
            root.Controls.Add(logCard, 0, 2);
        }

        private void LoadTweaks()
        {
            var items = TweakCatalog.Build()
                .Where(t => t.Group == "Мышь и клава")
                .ToList();

            foreach (var row in _rows) row.Dispose();
            _rows.Clear();
            _list.Controls.Clear();

            foreach (var item in items)
            {
                var row = new TweakRow(item) { Width = Math.Max(320, _list.ClientSize.Width - 26) };
                row.ApplyClicked += ApplyRow;
                row.RevertClicked += RevertRow;
                _rows.Add(row);
                _list.Controls.Add(row);
            }
            ResizeRows();
            Log($"Загружено настроек ввода: {_rows.Count}.");
            RefreshStates();
        }

        private void ResizeRows()
        {
            if (_list == null) return;
            int width = Math.Max(320, _list.ClientSize.Width - 26);
            foreach (var row in _rows) row.Width = width;
        }

        private void RefreshStates()
        {
            if (_rows.Count == 0) return;
            RunAsync("Читаю состояние твиков ввода...", () =>
            {
                foreach (var row in _rows)
                {
                    var state = row.Item.DetectSafe();
                    try { BeginInvoke(new Action(() => row.SetState(state))); } catch { }
                }
                Log("Состояние настроек ввода обновлено.");
            });
        }

        private void ApplyRow(TweakRow row)
        {
            RunAsync($"Включаю «{row.Item.Title}»...", () =>
            {
                try { BeginInvoke(new Action(() => row.SetBusy("…"))); } catch { }
                var (ok, _) = TweakRunner.ApplyOne(row.Item, Log);
                var state = row.Item.DetectSafe();
                try { BeginInvoke(new Action(() => row.SetState(state))); } catch { }
                Log(ok ? $"✓ {row.Item.Title}" : $"✗ {row.Item.Title} — не применилось");
            });
        }

        private void RevertRow(TweakRow row)
        {
            RunAsync($"Откатываю «{row.Item.Title}»...", () =>
            {
                try { BeginInvoke(new Action(() => row.SetBusy("…"))); } catch { }
                bool ok = TweakRunner.RevertOne(row.Item, Log);
                var state = row.Item.DetectSafe();
                try { BeginInvoke(new Action(() => row.SetState(state))); } catch { }
                Log(ok ? $"✓ {row.Item.Title}: возвращено как было" : $"! {row.Item.Title}: бэкап не найден");
            });
        }

        private void TestInputLatency()
        {
            if (_busy) { Log("Дождись завершения текущей операции."); return; }
            _testButton.Enabled = false;
            _latencyLabel.Text = "Измеряю задержку обработки событий интерфейса...";
            Task.Run(() =>
            {
                var samples = new List<double>();
                for (int i = 0; i < 20; i++)
                {
                    using var gate = new ManualResetEventSlim(false);
                    var sw = Stopwatch.StartNew();
                    try { BeginInvoke(new Action(() => gate.Set())); }
                    catch { break; }
                    if (!gate.Wait(1000)) break;
                    sw.Stop();
                    samples.Add(sw.Elapsed.TotalMilliseconds);
                }
                double median = samples.Count == 0 ? -1 : samples.OrderBy(x => x).ElementAt(samples.Count / 2);
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        _testButton.Enabled = true;
                        _latencyLabel.Text = median >= 0
                            ? $"Медиана обработки события: {median:0.##} мс. Это ориентир очереди Windows/программы, а не лабораторное измерение сенсора мыши."
                            : "Не удалось получить замер — окно занято или недоступно.";
                    }));
                }
                catch { }
            });
        }

        private void RunAsync(string title, Action work)
        {
            if (_busy) { Log("Дождись завершения текущей операции."); return; }
            _busy = true;
            _testButton.Enabled = false;
            Log(title);
            Task.Run(() =>
            {
                try { work(); }
                catch (Exception ex) { Log("[ОШИБКА] " + ex.Message); }
                finally
                {
                    try { BeginInvoke(new Action(() => { _busy = false; _testButton.Enabled = true; })); }
                    catch { }
                }
            });
        }

        private void Log(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (IsHandleCreated && InvokeRequired)
            {
                try { BeginInvoke(new Action(() => Log(text))); } catch { }
                return;
            }
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            _log.ScrollToCaret();
        }
    }
}
