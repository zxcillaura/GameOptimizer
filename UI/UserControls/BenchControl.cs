using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GameOptimizer.Core.Bench;
using GameOptimizer.Core.Themes;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// Import a CapFrameX / PresentMon capture and get the numbers that matter:
    /// average FPS, 1% low and 0.1% low — with history to prove a change worked.
    /// </summary>
    public class BenchControl : UserControl
    {
        private TextBox _path;
        private ComboBox _game;
        private TextBox _label;
        private Label _avg, _low1, _low01, _stut, _delta;
        private LiveChart _chart;
        private LiveChart.Series _ftSeries;
        private ListView _history;
        private RichTextBox _log;

        public BenchControl() => Build();

        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
            Controls.Add(root);

            // ── import + stats ───────────────────────────────────────────
            var importCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Импорт замера",
                Subtitle = "CSV от CapFrameX или PresentMon", AccentColor = Theme.GoldColor, ShowAccent = true
            };

            var row1 = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            _path = MakeInput("путь к CSV...", 320);
            var browse = new FlatButton
            {
                Text = "Обзор", IconKind = "sliders", Style = FlatButtonStyle.Outline,
                AccentColor = Theme.GoldColor, Size = new Size(96, 28), Location = new Point(326, 3)
            };
            var auto = new FlatButton
            {
                Text = "Найти самому", Style = FlatButtonStyle.Ghost,
                Size = new Size(120, 28), Location = new Point(428, 3)
            };
            _game = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(554, 3),
                Width = 92, BackColor = Theme.Surface, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f)
            };
            _game.Items.AddRange(new object[] { "CS2", "Dota 2", "Другое" });
            _game.SelectedIndex = 0;
            _label = MakeInput("метка (до/после)", 140);
            _label.Location = new Point(652, 3);

            var import = new FlatButton
            {
                Text = "Импортировать", IconKind = "chart", Style = FlatButtonStyle.Filled,
                AccentColor = Theme.GoldColor, Size = new Size(150, 28), Location = new Point(800, 3)
            };
            // keep the primary action glued to the right edge on any window width
            row1.Resize += (_, __) => import.Left = Math.Max(800, row1.Width - import.Width - 2);

            browse.Click += (_, __) => PickFile();
            auto.Click   += (_, __) => AutoFind();
            import.Click += (_, __) => DoImport();

            row1.Controls.Add(_path);
            row1.Controls.Add(browse);
            row1.Controls.Add(auto);
            row1.Controls.Add(_game);
            row1.Controls.Add(_label);
            row1.Controls.Add(import);

            var stats = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _avg   = MakeStat("Средний FPS", "—", Theme.Accent, 0);
            _low1  = MakeStat("1% low", "—", Theme.Success, 160);
            _low01 = MakeStat("0.1% low", "—", Theme.AccentAlt, 320);
            _stut  = MakeStat("Микрофризы", "—", Theme.Warning, 480);
            _delta = MakeStat("К прошлому", "—", Theme.GoldColor, 640);
            foreach (var s in new[] { _avg, _low1, _low01, _stut, _delta }) stats.Controls.Add(s);

            importCard.Controls.Add(stats);
            importCard.Controls.Add(row1);
            root.Controls.Add(importCard, 0, 0);

            // ── frametime chart ──────────────────────────────────────────
            var chartCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "График frametime",
                Subtitle = "чем ровнее — тем плавнее", FillColor = Theme.Card
            };
            _chart = new LiveChart
            {
                Dock = DockStyle.Fill, Title = "", Max = 40, Unit = " мс",
                Capacity = 240, AutoScale = true, BackColor = Theme.Card
            };
            _ftSeries = _chart.AddSeries("frametime", Theme.GoldColor, true);
            chartCard.Controls.Add(_chart);
            root.Controls.Add(chartCard, 0, 1);

            // ── history + log ────────────────────────────────────────────
            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

            var histCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "История замеров",
                Subtitle = "двойной клик — удалить", Margin = new Padding(0, 0, 8, 0)
            };
            _history = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Theme.Card, ForeColor = Theme.TextSecondary,
                BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8.5f),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _history.Columns.Add("Дата", 118);
            _history.Columns.Add("Игра", 62);
            _history.Columns.Add("Метка", 130);
            _history.Columns.Add("avg", 56);
            _history.Columns.Add("1% low", 62);
            _history.Columns.Add("0.1%", 56);
            _history.DoubleClick += (_, __) => DeleteSelected();
            histCard.Controls.Add(_history);

            var logCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Журнал", Margin = new Padding(8, 0, 0, 0)
            };
            _log = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Theme.LogBg, ForeColor = Theme.LogFg,
                Font = new Font("Consolas", 8.6f)
            };
            logCard.Controls.Add(_log);

            bottom.Controls.Add(histCard, 0, 0);
            bottom.Controls.Add(logCard, 1, 0);
            root.Controls.Add(bottom, 0, 2);

            RefreshHistory();
        }

        private static TextBox MakeInput(string placeholder, int width) => new()
        {
            Text = "", Width = width, Height = 28, Location = new Point(0, 3),
            BackColor = Theme.Surface, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.8f), PlaceholderText = placeholder
        };

        private static Label MakeStat(string caption, string value, Color accent, int left) => new()
        {
            Text = $"{caption}\r\n{value}", Left = left, Top = 2, Width = 156, Height = 66,
            ForeColor = accent, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            BackColor = Color.Transparent, Tag = caption,
            AutoSize = false, TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        private static void SetStat(Label l, string value, Color? color = null)
        {
            l.Text = $"{l.Tag}\r\n{value}";
            if (color.HasValue) l.ForeColor = color.Value;
        }

        private void PickFile()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Замеры (*.csv;*.txt)|*.csv;*.txt|Все файлы (*.*)|*.*",
                Title = "Выбери CSV с замером"
            };
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) _path.Text = dlg.FileName;
        }

        private void AutoFind()
        {
            var found = BenchmarkStore.FindLikelyCaptures();
            if (found.Count == 0) { Log("Автопоиск: CSV не найдено в Документах, Загрузках и на Рабочем столе."); return; }
            _path.Text = found[0];
            Log($"Автопоиск: найден свежий файл {Path.GetFileName(found[0])}");
            if (found.Count > 1) Log($"  (всего найдено файлов: {found.Count}, можно выбрать любой вручную)");
        }

        private void DoImport()
        {
            string path = _path.Text?.Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log("Укажи существующий CSV-файл.");
                return;
            }

            var run = BenchmarkStore.ImportCsv(path, _game.SelectedItem?.ToString() ?? "CS2",
                                               _label.Text?.Trim(), out string error);
            if (run == null) { Log($"Не удалось импортировать: {error}"); return; }

            var previous = BenchmarkStore.History
                .FirstOrDefault(h => h.Game == run.Game && h.Id != run.Id);

            SetStat(_avg,   $"{run.AvgFps:F0} FPS");
            SetStat(_low1,  $"{run.OnePercentLow:F0} FPS");
            SetStat(_low01, $"{run.PointOnePercentLow:F0} FPS");
            SetStat(_stut,  $"{run.Stutters}  за {run.DurationSec:F0} с");
            SetStat(_avg,   $"{run.AvgFps:F0} FPS", Theme.Accent);
            SetStat(_low1,  $"{run.OnePercentLow:F0} FPS", Theme.Success);
            SetStat(_low01, $"{run.PointOnePercentLow:F0} FPS", Theme.AccentAlt);

            if (previous != null)
            {
                double d = run.OnePercentLow - previous.OnePercentLow;
                SetStat(_delta, $"{(d >= 0 ? "+" : "")}{d:F0} FPS  1% low",
                        d >= 0 ? Theme.Success : Theme.Danger);
                Log($"Сравнение с «{previous.Label}» ({previous.Date}): " +
                    $"avg {previous.AvgFps:F0} → {run.AvgFps:F0}, " +
                    $"1% low {previous.OnePercentLow:F0} → {run.OnePercentLow:F0}");
            }
            else
            {
                SetStat(_delta, "нет базы", Theme.Muted);
                Log("Это первый замер для этой игры — дальше будет с чем сравнивать.");
            }

            _ftSeries.Values.Clear();
            _chart.Capacity = Math.Max(10, BenchmarkStore.LastFrametimes.Count);
            _ftSeries.Values.AddRange(BenchmarkStore.LastFrametimes);
            _chart.Invalidate();

            BenchmarkStore.Add(run);
            RefreshHistory();

            Log($"Импортирован: {Path.GetFileName(path)}");
            Log($"  {run.Frames} кадров за {run.DurationSec:F1} с, " +
                $"средний frametime {run.AvgFrameMs:F2} мс, медиана {run.MedianFrameMs:F2} мс");
            Log("  Напоминание: последний замер должен идти той же сценой, что и предыдущий, " +
                "иначе цифры несравнимы.");
        }

        private void RefreshHistory()
        {
            _history.BeginUpdate();
            _history.Items.Clear();
            foreach (var h in BenchmarkStore.History)
            {
                var it = new ListViewItem(new[]
                {
                    h.Date, h.Game, h.Label,
                    $"{h.AvgFps:F0}", $"{h.OnePercentLow:F0}", $"{h.PointOnePercentLow:F0}"
                }) { Tag = h.Id };
                _history.Items.Add(it);
            }
            _history.EndUpdate();
        }

        private void DeleteSelected()
        {
            if (_history.SelectedItems.Count == 0) return;
            string id = _history.SelectedItems[0].Tag as string;
            BenchmarkStore.Remove(id);
            RefreshHistory();
            Log("Замер удалён из истории.");
        }

        private void Log(string line)
        {
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = Theme.LogFg;
            _log.AppendText(line + Environment.NewLine);
            _log.ScrollToCaret();
        }
    }
}
