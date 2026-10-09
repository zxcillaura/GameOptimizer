using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameOptimizer.Core.Diagnostics;
using GameOptimizer.Core.Models;
using GameOptimizer.Core.Themes;
using GameOptimizer.Utils;

namespace GameOptimizer.UI.UserControls
{
    public class FaceitControl : UserControl
    {
        private Label           _titleLabel;
        private Label           _subtitleLabel;
        private Panel           _statusCard;
        private Panel           _acStatusDot;
        private Label           _acStatusLabel;
        private Panel           _checkCard;
        private FlowLayoutPanel _checksFlow;
        private Button          _scanButton;
        private Label           _loaderLabel;
        private Panel           _loaderPanel;

        private readonly ACDiagnostic _diagnostic = new();

        public FaceitControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.Background;

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
            Controls.Add(scroll);

            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Theme.Background };
            scroll.Controls.Add(wrapper);

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Theme.Background };
            _titleLabel    = new Label { Text = "FACEIT Anti-Cheat", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Theme.Warning, AutoSize = true, Location = new Point(32, 20) };
            _subtitleLabel = new Label { Text = "Диагностика, проверка драйверов, совместимость", Font = new Font("Segoe UI", 10), ForeColor = Theme.Muted, AutoSize = true, Location = new Point(32, 52) };
            header.Controls.Add(_titleLabel);
            header.Controls.Add(_subtitleLabel);

            // Content
            var content = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Theme.Background, Padding = new Padding(32, 8, 32, 24) };
            var layout  = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Theme.Background, Padding = new Padding(0), Margin = new Padding(0) };

            // Status card
            _statusCard = CreateCard(740, 80);
            var stTitle = new Label { Text = "Статус FACEIT AC", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(20, 16) };
            _acStatusDot   = new Panel { Size = new Size(14, 14), Location = new Point(20, 50), BackColor = Theme.Muted };
            _acStatusDot.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.FillEllipse(new SolidBrush(_acStatusDot.BackColor), 0, 0, 13, 13); };
            _acStatusLabel = new Label { Text = "Нажмите СКАН для проверки", Font = new Font("Segoe UI", 10), ForeColor = Theme.Muted, AutoSize = true, Location = new Point(42, 47) };
            _statusCard.Controls.AddRange(new Control[] { stTitle, _acStatusDot, _acStatusLabel });

            // Loader
            _loaderPanel = new Panel { Size = new Size(740, 36), BackColor = Theme.Background, Visible = false };
            _loaderLabel = new Label { Text = "Сканирование системы...", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = Theme.Warning, AutoSize = true, Location = new Point(0, 10) };
            _loaderPanel.Controls.Add(_loaderLabel);

            // Checks card
            _checkCard = CreateCard(740, 340);
            var cTitle = new Label { Text = "Результаты проверки", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(20, 16) };
            _checksFlow = new FlowLayoutPanel { Location = new Point(12, 48), Size = new Size(716, 278), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Card };
            _checksFlow.Controls.Add(new Label { Text = "Запустите скан для просмотра результатов...", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9), AutoSize = true, Padding = new Padding(4, 4, 0, 0) });
            _checkCard.Controls.Add(cTitle);
            _checkCard.Controls.Add(_checksFlow);

            // Button row
            var btnRow = new FlowLayoutPanel { AutoSize = true, BackColor = Theme.Background, Margin = new Padding(0, 8, 0, 0) };

            _scanButton = new Button { Text = "СКАН", Size = new Size(200, 46), FlatStyle = FlatStyle.Flat, BackColor = Theme.Warning, ForeColor = Color.Black, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 12, 0) };
            _scanButton.FlatAppearance.BorderSize = 0;
            _scanButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 180, 90);
            _scanButton.Click += async (_, _) => await RunScanAsync();

            var fixButton = new Button { Text = "ИСПРАВИТЬ ОДНИМ КЛИКОМ", Size = new Size(260, 46), FlatStyle = FlatStyle.Flat, BackColor = Theme.SoftColor, ForeColor = Color.FromArgb(6, 14, 12), Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            fixButton.FlatAppearance.BorderSize = 0;
            fixButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 200, 90);
            fixButton.Click += (_, _) =>
            {
                MessageBox.Show(
                    "Автоматическое отключение VBS, HVCI и Hyper-V не выполняется: это снижает защиту Windows и может конфликтовать с Vanguard. Используйте инструкции конкретного результата сканирования и меняйте только нужный параметр вручную.",
                    "Безопасное исправление FACEIT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnRow.Controls.Add(_scanButton);
            btnRow.Controls.Add(fixButton);

            layout.Controls.Add(Wrap(_statusCard, 0, 0, 0, 16));
            layout.Controls.Add(Wrap(_loaderPanel, 0, 0, 0, 4));
            layout.Controls.Add(Wrap(_checkCard, 0, 0, 0, 16));
            layout.Controls.Add(btnRow);

            content.Controls.Add(layout);
            wrapper.Controls.Add(content);
            wrapper.Controls.Add(header);
        }

        private async Task RunScanAsync()
        {
            _scanButton.Enabled = false;
            _loaderPanel.Visible = true;
            _checksFlow.Controls.Clear();
            _acStatusDot.BackColor = Theme.Muted;
            _acStatusLabel.Text    = "Сканирование...";
            _acStatusLabel.ForeColor = Theme.Muted;

            System.Collections.Generic.IReadOnlyList<CheckResult> results;
            try
            {
                results = await Task.Run(() => _diagnostic.RunFaceitDiagnostics());
            }
            catch (Exception ex)
            {
                _loaderPanel.Visible = false;
                _scanButton.Enabled = true;
                _acStatusDot.BackColor = Theme.Danger;
                _acStatusLabel.Text = "Диагностика не завершилась";
                _acStatusLabel.ForeColor = Theme.Danger;
                _checksFlow.Controls.Add(new Label { Text = "Ошибка: " + ex.Message, ForeColor = Theme.Danger, AutoSize = true, Padding = new Padding(4) });
                return;
            }

            _loaderPanel.Visible = false;
            _scanButton.Enabled  = true;

            foreach (var r in results)
            {
                bool ok = r.Status == CheckStatus.Ok;
                bool warn = r.Status == CheckStatus.Warning;

                var row = new Panel { Size = new Size(710, 52), BackColor = Theme.Card, Margin = new Padding(0, 0, 0, 2) };
                Color dotColor = ok ? Theme.Success : warn ? Theme.Warning : Theme.Danger;

                var dot = new Panel { Size = new Size(10, 10), Location = new Point(10, 21), BackColor = dotColor };
                dot.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.FillEllipse(new SolidBrush(dot.BackColor), 0, 0, 9, 9); };

                var name = new Label { Text = r.Message, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(28, 8) };
                var instr = new Label { Text = r.Instruction, Font = new Font("Segoe UI", 7.5f), ForeColor = ok ? Theme.Muted : dotColor, AutoSize = false, Size = new Size(680, 32), Location = new Point(28, 27), AutoEllipsis = true };
                row.Controls.AddRange(new Control[] { dot, name, instr });
                row.Paint += (_, e) => { using var p = new Pen(Theme.Border, 1); e.Graphics.DrawLine(p, 0, row.Height - 1, row.Width, row.Height - 1); };
                _checksFlow.Controls.Add(row);
            }

            bool anyError = false;
            foreach (var r in results) if (r.Status == CheckStatus.Error) { anyError = true; break; }

            _acStatusDot.BackColor   = anyError ? Theme.Danger : Theme.Success;
            _acStatusLabel.Text      = anyError ? "Найдены проблемы — см. детали ниже" : "FACEIT AC: Все проверки пройдены";
            _acStatusLabel.ForeColor = anyError ? Theme.Danger : Theme.Success;
        }

        private static Panel CreateCard(int width, int height)
        {
            var card = new Panel { Size = new Size(width, height), BackColor = Theme.Card };
            card.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; using var pen = new Pen(Theme.Border, 1); e.Graphics.DrawRoundedRectangle(pen, 0, 0, card.Width - 1, card.Height - 1, 10); };
            return card;
        }

        private static Panel Wrap(Control ctrl, int l, int t, int r, int b)
        {
            var p = new Panel { Size = new Size(ctrl.Width + l + r, ctrl.Height + t + b), BackColor = Theme.Background };
            ctrl.Location = new Point(l, t);
            p.Controls.Add(ctrl);
            return p;
        }
    }
}
