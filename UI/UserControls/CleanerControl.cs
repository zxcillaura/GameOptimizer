using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core.Cleanup;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// 1.0 — глубокий, но безопасный очиститель. Каждая цель подписана простым
    /// языком; сначала предпросмотр («освободится ~X ГБ»), потом удаление.
    /// Опасные цели (WinSxS, Windows.old) требуют отдельного подтверждения.
    /// </summary>
    public class CleanerControl : UserControl
    {
        private Label           _titleLabel, _statusLabel;
        private FlowLayoutPanel _flow;
        private Button          _scanBtn, _cleanBtn;
        private RichTextBox     _logBox;

        private readonly List<CleanTarget> _targets = DeepCleaner.Targets();
        private readonly Dictionary<string, (CheckBox cb, Label size)> _rows = new();

        public CleanerControl()
        {
            InitializeComponents();
            ApplyTheme();
        }

        private void InitializeComponents()
        {
            Dock    = DockStyle.Fill;
            Padding = new Padding(24, 16, 24, 16);
            BackColor = Theme.Background;

            var header = new Panel { Dock = DockStyle.Top, Height = 60 };
            _titleLabel = new Label
            {
                Text = "Очиститель системы", Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(0, 2), AutoSize = true
            };
            _statusLabel = new Label
            {
                Text = "Нажми «Оценить» — посчитаю, сколько места освободится. Удаление — только по кнопке «Очистить».",
                Font = new Font("Segoe UI", 9), Location = new Point(0, 34), AutoSize = true
            };
            header.Controls.Add(_titleLabel);
            header.Controls.Add(_statusLabel);

            var actions = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(0, 8, 0, 8) };
            _scanBtn  = MakeBtn("Оценить",  false, ScanBtn_Click);
            _cleanBtn = MakeBtn("Очистить", true,  CleanBtn_Click);
            _cleanBtn.Enabled = false;
            _scanBtn.Location  = new Point(0,   8);
            _cleanBtn.Location = new Point(145, 8);
            actions.Controls.Add(_scanBtn);
            actions.Controls.Add(_cleanBtn);

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 268, AutoScroll = true,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(0, 6, 0, 6)
            };
            foreach (var t in _targets) _flow.Controls.Add(BuildRow(t));

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = "[Очиститель 1.0] Готов. Сначала «Оценить».\r\n"
            };

            Controls.Add(_logBox);
            Controls.Add(_flow);
            Controls.Add(actions);
            Controls.Add(header);
        }

        private Panel BuildRow(CleanTarget t)
        {
            var row = new Panel { Width = 820, Height = 48, BackColor = Theme.Card, Margin = new Padding(0, 0, 0, 6) };
            var cb = new CheckBox
            {
                Text = t.Title, Checked = t.DefaultOn, Tag = t.Key,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = t.NeedsConfirm ? Theme.Warning : Theme.Text, BackColor = Theme.Card,
                FlatStyle = FlatStyle.Flat, AutoSize = false,
                Location = new Point(10, 5), Size = new Size(560, 20)
            };
            cb.FlatAppearance.BorderColor = Theme.Border;
            var desc = new Label
            {
                Text = t.Desc, Font = new Font("Segoe UI", 7.8f),
                ForeColor = Theme.Muted, BackColor = Theme.Card,
                Location = new Point(12, 25), Size = new Size(640, 20), AutoEllipsis = true
            };
            var size = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Accent, BackColor = Theme.Card,
                Location = new Point(690, 14), Size = new Size(120, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            row.Controls.Add(cb);
            row.Controls.Add(desc);
            row.Controls.Add(size);
            _rows[t.Key] = (cb, size);
            return row;
        }

        // ── Оценить (предпросмотр размеров) ───────────────────────────────
        private async void ScanBtn_Click(object? sender, EventArgs e)
        {
            _scanBtn.Enabled = false; _cleanBtn.Enabled = false;
            _statusLabel.Text = "Считаю размеры...";
            AppendLog("Оценка размеров мусора...");

            long total = 0;
            await Task.Run(() =>
            {
                total = DeepCleaner.Preview(_targets, msg => Invoke((Action)(() => AppendLog(msg))));
            });

            foreach (var t in _targets)
                if (_rows.TryGetValue(t.Key, out var r))
                    r.size.Text = t.Measure != null && t.LastSizeBytes >= 0
                        ? (t.LastSizeBytes > 0 ? DeepCleaner.Human(t.LastSizeBytes) : "—")
                        : "действие";

            _statusLabel.Text = $"Освободится примерно: ~{DeepCleaner.Human(total)}. Проверь галочки и нажми «Очистить».";
            AppendLog($"[OK] Итого к освобождению: ~{DeepCleaner.Human(total)}.");
            _scanBtn.Enabled = true; _cleanBtn.Enabled = true;
        }

        // ── Очистить ──────────────────────────────────────────────────────
        private async void CleanBtn_Click(object? sender, EventArgs e)
        {
            var chosen = _targets.Where(t => _rows[t.Key].cb.Checked).ToList();
            if (chosen.Count == 0) { AppendLog("Ничего не выбрано."); return; }

            var risky = chosen.Where(t => t.NeedsConfirm).ToList();
            if (risky.Count > 0)
            {
                string names = string.Join("\n", risky.Select(t => "• " + t.Title));
                if (MessageBox.Show(
                        "Выбраны необратимые операции:\n\n" + names +
                        "\n\nПосле них откатить установленные обновления / вернуться к старой сборке Windows будет нельзя.\n\nПродолжить?",
                        "Очиститель — подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    != DialogResult.Yes)
                    return;
            }

            _cleanBtn.Enabled = false; _scanBtn.Enabled = false;
            _statusLabel.Text = "Очистка...";
            AppendLog($"Очистка {chosen.Count} целей...");

            DeepCleanResult res = null;
            await Task.Run(() =>
            {
                res = DeepCleaner.Clean(chosen, msg => Invoke((Action)(() => AppendLog(msg))));
            });

            foreach (var t in chosen)
                if (_rows.TryGetValue(t.Key, out var r)) r.size.Text = "✓";

            _statusLabel.Text = res != null
                ? $"Готово. Освобождено ~{DeepCleaner.Human(res.FreedBytes)} по {res.Targets} целям."
                : "Готово.";
            if (res?.Notes.Count > 0)
                foreach (var n in res.Notes) AppendLog("  ! " + n);
            AppendLog("[OK] Очистка завершена.");
            _scanBtn.Enabled = true; _cleanBtn.Enabled = true;
        }

        private void AppendLog(string msg)
        {
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            _logBox.ScrollToCaret();
        }

        private Button MakeBtn(string text, bool primary, EventHandler click)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(135, 30), FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.Click += click;
            if (primary) Theme.StyleButton(btn);
            else         Theme.StyleButton(btn, secondary: true);
            return btn;
        }

        private void ApplyTheme()
        {
            BackColor = Theme.Background; ForeColor = Theme.Text;
            _titleLabel.ForeColor  = Theme.Text;
            _statusLabel.ForeColor = Theme.Muted;
            _flow.BackColor        = Theme.Background;
            _logBox.BackColor      = Theme.Card;
            _logBox.ForeColor      = Color.FromArgb(100, 210, 130);
        }
    }
}
