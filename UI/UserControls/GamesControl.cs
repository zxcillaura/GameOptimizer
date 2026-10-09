using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GameOptimizer.Core.Games;
using GameOptimizer.Core.Themes;
using GameOptimizer.UI.Controls;

namespace GameOptimizer.UI.UserControls
{
    /// <summary>
    /// Per-game launch with an automatic "clean mode": overlays closed, browsers
    /// closed, update stopped, timer at 0.5 ms — and everything handed back on exit.
    /// </summary>
    public class GamesControl : UserControl
    {
        private RichTextBox _log;
        private Label _sessionState;
        private FlatButton _watchBtn;
        private CheckBox _optBrowsers, _optOverlays, _optUpdate, _optHogs, _optMemory;
        private bool _hooked;

        public GamesControl()
        {
            Build();
            Hook();
        }

        private void Hook()
        {
            if (_hooked) return;
            GameSession.Log += OnLog;
            GameSession.Changed += OnChanged;
            _hooked = true;
        }

        private void OnLog(string line)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke(new Action(() => Append(line))); } catch { }
        }

        private void OnChanged(string msg, bool start)
        {
            if (!IsHandleCreated) return;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _sessionState.Text = start ? $"ЧИСТЫЙ РЕЖИМ АКТИВЕН — {msg}"
                                               : $"Ожидание игры  •  последняя сессия: {msg}";
                    _sessionState.ForeColor = start ? Theme.Success : Theme.Muted;
                    Toast.Show(FindForm(),
                        start ? $"{msg}: чистый режим включён" : $"Игра закрыта — {msg}",
                        start ? Theme.Success : Theme.Accent, 3800);
                    RefreshWatchButton();
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
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 186));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // ── game cards ───────────────────────────────────────────────
            var gamesRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent
            };
            gamesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            gamesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            bool first = true;
            foreach (var g in GameSession.Games)
            {
                var card = MakeGameCard(g);
                card.Margin = first ? new Padding(0, 0, 8, 8) : new Padding(8, 0, 0, 8);
                gamesRow.Controls.Add(card, first ? 0 : 1, 0);
                first = false;
            }
            root.Controls.Add(gamesRow, 0, 0);

            // ── session card ─────────────────────────────────────────────
            var sessionCard = new CardPanel
            {
                Dock = DockStyle.Fill, Title = "Чистый игровой режим",
                Subtitle = "включается сам при запуске игры", AccentColor = Theme.SoftColor, ShowAccent = true
            };

            var top = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            _sessionState = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Theme.Muted, AutoSize = false,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ожидание игры"
            };
            _watchBtn = new FlatButton
            {
                Text = "Включить слежение", IconKind = "bolt",
                Style = FlatButtonStyle.Soft, AccentColor = Theme.SoftColor,
                Size = new Size(196, 30), Dock = DockStyle.Right
            };
            _watchBtn.Click += (_, __) => ToggleWatch();
            top.Controls.Add(_sessionState);
            top.Controls.Add(_watchBtn);

            // a wrapping flow panel so the options never run off the card edge
            var opts = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true, FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 4), Margin = new Padding(0)
            };
            _optOverlays = MakeCheck("Закрывать оверлеи", true);
            _optBrowsers = MakeCheck("Закрывать браузеры", true);
            _optHogs     = MakeCheck("Закрывать фоновые (Overwolf, BlueStacks, Ollama)", true);
            _optUpdate   = MakeCheck("Останавливать Windows Update", true);
            _optMemory   = MakeCheck("Чистить память и таймер 0.5 мс", true);

            foreach (var cb in new[] { _optOverlays, _optBrowsers, _optHogs, _optUpdate, _optMemory })
            {
                cb.Margin = new Padding(0, 0, 16, 2);
                opts.Controls.Add(cb);
                cb.CheckedChanged += (_, __) => PushOptions();
            }

            _log = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Theme.LogBg, ForeColor = Theme.LogFg,
                Font = new Font("Consolas", 8.8f), WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            sessionCard.Controls.Add(_log);
            sessionCard.Controls.Add(opts);
            sessionCard.Controls.Add(top);
            root.Controls.Add(sessionCard, 0, 1);

            PushOptions();
            RefreshWatchButton();
        }

        private CardPanel MakeGameCard(GameSession.GameDef g)
        {
            string cfg = Path.Combine(
                GameConfigWriter.CommonDir,
                g.Key == "cs2" ? "Counter-Strike Global Offensive" : "dota 2 beta",
                "game",
                g.Key == "cs2" ? "csgo" : "dota",
                "cfg",
                "autoexec.cfg");

            bool installed = File.Exists(cfg) || Directory.Exists(Path.GetDirectoryName(cfg));
            bool configured = false;
            try { configured = File.Exists(cfg) && File.ReadAllText(cfg).Contains("GAMEOPTIMIZ"); }
            catch { }

            var card = new CardPanel
            {
                Dock = DockStyle.Fill, Title = g.Name,
                Subtitle = installed ? (configured ? "конфиг применяется" : "конфиг не применён") : "не найдена",
                AccentColor = configured ? Theme.Success : Theme.Warning, ShowAccent = true
            };

            var info = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Theme.TextSecondary,
                AutoSize = false, TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Consolas", 8.6f),
                Text = "Ключевые параметры:\r\n" +
                       $"  {g.Convar}\r\n" +
                       "  приоритет High + все ядра\r\n" +
                       "  таймер 0.5 мс, standby очищен"
            };

            var btn = new FlatButton
            {
                Text = "Запустить", IconKind = "gamepad",
                Style = FlatButtonStyle.Filled, AccentColor = Theme.Accent,
                Dock = DockStyle.Bottom, Height = 32, Enabled = installed
            };
            btn.Click += (_, __) =>
            {
                if (GameSession.Launch(g))
                    Toast.Show(FindForm(), $"{g.Name}: запуск и чистый режим", Theme.Accent, 3600);
            };

            card.Controls.Add(info);
            card.Controls.Add(btn);
            return card;
        }

        private static CheckBox MakeCheck(string text, bool value) => new()
        {
            Text = text, Checked = value, AutoSize = true,
            ForeColor = Theme.TextSecondary, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.4f)
        };

        private void PushOptions()
        {
            GameSession.Options = new SessionOptions
            {
                CloseOverlays     = _optOverlays.Checked,
                CloseBrowsers     = _optBrowsers.Checked,
                CloseOtherHogs    = _optHogs.Checked,
                StopWindowsUpdate = _optUpdate.Checked,
                TrimMemory        = _optMemory.Checked
            };
        }

        private void ToggleWatch()
        {
            if (GameSession.Watching) GameSession.StopWatching();
            else                      GameSession.StartWatching();
            RefreshWatchButton();
        }

        private void RefreshWatchButton()
        {
            _watchBtn.Text = GameSession.Watching ? "Выключить слежение" : "Включить слежение";
            _watchBtn.AccentColor = GameSession.Watching ? Theme.Warning : Theme.SoftColor;
        }

        private void Append(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = line.Contains("SESSION")
                ? (line.Contains("──") ? Theme.Accent : Theme.LogFg)
                : Theme.TextSecondary;
            _log.AppendText(line + Environment.NewLine);
            _log.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _hooked)
            {
                GameSession.Log -= OnLog;
                GameSession.Changed -= OnChanged;
            }
            base.Dispose(disposing);
        }
    }
}
