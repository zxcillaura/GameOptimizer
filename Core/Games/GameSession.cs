using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GameOptimizer.Core.Latency;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Games
{
    public sealed class SessionOptions
    {
        public bool CloseBrowsers     { get; set; } = true;
        public bool CloseOverlays     { get; set; } = true;
        public bool StopWindowsUpdate { get; set; } = true;
        public bool CloseOtherHogs    { get; set; } = true;
        public bool TrimMemory        { get; set; } = true;
    }

    /// <summary>
    /// Watches for CS2 / Dota 2. While a game is running the machine is put into
    /// "clean" state; the moment the game closes, everything is handed back.
    /// Nothing here touches the registry, so it cannot leave permanent changes.
    /// </summary>
    public static class GameSession
    {
        public sealed class GameDef
        {
            public string Key      { get; set; } = "";
            public string Name     { get; set; } = "";
            public string Exe      { get; set; } = "";
            public string SteamUrl { get; set; } = "";
            public string Convar   { get; set; } = "";
        }

        public static readonly GameDef[] Games =
        {
            new GameDef { Key = "cs2",   Name = "Counter-Strike 2", Exe = "cs2",
                          SteamUrl = "steam://rungameid/730", Convar = "r_low_latency \"2\"" },
            new GameDef { Key = "dota2", Name = "Dota 2",           Exe = "dota2",
                          SteamUrl = "steam://rungameid/570", Convar = "fps_max 162" },
        };

        public static SessionOptions Options { get; set; } = new();
        public static bool   Active      { get; private set; }
        public static string ActiveGame  { get; private set; } = "";
        public static DateTime StartedAt { get; private set; }
        public static TimeSpan LastDuration { get; private set; }
        public static string LastSummary { get; private set; } = "";
        public static bool   Watching    { get; private set; }

        /// <summary>Raised on session state changes: (message, isStart).</summary>
        public static event Action<string, bool> Changed;
        public static event Action<string> Log;

        private static Thread _thread;
        private static volatile bool _stop = true;
        private static readonly List<string> _closedNow = new();

        private static readonly string[] BrowserNames =
        {
            "chrome", "msedge", "firefox", "browser", "opera", "vivaldi", "brave"
        };
        private static readonly string[] HogNames =
        {
            "Overwolf", "HD-Player", "BlueStacks", "RobloxPlayerBeta", "Notion", "Ollama", "EADM"
        };

        public static void StartWatching()
        {
            if (Watching) return;
            Watching = true;
            _stop = false;
            _thread = new Thread(Loop) { IsBackground = true, Name = "GAMEOPTIMIZv7-Session" };
            _thread.Start();
            Log?.Invoke("[SESSION] слежение включено — как только запустишь CS2 или Dota 2, режим включится сам");
        }

        public static void StopWatching()
        {
            Watching = false;
            _stop = true;
            if (Active) EndSession();
            Log?.Invoke("[SESSION] слежение выключено");
        }

        public static bool Launch(GameDef game)
        {
            try
            {
                Process.Start(new ProcessStartInfo(game.SteamUrl) { UseShellExecute = true });
                Log?.Invoke($"[SESSION] запускаю {game.Name}...");
                if (!Watching) StartWatching();
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[SESSION] не удалось запустить {game.Name}: {ex.Message}");
                return false;
            }
        }

        private static void Loop()
        {
            int goneTicks = 0;
            while (!_stop)
            {
                try
                {
                    var running = Games.Where(g => Process.GetProcessesByName(g.Exe).Length > 0).ToList();

                    if (running.Count > 0)
                    {
                        goneTicks = 0;
                        if (!Active) BeginSession(running[0]);
                    }
                    else if (Active)
                    {
                        if (++goneTicks >= 2) EndSession();
                    }
                }
                catch { }
                Thread.Sleep(2000);
            }
        }

        private static void BeginSession(GameDef game)
        {
            Active = true;
            ActiveGame = game.Name;
            StartedAt = DateTime.Now;
            _closedNow.Clear();

            var notes = new List<string>();
            Log?.Invoke($"[SESSION] ── {game.Name} обнаружен, включаю чистый режим ──");

            if (Options.StopWindowsUpdate)
            {
                ProcessRunner.Run("sc.exe", "stop wuauserv");
                notes.Add("Windows Update остановлен");
            }

            if (Options.CloseOverlays)
            {
                SystemTweaks.CloseOverlayProcesses(s => { });
                notes.Add("оверлеи закрыты");
            }

            if (Options.CloseBrowsers)
            {
                int n = KillByNames(BrowserNames, "браузер");
                if (n > 0) notes.Add($"закрыто браузеров: {n}");
            }

            if (Options.CloseOtherHogs)
            {
                int n = KillByNames(HogNames, "фон");
                if (n > 0) notes.Add($"закрыто фоновых процессов: {n}");
            }

            LatencyKeeper.Start(s => { });

            if (Options.TrimMemory)
            {
                LatencyKeeper.PurgeStandby();
                SystemTweaks.TrimWorkingSets(s => { });
                notes.Add("память очищена");
            }

            LastSummary = notes.Count > 0 ? string.Join(", ", notes) : "без дополнительных действий";
            Log?.Invoke($"[SESSION] режим активен: таймер 0.5 мс, приоритет High, {LastSummary}");
            Changed?.Invoke(game.Name, true);
        }

        private static void EndSession()
        {
            LastDuration = DateTime.Now - StartedAt;
            Active = false;
            LatencyKeeper.Stop(s => { });

            string dur = LastDuration.TotalHours >= 1
                ? $"{(int)LastDuration.TotalHours} ч {LastDuration.Minutes} мин"
                : $"{LastDuration.Minutes} мин {LastDuration.Seconds} с";

            Log?.Invoke($"[SESSION] ── игра закрыта, чистый режим снят (сессия {dur}) ──");
            Changed?.Invoke($"{ActiveGame}: {dur}", false);
            ActiveGame = "";
        }

        private static int KillByNames(IEnumerable<string> names, string kind)
        {
            int killed = 0;
            foreach (var name in names)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (p.Id == Process.GetCurrentProcess().Id) continue;
                        string title = p.MainWindowTitle;
                        p.Kill();
                        killed++;
                        _closedNow.Add(p.ProcessName);
                        if (!string.IsNullOrWhiteSpace(title))
                            Log?.Invoke($"[SESSION]   закрыт {kind}: {p.ProcessName} — {title}");
                    }
                    catch { }
                }
            }
            return killed;
        }
    }
}
