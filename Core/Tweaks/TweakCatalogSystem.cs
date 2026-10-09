using System;
using System.Collections.Generic;
using System.Linq;
using GameOptimizer.Core.Backup;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Tweaks
{
    /// <summary>
    /// Services and scheduled tasks exposed as individual switches.
    /// State is read once in bulk (two PowerShell calls) and cached, so the
    /// list stays fast instead of spawning a process per row.
    /// </summary>
    public static class TweakCatalogSystem
    {
        private static readonly Dictionary<string, (string State, string Start)> Services = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> Tasks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object Gate = new();

        public static int ServiceCount { get { lock (Gate) return Services.Count; } }
        public static int TaskCount    { get { lock (Gate) return Tasks.Count; } }

        public static void Refresh(Action<string> log = null)
        {
            try
            {
                var (_, outp) = ProcessRunner.PowerShell(
                    "Get-CimInstance Win32_Service -ErrorAction SilentlyContinue | ForEach-Object { \"$($_.Name)|$($_.State)|$($_.StartMode)\" }", 120000);
                var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in (outp ?? "").Split('\n'))
                {
                    var p = line.Trim().Split('|');
                    if (p.Length >= 3 && p[0].Length > 0) map[p[0]] = (p[1], p[2]);
                }
                lock (Gate) { Services.Clear(); foreach (var kv in map) Services[kv.Key] = kv.Value; }
            }
            catch { }

            try
            {
                var (_, outp) = ProcessRunner.PowerShell(
                    "Get-ScheduledTask -ErrorAction SilentlyContinue | ForEach-Object { \"$($_.TaskPath)|$($_.TaskName)|$($_.State)\" }", 120000);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in (outp ?? "").Split('\n'))
                {
                    var p = line.Trim().Split('|');
                    if (p.Length >= 3 && p[1].Length > 0) map[p[0] + "|" + p[1]] = p[2];
                }
                lock (Gate) { Tasks.Clear(); foreach (var kv in map) Tasks[kv.Key] = kv.Value; }
            }
            catch { }

            log?.Invoke($"      прочитано служб: {ServiceCount}, задач планировщика: {TaskCount}");
        }

        // ── services ─────────────────────────────────────────────────────
        private sealed class ServiceItem : TweakItem
        {
            public string Service = "";

            public override TweakState Detect()
            {
                (string State, string Start) info;
                lock (Gate)
                {
                    if (!Services.TryGetValue(Service, out info))
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = "служба не найдена в системе" };
                }

                bool disabled = info.Start.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
                return new TweakState
                {
                    Status = disabled ? TweakStatus.Applied : TweakStatus.NotApplied,
                    Detail = $"запуск: {Translate(info.Start)}, состояние: {Translate(info.State)}"
                };
            }

            public override bool Apply(Action<string> log)
            {
                BackupManager.TrackService(Service);
                var (stopCode, _) = ProcessRunner.Run("sc.exe", $"stop {Service}");
                var (code, output) = ProcessRunner.Run("sc.exe", $"config {Service} start= disabled");

                string err = (output ?? "").Trim().Split('\n').FirstOrDefault()?.Trim() ?? "";
                if (code == 0)
                {
                    lock (Gate)
                    {
                        if (Services.ContainsKey(Service))
                            Services[Service] = ("Stopped", "Disabled");
                    }
                    log($"      {Service}: переведена в Disabled" +
                        (stopCode == 0 ? ", остановлена" : ", остановится после перезагрузки"));
                }
                else
                {
                    log($"      {Service}: не удалось отключить — {err}");
                }
                return code == 0;
            }

            private static string Translate(string s) => s switch
            {
                "Auto"           => "авто",
                "Auto (Delayed)" => "авто (с задержкой)",
                "Manual"         => "вручную",
                "Disabled" => "отключена",
                "Running"  => "работает",
                "Stopped"  => "остановлена",
                _          => s
            };
        }

        private static readonly (string Name, string Title, string Desc, TweakRisk Risk)[] ServiceTable =
        {
            ("DiagTrack",        "Телеметрия и отслеживание",     "Собирает и отправляет статистику использования Windows.", TweakRisk.Safe),
            ("dmwappushservice", "WAP Push-сообщения",            "Служба управления устройствами, для игр не нужна.",       TweakRisk.Safe),
            ("MapsBroker",       "Диспетчер загруженных карт",    "Обслуживает офлайн-карты.",                              TweakRisk.Safe),
            ("RetailDemo",       "Демонстрационный режим",        "Режим витрины магазина.",                                TweakRisk.Safe),
            ("Fax",              "Факс",                          "Приём и отправка факсов.",                               TweakRisk.Safe),
            ("RemoteRegistry",   "Удалённый реестр",              "Позволяет менять реестр по сети. Ещё и риск безопасности.", TweakRisk.Safe),
            ("lfsvc",            "Геолокация",                    "Определение местоположения устройства.",                 TweakRisk.Safe),
            ("WMPNetworkSvc",    "Общий доступ к медиаплееру",    "Сетевой обмен медиатекой.",                              TweakRisk.Safe),
            ("SSDPSRV",          "Обнаружение SSDP",              "Поиск UPnP-устройств в сети.",                           TweakRisk.Safe),
            ("upnphost",         "Хост UPnP",                     "Проброс портов для UPnP-устройств.",                     TweakRisk.Safe),
            ("PcaSvc",           "Помощник совместимости",        "Анализирует программы на совместимость в фоне.",         TweakRisk.Safe),
            ("XblAuthManager",   "Xbox Live — авторизация",       "Нужна только для игр из Microsoft Store.",               TweakRisk.Safe),
            ("XblGameSave",      "Xbox Live — сохранения",        "Облачные сохранения Xbox.",                              TweakRisk.Safe),
            ("XboxNetApiSvc",    "Xbox Live — сеть",              "Сетевая часть Xbox Live.",                               TweakRisk.Safe),
            ("XboxGipSvc",       "Xbox — аксессуары",             "Поддержка аксессуаров Xbox.",                            TweakRisk.Safe),
            ("WerSvc",           "Отчёты об ошибках",             "Пишет дампы при падениях. Минус: дампы бывают полезны.",   TweakRisk.Caution),
            ("TrkWks",           "Отслеживание ссылок",           "Следит за перемещением файлов на дисках.",               TweakRisk.Caution),
            ("WSearch",          "Индексация поиска Windows",     "Пишет индекс в фоне. Поиск станет медленнее.",            TweakRisk.Caution),
            ("SysMain",          "Superfetch (предзагрузка)",     "Кэширует часто используемые программы в память.",         TweakRisk.Caution),
            ("NlaSvc",           "Определение местоположения в сети", "Пытается понять, где ты (дом/публичная сеть). В играх — лишний фоновый трафик.", TweakRisk.Safe),
            ("Spooler",          "Диспетчер печати",              "Нужен только если печатаешь.",                           TweakRisk.Caution),
        };

        public static List<TweakItem> BuildServices()
        {
            var list = new List<TweakItem>();
            foreach (var s in ServiceTable)
                list.Add(new ServiceItem
                {
                    Id = "svc:" + s.Name, Group = "Службы",
                    Title = s.Title, Desc = s.Desc, Risk = s.Risk,
                    Note = s.Name, Service = s.Name
                });
            return list;
        }

        // ── scheduled tasks ──────────────────────────────────────────────
        private sealed class TaskItem : TweakItem
        {
            public string TaskPath = "";
            public string TaskName = "";

            public override TweakState Detect()
            {
                string state;
                lock (Gate)
                {
                    if (!Tasks.TryGetValue(TaskPath + "|" + TaskName, out state))
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = "задача не найдена" };
                }
                bool off = state.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
                return new TweakState
                {
                    Status = off ? TweakStatus.Applied : TweakStatus.NotApplied,
                    Detail = $"состояние задачи: {(off ? "отключена" : "включена")}"
                };
            }

            public override bool Apply(Action<string> log)
            {
                lock (Gate)
                {
                    if (Tasks.TryGetValue(TaskPath + "|" + TaskName, out string prev))
                        BackupManager.Current.Meta["task:" + Id] = prev;
                }

                var (_, outp) = ProcessRunner.PowerShell(
                    $"Disable-ScheduledTask -TaskPath '{TaskPath}' -TaskName '{TaskName}' -ErrorAction Stop | Out-Null; 'OK'", 60000);
                bool ok = outp != null && outp.Contains("OK");
                if (ok) lock (Gate) { Tasks[TaskPath + "|" + TaskName] = "Disabled"; }
                log($"      {TaskName}: {(ok ? "отключена" : "не удалось")}");
                return ok;
            }

            public override bool RevertExtra(BackupSnapshot snap, Action<string> log)
            {
                string want = "Ready";
                if (snap?.Meta != null && snap.Meta.TryGetValue("task:" + Id, out string prev) && !string.IsNullOrWhiteSpace(prev))
                    want = prev;

                string cmd = want.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
                    ? $"Disable-ScheduledTask -TaskPath '{TaskPath}' -TaskName '{TaskName}' -ErrorAction SilentlyContinue"
                    : $"Enable-ScheduledTask -TaskPath '{TaskPath}' -TaskName '{TaskName}' -ErrorAction SilentlyContinue";

                ProcessRunner.PowerShell(cmd + " | Out-Null", 60000);
                lock (Gate) { Tasks[TaskPath + "|" + TaskName] = want; }
                log($"      {TaskName} -> {want}");
                return true;
            }
        }

        private static readonly (string Path, string Name, string Title, TweakRisk Risk)[] TaskTable =
        {
            (@"\Microsoft\Windows\Application Experience\", "ProgramDataUpdater",
                "Сбор данных о программах (телеметрия)", TweakRisk.Safe),
            (@"\Microsoft\Windows\Application Experience\", "Microsoft Compatibility Appraiser",
                "Проверка совместимости приложений", TweakRisk.Safe),
            (@"\Microsoft\Windows\Application Experience\", "StartupAppTask",
                "Опрос автозагрузки при старте", TweakRisk.Safe),
            (@"\Microsoft\Windows\Customer Experience Improvement Program\", "Consolidator",
                "Программа улучшения качества ПО", TweakRisk.Safe),
            (@"\Microsoft\Windows\Customer Experience Improvement Program\", "UsbCeip",
                "Сбор данных об USB-устройствах", TweakRisk.Safe),
            (@"\Microsoft\Windows\DiskDiagnostic\", "Microsoft-Windows-DiskDiagnosticDataCollector",
                "Сбор диагностики диска", TweakRisk.Safe),
            (@"\Microsoft\Windows\Feedback\Siuf\", "DmClient",
                "Опросы обратной связи", TweakRisk.Safe),
            (@"\Microsoft\Windows\Feedback\Siuf\", "DmClientOnScenarioDownload",
                "Загрузка сценариев опросов", TweakRisk.Safe),
            (@"\Microsoft\Windows\Windows Error Reporting\", "QueueReporting",
                "Отправка отчётов об ошибках", TweakRisk.Caution),
            (@"\Microsoft\Windows\Maintenance\", "WinSAT",
                "Оценка производительности Windows", TweakRisk.Safe),
            (@"\Microsoft\Windows\Autochk\", "Proxy",
                "Проверка диска в фоне", TweakRisk.Caution),
            (@"\Microsoft\Windows\CloudExperienceHost\", "CreateObjectTask",
                "Фоновая подготовка интерфейса Windows", TweakRisk.Caution),
            (@"\Microsoft\Windows\TaskScheduler\", "MaintenanceOptimizer",
                "Плановое обслуживание Windows (фоновые обновления)", TweakRisk.Caution),
        };

        public static List<TweakItem> BuildTasks()
        {
            var list = new List<TweakItem>();
            foreach (var t in TaskTable)
                list.Add(new TaskItem
                {
                    Id = "task:" + t.Name, Group = "Задачи", Risk = t.Risk,
                    Title = t.Title, Desc = t.Name, Note = t.Path.Trim('\\').Replace("Microsoft\\Windows\\", ""),
                    TaskPath = t.Path, TaskName = t.Name
                });
            return list;
        }
    }
}
