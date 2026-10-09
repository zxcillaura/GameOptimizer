using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core;

namespace GameOptimizer.UI.UserControls
{
    public partial class CleanerControl : UserControl
    {
        private CheckBox _chkTemp;
        private CheckBox _chkPrefetch;
        private CheckBox _chkShader;
        private CheckBox _chkBrowser;
        private CheckBox _chkLogs;
        private Button _btnClean;
        private Button _btnQuickClean;
        private RichTextBox _logText;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        public CleanerControl()
        {
            InitializeComponent();
            this.BackColor = Theme.Current.Background;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Controls.Clear();
            this.BackColor = Theme.Current.Background;

            var title = new Label
            {
                Text = "🧹 Очистка системы",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(title);

            var panel = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(600, 160),
                BackColor = Color.FromArgb(30, 30, 35)
            };
            this.Controls.Add(panel);

            int y = 15;
            _chkTemp = CreateCheckbox("🧹 Временные файлы (Temp)", panel, ref y);
            _chkPrefetch = CreateCheckbox("⚡ Prefetch (кэш запуска программ)", panel, ref y);
            _chkShader = CreateCheckbox("🎮 Кэш шейдеров (D3D, NVIDIA, AMD)", panel, ref y);
            _chkBrowser = CreateCheckbox("🌐 Кэш браузеров (Chrome, Edge)", panel, ref y);
            _chkLogs = CreateCheckbox("📋 Логи и отчёты об ошибках", panel, ref y);

            _chkTemp.Checked = true;
            _chkPrefetch.Checked = true;
            _chkShader.Checked = true;
            _chkBrowser.Checked = true;
            _chkLogs.Checked = true;

            _btnQuickClean = new Button
            {
                Text = "⚡ Быстрая очистка",
                Location = new Point(20, 240),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnQuickClean.Click += BtnQuickClean_Click;
            this.Controls.Add(_btnQuickClean);

            _btnClean = new Button
            {
                Text = "🧹 Глубокая очистка",
                Location = new Point(180, 240),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnClean.Click += BtnClean_Click;
            this.Controls.Add(_btnClean);

            var btnSelectAll = new Button
            {
                Text = "✅ Выбрать всё",
                Location = new Point(340, 240),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnSelectAll.Click += (s, e) => { foreach (var cb in new[] { _chkTemp, _chkPrefetch, _chkShader, _chkBrowser, _chkLogs }) cb.Checked = true; };
            this.Controls.Add(btnSelectAll);

            var btnDeselectAll = new Button
            {
                Text = "❌ Снять всё",
                Location = new Point(470, 240),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnDeselectAll.Click += (s, e) => { foreach (var cb in new[] { _chkTemp, _chkPrefetch, _chkShader, _chkBrowser, _chkLogs }) cb.Checked = false; };
            this.Controls.Add(btnDeselectAll);

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 290),
                Size = new Size(600, 15),
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(_progressBar);

            _statusLabel = new Label
            {
                Text = "🟢 Готов к очистке",
                Location = new Point(20, 315),
                Size = new Size(600, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(_statusLabel);

            _logText = new RichTextBox
            {
                Location = new Point(20, 345),
                Size = new Size(600, 170),
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(_logText);

            Log("🧹 Cleaner Control готов к работе.");
        }

        private CheckBox CreateCheckbox(string text, Panel parent, ref int y)
        {
            var cb = new CheckBox
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(550, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            parent.Controls.Add(cb);
            y += 30;
            return cb;
        }

        private void BtnQuickClean_Click(object sender, EventArgs e)
        {
            Log("⚡ Запуск быстрой очистки...");
            _statusLabel.Text = "⚡ Быстрая очистка...";
            _progressBar.Style = ProgressBarStyle.Marquee;

            Task.Run(() =>
            {
                try
                {
                    SystemCleaner.QuickCleanup(Log);
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка: {ex.Message}");
                }
                finally
                {
                    this.Invoke(new Action(() =>
                    {
                        _progressBar.Style = ProgressBarStyle.Continuous;
                        _progressBar.Value = 100;
                        _statusLabel.Text = "✅ Быстрая очистка завершена!";
                    }));
                }
            });
        }

        private void BtnClean_Click(object sender, EventArgs e)
        {
            bool anyChecked = _chkTemp.Checked || _chkPrefetch.Checked || _chkShader.Checked || _chkBrowser.Checked || _chkLogs.Checked;
            if (!anyChecked)
            {
                MessageBox.Show("Выберите хотя бы один пункт для очистки!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusLabel.Text = "⚠️ Ничего не выбрано";
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите выполнить глубокую очистку?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                _statusLabel.Text = "🟢 Очистка отменена";
                return;
            }

            Log("🧹 Запуск глубокой очистки...");
            _statusLabel.Text = "🧹 Глубокая очистка...";
            _progressBar.Style = ProgressBarStyle.Marquee;
            ToggleButtons(false);

            Task.Run(() =>
            {
                try
                {
                    SystemCleaner.RunDeepCleanup(Log);
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка: {ex.Message}");
                }
                finally
                {
                    this.Invoke(new Action(() =>
                    {
                        _progressBar.Style = ProgressBarStyle.Continuous;
                        _progressBar.Value = 100;
                        _statusLabel.Text = "✅ Глубокая очистка завершена!";
                        ToggleButtons(true);
                    }));
                }
            });
        }

        private void ToggleButtons(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ToggleButtons(enabled)));
                return;
            }
            _btnClean.Enabled = enabled;
            _btnQuickClean.Enabled = enabled;
        }

        private void Log(string message)
        {
            if (_logText.InvokeRequired)
            {
                _logText.Invoke(new Action(() => Log(message)));
                return;
            }
            _logText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            _logText.ScrollToCaret();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "CleanerControl";
            this.Size = new Size(660, 540);
            this.ResumeLayout(false);
        }
    }
}