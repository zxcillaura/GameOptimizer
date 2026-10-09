using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Bench;
using GameOptimizer.Core.Games;
using GameOptimizer.Core.Hardware;
using GameOptimizer.Core.Latency;
using GameOptimizer.Core.Monitor;
using GameOptimizer.Core.Profiles;
using GameOptimizer.Core.Tweaks;

namespace GameOptimizer.Cli
{
    /// <summary>
    /// Headless mode — the same engine the GUI uses, driven from the terminal.
    ///   GAMEOPTIMIZ1.0.exe /scan
    ///   GAMEOPTIMIZ1.0.exe /apply balanced [--no-restore-point] [--latency]
    ///   GAMEOPTIMIZ1.0.exe /revert
    ///   GAMEOPTIMIZ1.0.exe /latency on|off
    ///   GAMEOPTIMIZ1.0.exe /purge
    ///   GAMEOPTIMIZ1.0.exe /report
    /// </summary>
    public static class CliRunner
    {
        [DllImport("kernel32.dll")] private static extern bool AttachConsole(int processId);

        private const int AttachParentProcess = -1;

        public static int Run(string[] args)
        {
            // attach to the calling terminal; .NET then writes through the console
            // wide API, so Cyrillic renders correctly in cmd/PowerShell and in files
            AttachConsole(AttachParentProcess);

            string cmd = args[0].TrimStart('/', '-').ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            try
            {
                switch (cmd)
                {
                    case "scan":      return Scan();
                    case "doctor":    return Doctor();
                    case "report":    return Scan(writeFile: true);
                    case "apply":     return Apply(rest);
                    case "revert":    return Revert();
                    case "latency":   return Latency(rest);
                    case "purge":     return Purge();
                    case "backup":    return BackupOnly();
                    case "monitor":   return MonitorOnce();
                    case "tweaks":    return TweaksList(rest);
                    case "tweak":     return TweakCommand(rest);
                    case "bench":     return Bench(rest);
                    case "session":   return SessionStatus();
                    case "help":
                    default:          return Help();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА] {ex.Message}");
                return 1;
            }
        }

        private static int Scan(bool writeFile = false)
        {
            var rig = HardwareScanner.Scan();
            string text = rig.ToText();
            Console.WriteLine(text);
            if (writeFile)
            {
                string path = Path.Combine(BackupManager.Root, $"report-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                Directory.CreateDirectory(BackupManager.Root);
                File.WriteAllText(path, text, new UTF8Encoding(true));
                Console.WriteLine($"  Отчёт сохранён: {path}");
            }
            return 0;
        }

        private static int Doctor()
        {
            Console.WriteLine("GAMEOPTIMIZ 4.0 — самодиагностика");
            Console.WriteLine();
            var rig = HardwareScanner.Scan();
            var health = GameOptimizer.Core.Health.HealthReport.Evaluate(rig);
            Console.WriteLine(rig.ToText());
            Console.WriteLine();
            Console.WriteLine($"Проверка применимости каталога твиков: ");
            var items = AllTweaks(true);
            int applicable = 0, unsupported = 0;
            foreach (var item in items)
            {
                string reason = item.Applicability(rig);
                if (reason == null) applicable++;
                else
                {
                    unsupported++;
                    Console.WriteLine($"  - {item.Id}: {reason}");
                }
            }
            Console.WriteLine($"Применимы: {applicable}; скрыты как неприменимые: {unsupported}; всего: {items.Count}.");
            Console.WriteLine($"HealthScore: {health.Score}/100 ({health.Grade}) — {health.Verdict}");
            return 0;
        }

        private static int Apply(string[] args)
        {
            string name = args.FirstOrDefault(a => !a.StartsWith("-"))?.ToLowerInvariant() ?? "balanced";
            ProfileKind profile = name switch
            {
                "safe"    => ProfileKind.Safe,
                "extreme" => ProfileKind.Extreme,
                _         => ProfileKind.Balanced
            };
            bool restorePoint = !args.Any(a => a.Equals("--no-restore-point", StringComparison.OrdinalIgnoreCase));
            bool latency      = args.Any(a => a.Equals("--latency", StringComparison.OrdinalIgnoreCase));
            var categories = ProfileRunner.Categories.Select(c => c.Key).ToArray();

            string backup = ProfileRunner.Run(profile, categories, restorePoint, Console.WriteLine);
            if (string.IsNullOrWhiteSpace(backup)) return 1;
            if (latency) Console.WriteLine("[LATENCY] " + LatencyKeeper.Start(Console.WriteLine));
            return 0;
        }

        private static int Revert()
        {
            var categories = ProfileRunner.Categories.Select(c => c.Key).ToArray();
            int before = TweakCatalog.Build().Count(t => TweakRunner.HasBackup(t.Id));
            ProfileRunner.Run(ProfileKind.Factory, categories, false, Console.WriteLine);
            int after = TweakCatalog.Build().Count(t => TweakRunner.HasBackup(t.Id));
            Console.WriteLine(before > 0 ? $"[ОТКАТ] обработано твиков с бэкапом: {before}; доступно для повторного отката: {after}" : "[ОТКАТ] нечего восстанавливать");
            return before > 0 ? 0 : 1;
        }

        private static int Latency(string[] args)
        {
            string mode = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";
            switch (mode)
            {
                case "on":  Console.WriteLine("[LATENCY] " + LatencyKeeper.Start(Console.WriteLine)); break;
                case "off": Console.WriteLine("[LATENCY] " + LatencyKeeper.Stop(Console.WriteLine));  break;
                default:    Console.WriteLine($"[LATENCY] состояние: {LatencyKeeper.Status}"); break;
            }
            return 0;
        }

        private static int Purge()
        {
            bool ok = LatencyKeeper.PurgeStandby();
            Console.WriteLine(ok
                ? "[MEMORY] список ожидания (standby) очищен"
                : "[MEMORY] не удалось очистить standby (нужны права администратора)");
            return ok ? 0 : 1;
        }

        private static int BackupOnly()
        {
            BackupManager.Begin("manual");
            SystemTweaks.CreateRestorePoint(Console.WriteLine);
            string path = BackupManager.Save();
            Console.WriteLine($"[БЭКАП] {path}");
            return 0;
        }

        private static int MonitorOnce()
        {
            Console.WriteLine("Снимаю живые показатели (5 секунд)...");
            Sample s = null;
            SystemMonitor.Sampled += x => s = x;
            SystemMonitor.Start(800);
            System.Threading.Thread.Sleep(5000);
            s = SystemMonitor.Latest;
            SystemMonitor.Stop();

            if (s == null) { Console.WriteLine("Данные не собраны."); return 1; }

            Console.WriteLine();
            Console.WriteLine($"  CPU          : {s.CpuLoad:F1} %   ({s.CpuClockMhz:F0} МГц)");
            Console.WriteLine($"  RAM          : {s.RamPercent:F1} %   ({s.RamUsedGb:F1} / {s.RamTotalGb:F0} ГБ)");
            if (s.GpuValid)
            {
                Console.WriteLine($"  GPU          : {s.GpuLoad:F1} %   ({s.GpuClockMhz:F0} МГц, {s.GpuPowerW:F0} Вт)");
                Console.WriteLine($"  VRAM         : {s.GpuMemPct:F1} %   ({s.GpuMemUsedMb / 1024.0:F1} ГБ)");
                Console.WriteLine($"  Температура  : {s.GpuTempC:F0} °C");
            }
            else Console.WriteLine("  GPU          : данные nvidia-smi недоступны");

            // first call only builds the CPU-time baseline, so measure twice
            SystemMonitor.TopProcesses(8);
            System.Threading.Thread.Sleep(1500);
            var procs = SystemMonitor.TopProcesses(8);

            Console.WriteLine();
            Console.WriteLine("  Топ процессов по CPU:");
            Console.WriteLine("    " + "процесс".PadRight(26) + "PID".PadLeft(8) + "CPU %".PadLeft(9) + "МБ".PadLeft(10));
            foreach (var p in procs)
                Console.WriteLine($"    {p.Name.PadRight(26)}{p.Pid,8}{p.CpuPct,9:F1}{p.MemMb,10:F0}");

            Console.WriteLine();
            Console.WriteLine($"  Всего процессов: {SystemMonitor.ProcessCount}");
            return 0;
        }

        private static int Bench(string[] args)
        {
            string path = args.FirstOrDefault(a => !a.StartsWith("-"));
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Console.WriteLine("Укажи CSV-файл:  GAMEOPTIMIZ1.0.exe /bench \"C:\\путь\\capture.csv\" [игра] [метка]");
                var found = BenchmarkStore.FindLikelyCaptures();
                if (found.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Похожие файлы найдены автоматически:");
                    foreach (var f in found.Take(8)) Console.WriteLine("  " + f);
                }
                return 1;
            }

            string game = args.Skip(1).FirstOrDefault(a => !a.StartsWith("-")) ?? "CS2";
            string label = args.Skip(2).FirstOrDefault(a => !a.StartsWith("-")) ?? "замер";

            var run = BenchmarkStore.ImportCsv(path, game, label, out string error);
            if (run == null) { Console.WriteLine("Ошибка импорта: " + error); return 1; }

            Console.WriteLine();
            Console.WriteLine("══════════ РЕЗУЛЬТАТ ЗАМЕРА ══════════");
            Console.WriteLine($"  Игра          : {run.Game}   ({run.Label})");
            Console.WriteLine($"  Кадров        : {run.Frames} за {run.DurationSec:F1} с");
            Console.WriteLine($"  Средний FPS   : {run.AvgFps:F1}");
            Console.WriteLine($"  1% low        : {run.OnePercentLow:F1}");
            Console.WriteLine($"  0.1% low      : {run.PointOnePercentLow:F1}");
            Console.WriteLine($"  Средний frame : {run.AvgFrameMs:F2} мс (медиана {run.MedianFrameMs:F2} мс)");
            Console.WriteLine($"  Микрофризы    : {run.Stutters}");

            var prev = BenchmarkStore.History.FirstOrDefault(h => h.Game == run.Game);
            if (prev != null)
            {
                double d1 = run.OnePercentLow - prev.OnePercentLow;
                double da = run.AvgFps - prev.AvgFps;
                Console.WriteLine();
                Console.WriteLine($"  Прошлый замер : {prev.Date}  «{prev.Label}»");
                Console.WriteLine($"  Средний FPS   : {prev.AvgFps:F0} -> {run.AvgFps:F0}  ({da:+0;-0;0})");
                Console.WriteLine($"  1% low        : {prev.OnePercentLow:F0} -> {run.OnePercentLow:F0}  ({d1:+0;-0;0})");
            }
            BenchmarkStore.Add(run);
            return 0;
        }

        private static int SessionStatus()
        {
            Console.WriteLine();
            Console.WriteLine("═══ ЧИСТЫЙ ИГРОВОЙ РЕЖИМ ═══");
            Console.WriteLine($"  Слежение        : {(GameSession.Watching ? "включено" : "выключено")}");
            Console.WriteLine($"  Сессия активна  : {(GameSession.Active ? $"да ({GameSession.ActiveGame})" : "нет")}");
            Console.WriteLine($"  Последняя       : {(GameSession.LastDuration.TotalSeconds > 0 ? $"{GameSession.LastDuration.TotalMinutes:F0} мин" : "не было")}");
            Console.WriteLine($"  Опции           : оверлеи={GameSession.Options.CloseOverlays}, " +
                              $"браузеры={GameSession.Options.CloseBrowsers}, " +
                              $"фон={GameSession.Options.CloseOtherHogs}, " +
                              $"update={GameSession.Options.StopWindowsUpdate}, " +
                              $"память={GameSession.Options.TrimMemory}");
            Console.WriteLine();
            foreach (var g in GameSession.Games)
            {
                string exe = g.Key == "cs2" ? "cs2.exe" : "dota2.exe";
                bool running = System.Diagnostics.Process.GetProcessesByName(g.Exe).Length > 0;
                Console.WriteLine($"  {g.Name,-18} {(running ? "ЗАПУЩЕНА" : "не запущена")}   {g.SteamUrl}");
            }
            return 0;
        }

        private static List<TweakItem> AllTweaks(bool refreshSystemCaches)
        {
            if (refreshSystemCaches) TweakCatalogSystem.Refresh();
            var items = TweakCatalog.Build();
            items.AddRange(TweakCatalogSystem.BuildServices());
            items.AddRange(TweakCatalogSystem.BuildTasks());
            return items;
        }

        private static int TweaksList(string[] args)
        {
            string filter = (args.FirstOrDefault(a => !a.StartsWith("-")) ?? "").ToLowerInvariant();
            var items = AllTweaks(true);

            Console.WriteLine();
            Console.WriteLine("СОСТОЯНИЕ   РИСК       ГРУППА        ID / НАЗВАНИЕ");
            Console.WriteLine(new string('-', 104));

            int applied = 0, shown = 0;
            foreach (var it in items)
            {
                if (filter.Length > 0 &&
                    !($"{it.Id} {it.Title} {it.Group} {it.Note}").ToLowerInvariant().Contains(filter)) continue;

                shown++;
                var st = it.DetectSafe();
                if (st.Status == TweakStatus.Applied) applied++;
                string risk = it.Risk switch
                {
                    TweakRisk.Safe    => "безопасно",
                    TweakRisk.Caution => "осторожно",
                    _                 => "РИСК"
                };

                Console.WriteLine($"{st.StatusText,-12}{risk,-11}{it.Group,-14}{it.Id}");
                Console.WriteLine($"{"",-37}{it.Title}");
                if (!string.IsNullOrWhiteSpace(st.Detail))
                    Console.WriteLine($"{"",-37}{st.Detail}");
            }

            Console.WriteLine(new string('-', 104));
            Console.WriteLine(filter.Length > 0
                ? $"Включено: {applied} из {shown} показанных (всего в каталоге {items.Count})."
                : $"Включено: {applied} из {shown} пунктов.");
            Console.WriteLine("Управление:  GAMEOPTIMIZ1.0.exe /tweak <id> on|off|status");
            return 0;
        }

        private static int TweakCommand(string[] args)
        {
            string id = args.FirstOrDefault(a => !a.StartsWith("-"));
            string mode = (args.Skip(1).FirstOrDefault(a => !a.StartsWith("-")) ?? "status").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Укажи id:  GAMEOPTIMIZ1.0.exe /tweak power-ultimate on");
                Console.WriteLine("Список id: GAMEOPTIMIZ1.0.exe /tweaks");
                return 1;
            }

            bool system = id.StartsWith("svc:", StringComparison.OrdinalIgnoreCase)
                       || id.StartsWith("task:", StringComparison.OrdinalIgnoreCase);
            var item = AllTweaks(system)
                .FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (item == null) { Console.WriteLine($"Твик «{id}» не найден. Посмотри список: /tweaks"); return 1; }

            switch (mode)
            {
                case "on":
                    Console.WriteLine($"▶ {item.Title}");
                    var (ok, backup) = TweakRunner.ApplyOne(item, Console.WriteLine);
                    Console.WriteLine(ok ? $"✓ включено  ({backup})" : "✗ не применилось");
                    return ok ? 0 : 1;

                case "off":
                    Console.WriteLine($"◀ откат: {item.Title}");
                    bool done = TweakRunner.RevertOne(item, Console.WriteLine);
                    Console.WriteLine(done ? "✓ возвращено как было" : "нет сохранённого бэкапа для этого твика");
                    return done ? 0 : 1;

                default:
                    var st = item.DetectSafe();
                    Console.WriteLine();
                    Console.WriteLine($"  {item.Title}   [{item.Id}]");
                    Console.WriteLine($"  группа : {item.Group}");
                    Console.WriteLine($"  риск   : {(item.Risk == TweakRisk.Safe ? "безопасно" : item.Risk == TweakRisk.Caution ? "осторожно" : "РИСК")}");
                    Console.WriteLine($"  статус : {st.StatusText}");
                    Console.WriteLine($"  детали : {st.Detail}");
                    Console.WriteLine($"  бэкап  : {(st.RevertAvailable ? TweakRunner.BackupPathFor(item.Id) : "нет")}");
                    return 0;
            }
        }

        private static int Help()
        {
            Console.WriteLine("GAMEOPTIMIZ 4.0 — режим командной строки");
            Console.WriteLine();
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /scan                 диагностика железа и системы");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /doctor               железо + применимость твиков + HealthScore");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /report               то же + сохранить отчёт в файл");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /apply safe           безопасный профиль");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /apply balanced       рекомендуемый (по умолчанию)");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /apply extreme        максимальный, VBS/HVCI off");
            Console.WriteLine("       ключи: --no-restore-point  --latency");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /revert               откатить последний профиль");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /latency on|off       удержание таймера 0.5 мс");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /purge                очистить список ожидания памяти");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /backup               только точка восстановления и бэкап");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /monitor              живые CPU/GPU/RAM/температура + топ процессов");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /bench <csv> [игра] [метка]   разбор замера CapFrameX/PresentMon");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /session              состояние чистого игрового режима");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /tweaks [фильтр]      все твики со состоянием и уровнем риска");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /tweak <id> on|off    включить или откатить один конкретный твик");
            Console.WriteLine("  GAMEOPTIMIZ1.0.exe /tweak <id> status    подробности по одному твику");
            return 0;
        }
    }
}
