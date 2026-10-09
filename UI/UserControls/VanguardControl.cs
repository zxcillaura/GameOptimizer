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
    public class VanguardControl : UserControl
    {
        private Label           _titleLabel;
        private Label           _subtitleLabel;
        private Panel           _statusCard;
        private Panel           _statusDot;
        private Label           _statusLabel;
        private Panel           _checkCard;
        private FlowLayoutPanel _checksFlow;
        private Button          _scanButton;
        private Panel           _loaderPanel;

        private readonly ACDiagnostic _diagnostic = new();

        public VanguardControl()
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
            _titleLabel    = new Label { Text = "Riot Vanguard", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Theme.Danger, AutoSize = true, Location = new Point(32, 20) };
            _subtitleLabel = new Label { Text = "TPM 2.0, Secure Boot, HVCI, Hyper-V — проверка совместимости", Font = new Font("Segoe UI", 10), ForeColor = Theme.Muted, AutoSize = true, Location = new Point(32, 52) };
            header.Controls.Add(_titleLabel);
            header.Controls.Add(_subtitleLabel);

            // Content
            var content = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Theme.Background, Padding = new Padding(32, 8, 32, 24) };
            var layout  = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Theme.Background };

            // Status card
            _statusCard  = CreateCard(740, 80);
            var stTitle  = new Label { Text = "Статус Vanguard", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(20, 16) };
            _statusDot   = new Panel { Size = new Size(14, 14), Location = new Point(20, 50), BackColor = Theme.Muted };
            _statusDot.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.FillEllipse(new SolidBrush(_statusDot.BackColor), 0, 0, 13, 13); };
            _statusLabel = new Label { Text = "Нажмите СКАН для проверки", Font = new Font("Segoe UI", 10), ForeColor = Theme.Muted, AutoSize = true, Location = new Point(42, 47) };
            _statusCard.Controls.AddRange(new Control[] { stTitle, _statusDot, _statusLabel });

            // Loader
            _loaderPanel = new Panel { Size = new Size(740, 36), BackColor = Theme.Background, Visible = false };
            _loaderPanel.Controls.Add(new Label { Text = "Сканирование системы...", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = Theme.Danger, AutoSize = true, Location = new Point(0, 10) });

            // Checks card
            _checkCard   = CreateCard(740, 340);
            var cTitle   = new Label { Text = "Результаты проверки", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(20, 16) };
            _checksFlow  = new FlowLayoutPanel { Location = new Point(12, 48), Size = new Size(716, 278), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Card };
            _checksFlow.Controls.Add(new Label { Text = "Запустите скан для просмотра результатов...", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9), AutoSize = true, Padding = new Padding(4, 4, 0, 0) });
            _checkCard.Controls.Add(cTitle);
            _checkCard.Controls.Add(_checksFlow);

            var btnRow = new FlowLayoutPanel { AutoSize = true, BackColor = Theme.Background, Margin = new Padding(0, 8, 0, 0) };

            _scanButton = new Button { Text = "СКАН", Size = new Size(200, 46), FlatStyle = FlatStyle.Flat, BackColor = Theme.Danger, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 12, 0) };
            _scanButton.FlatAppearance.BorderSize = 0;
            _scanButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 80, 80);
            _scanButton.Click += async (_, _) => await RunScanAsync();

            var fixButton = new Button { Text = "ИСПРАВИТЬ ОДНИМ КЛИКОМ", Size = new Size(260, 46), FlatStyle = FlatStyle.Flat, BackColor = Theme.SoftColor, ForeColor = Color.FromArgb(6, 14, 12), Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            fixButton.FlatAppearance.BorderSize = 0;
            fixButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 200, 90);
            fixButton.Click += (_, _) =>
            {
                MessageBox.Show(
                    "TPM 2.0 и Secure Boot нельзя безопасно включить из Windows через реестр. Откройте UEFI/BIOS и следуйте инструкции результата сканирования. Не меняйте VBS/HVCI автоматически: эти параметры влияют на безопасность системы.",
                    "Безопасное исправление Vanguard", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _statusDot.BackColor  = Theme.Muted;
            _statusLabel.Text     = "Сканирование...";
            _statusLabel.ForeColor = Theme.Muted;

            (CheckResult tpm, CheckResult secureBoot, CheckResult vbs, CheckResult hyperV, CheckResult hvci) data;
            try
            {
                data = await Task.Run(() => _diagnostic.CheckAll());
            }
            catch (Exception ex)
            {
                _loaderPanel.Visible = false;
                _scanButton.Enabled = true;
                _statusDot.BackColor = Theme.Danger;
                _statusLabel.Text = "Диагностика не завершилась";
                _statusLabel.ForeColor = Theme.Danger;
                _checksFlow.Controls.Add(new Label { Text = "Ошибка: " + ex.Message, ForeColor = Theme.Danger, AutoSize = true, Padding = new Padding(4) });
                return;
            }
            var (tpm, secureBoot, vbs, hyperV, hvci) = data;
            var results = new[] { tpm, secureBoot, vbs, hyperV, hvci };
            string[] names = { "TPM 2.0", "Безопасная загрузка", "VBS (Защита виртуализацией)", "Hyper-V", "HVCI (Целостность памяти)" };

            _loaderPanel.Visible = false;
            _scanButton.Enabled  = true;

            bool anyError = false;
            for (int i = 0; i < results.Length; i++)
            {
                var r  = results[i];
                bool ok   = r.Status == CheckStatus.Ok;
                bool warn = r.Status == CheckStatus.Warning;
                if (r.Status == CheckStatus.Error) anyError = true;

                Color dotColor = ok ? Theme.Success : warn ? Theme.Warning : Theme.Danger;

                var row = new Panel { Size = new Size(710, 56), BackColor = Theme.Card, Margin = new Padding(0, 0, 0, 2) };
                var dot = new Panel { Size = new Size(10, 10), Location = new Point(10, 23), BackColor = dotColor };
                dot.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.FillEllipse(new SolidBrush(dot.BackColor), 0, 0, 9, 9); };

                var nameLabel  = new Label { Text = names[i], Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(28, 8) };
                var statusLbl  = new Label { Text = r.Message, Font = new Font("Segoe UI", 8.5f), ForeColor = dotColor, AutoSize = true, Location = new Point(28, 26) };
                var instrLabel = new Label { Text = ok ? "" : r.Instruction, Font = new Font("Segoe UI", 7.5f), ForeColor = Theme.Muted, AutoSize = false, Size = new Size(680, 14), Location = new Point(28, 40), AutoEllipsis = true };

                row.Controls.AddRange(new Control[] { dot, nameLabel, statusLbl, instrLabel });
                row.Paint += (_, e) => { using var p = new Pen(Theme.Border, 1); e.Graphics.DrawLine(p, 0, row.Height - 1, row.Width, row.Height - 1); };
                _checksFlow.Controls.Add(row);
            }

            _statusDot.BackColor   = anyError ? Theme.Danger : Theme.Success;
            _statusLabel.Text      = anyError ? "Обнаружены проблемы — см. детали ниже" : "Vanguard: Все требования выполнены";
            _statusLabel.ForeColor = anyError ? Theme.Danger : Theme.Success;
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
