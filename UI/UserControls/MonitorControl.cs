using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using GameOptimizer.Core.Monitor;
using GameOptimizer.Core.Themes;
using GameOptimizer.UI;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>Live telemetry: gauges, rolling charts and the hungriest processes.</summary>
    public class MonitorControl : UserControl
    {
        private RingGauge _cpu, _gpu, _ram, _temp;
        private LiveChart _chartLoad, _chartMem;
        private ListView _procs;
        private Label _status;
        private bool _subscribed;
        private Timer _procTimer;

        public MonitorControl()
        {
            Build();
            VisibleChanged += (_, __) => Sync();
            HandleDestroyed += (_, __) => Unsubscribe();
        }

        private void Sync()
        {
            if (Visible) Subscribe();
            else Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            SystemMonitor.Sampled += OnSample;
            SystemMonitor.Start(1000);
            _subscribed = true;
            DarkMode.Apply(_procs);   // dark header + scrollbar for the process list
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            SystemMonitor.Sampled -= OnSample;
            _subscribed = false;
        }

        private void OnSample(Sample s)
        {
            if (!IsHandleCreated) return;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _cpu.Value = s.CpuLoad;
                    _gpu.Value = s.GpuValid ? s.GpuLoad : 0;
                    _ram.Value = s.RamPercent;
                    _temp.Value = s.GpuValid ? Math.Min(100, s.GpuTempC) : 0;

                    _cpu.BigText = $"{s.CpuLoad:F0}";
                    _gpu.BigText = s.GpuValid ? $"{s.GpuLoad:F0}" : "—";
                    _ram.BigText = $"{s.RamPercent:F0}";
                    _temp.BigText = s.GpuValid ? $"{s.GpuTempC:F0}" : "—";
                    _temp.Unit = s.GpuValid ? $"°C   {s.GpuClockMhz:F0} МГц" : "нет данных";

                    _chartLoad.Push(s.CpuLoad, s.GpuLoad);
                    _chartMem.Push(s.GpuValid ? s.GpuMemPct : 0, s.RamPercent);

                    _status.Text = $"CPU {s.CpuClockMhz:F0} МГц   •   " +
                                   $"GPU {s.GpuClockMhz:F0} МГц / {s.GpuPowerW:F0} Вт   •   " +
                                   $"VRAM {(s.GpuMemUsedMb / 1024.0):F1} ГБ   •   " +
                                   $"RAM {s.RamUsedGb:F1} / {s.RamTotalGb:F0} ГБ   •   " +
                                   $"процессов: {SystemMonitor.ProcessCount}";
                }));
            }
            catch { }
        }

        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(16);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));  // gauges
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // status line
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));    // charts
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));    // processes
            Controls.Add(root);

            // ── gauges ───────────────────────────────────────────────────
            var gaugeRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++) gaugeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _cpu  = MakeGauge("загрузка CPU", "%");
            _gpu  = MakeGauge("загрузка GPU", "%");
            _ram  = MakeGauge("память", "%");
            _temp = MakeGauge("температура GPU", "°C");

            gaugeRow.Controls.Add(Wrap(_cpu), 0, 0);
            gaugeRow.Controls.Add(Wrap(_gpu), 1, 0);
            gaugeRow.Controls.Add(Wrap(_ram), 2, 0);
            gaugeRow.Controls.Add(Wrap(_temp), 3, 0);
            root.Controls.Add(gaugeRow, 0, 0);

            _status = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Theme.Muted, AutoSize = false,
                Font = new Font("Consolas", 8.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(_status, 0, 1);

            // ── charts ───────────────────────────────────────────────────
            var chartRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent
            };
            chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var c1 = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8), FillColor = Theme.Card };
            _chartLoad = new LiveChart
            {
                Dock = DockStyle.Fill, Title = "Загрузка CPU / GPU", Subtitle = "60 сек",
                Max = 100, Unit = "%", Capacity = 60, BackColor = Theme.Card
            };
            _chartLoad.AddSeries("CPU", Theme.GradientStart, true);
            _chartLoad.AddSeries("GPU", Theme.AccentAlt);
            c1.Controls.Add(_chartLoad);

            var c2 = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 8), FillColor = Theme.Card };
            _chartMem = new LiveChart
            {
                Dock = DockStyle.Fill, Title = "VRAM / RAM", Subtitle = "60 сек",
                Max = 100, Unit = "%", Capacity = 60, BackColor = Theme.Card
            };
            _chartMem.AddSeries("VRAM", Theme.AccentPink, true);
            _chartMem.AddSeries("RAM", Theme.GoldColor);
            c2.Controls.Add(_chartMem);

            chartRow.Controls.Add(c1, 0, 0);
            chartRow.Controls.Add(c2, 1, 0);
            root.Controls.Add(chartRow, 0, 2);

            // ── processes ────────────────────────────────────────────────
            var procCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Самые прожорливые процессы",
                Subtitle = "обновление каждые 2 сек", AccentColor = Theme.AccentPink
            };
            _procs = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Theme.Card, ForeColor = Theme.TextSecondary,
                BorderStyle = BorderStyle.None, HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Consolas", 8.8f), OwnerDraw = false
            };
            _procs.Columns.Add("Процесс", 240);
            _procs.Columns.Add("PID", 70);
            _procs.Columns.Add("CPU %", 90);
            _procs.Columns.Add("Память, МБ", 120);
            procCard.Controls.Add(_procs);
            root.Controls.Add(procCard, 0, 3);

            _procTimer = new Timer { Interval = 2000 };
            _procTimer.Tick += (_, __) =>
            {
                if (!Visible) return;
                var list = SystemMonitor.TopProcesses(9);
                _procs.BeginUpdate();
                _procs.Items.Clear();
                foreach (var p in list)
                {
                    var it = new ListViewItem(new[]
                    {
                        p.Name, p.Pid.ToString(), $"{p.CpuPct:F1}", $"{p.MemMb:F0}"
                    });
                    if (p.CpuPct > 25) it.ForeColor = Theme.Warning;
                    _procs.Items.Add(it);
                }
                _procs.EndUpdate();
            };
            _procTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _procTimer?.Stop(); _procTimer?.Dispose(); } catch { }
                Unsubscribe();
            }
            base.Dispose(disposing);
        }

        private static RingGauge MakeGauge(string caption, string unit)
        {
            return new RingGauge
            {
                Dock = DockStyle.Fill, Value = 0, MaxValue = 100,
                Caption = caption, Unit = unit, BigText = "0",
                AccentFrom = Theme.GradientStart, AccentTo = Theme.GradientEnd,
                Thickness = 9
            };
        }

        private static Panel Wrap(Control inner)
        {
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            host.Controls.Add(inner);
            return host;
        }
    }
}
