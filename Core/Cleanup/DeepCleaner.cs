using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameOptimizer.Utils;
using Microsoft.VisualBasic.FileIO;

namespace GameOptimizer.Core.Cleanup
{
    /// <summary>Одна цель чистки: что это, где лежит, сколько займёт, насколько безопасно.</summary>
    public sealed class CleanTarget
    {
        public string Key;
        public string Title;
        public string Desc;          // простым языком: что это и можно ли удалять
        public bool   NeedsConfirm;  // true = спрашивать отдельно (например Windows.old)
        public bool   DefaultOn = true;
        public Func<long> Measure;   // байты, которые освободятся
        public Action<Action<string>> Clean;
        public long LastSizeBytes;
    }

    public sealed record DeepCleanResult(long FreedBytes, int Targets, IReadOnlyList<string> Notes);

    /// <summary>
    /// 1.0: глубокая, но безопасная чистка. Всё — методы, которые поддерживает Microsoft
    /// (Temp, кэши, SoftwareDistribution, DISM StartComponentCleanup, корзина, DNS-кэш).
    /// Сначала предпросмотр (Measure), потом удаление (Clean). Where possible — в корзину.
    /// </summary>
    public static class DeepCleaner
    {
        private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string Win    => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        public static List<CleanTarget> Targets()
        {
            var list = new List<CleanTarget>
            {
                Dir("temp-user", "Временные файлы (пользователь)",
                    "Мусор программ из папки TEMP. Удаляется безопасно — Windows пересоздаёт по мере надобности.",
                    Path.GetTempPath()),
                Dir("temp-win", "Временные файлы (система)",
                    "Системная папка Windows\\Temp — остатки установок и обновлений.",
                    Path.Combine(Win, "Temp")),
                Dir("prefetch", "Prefetch",
                    "Данные предзагрузки программ. Их очистка не ускоряет Windows и временно замедляет первые запуски — используй только для диагностики.",
                    Path.Combine(Win, "Prefetch"), defaultOn: false),
                Pattern("thumbs", "Кэш эскизов",
                    "Миниатюры картинок/видео в проводнике. Пересоздаются автоматически.",
                    new[] { Path.Combine(Local, "Microsoft", "Windows", "Explorer") }, "thumbcache_*.db"),
                Dir("gpu-cache", "Шейдерный кэш DirectX",
                    "После очистки играм придётся компилировать шейдеры заново — возможны временные микрофризы. Нужен только для диагностики повреждённого кэша.",
                    Path.Combine(Local, "D3DSCache"), defaultOn: false),
                Dir("wu-cache", "Кэш Windows Update",
                    "Скачанные установщики обновлений (SoftwareDistribution\\Download). Безопасно — докачается при надобности.",
                    Path.Combine(Win, "SoftwareDistribution", "Download")),
                Dir("delivery", "Кэш Delivery Optimization",
                    "Файлы доставки обновлений P2P. Можно чистить.",
                    Path.Combine(Win, "SoftwareDistribution", "DeliveryOptimization")),
                Dir("crashdumps", "Дампы сбоев",
                    "Файлы аварийных дампов (.dmp). Нужны только для отладки.",
                    Path.Combine(Local, "CrashDumps")),
                Dir("wer", "Отчёты об ошибках Windows",
                    "Старые отчёты WER для диагностики сбоев. Личные документы и настройки не затрагиваются.",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "WER")),
                Dir("logs", "Старые логи Windows",
                    "Старые журналы установки и диагностики Windows. Текущие системные файлы не удаляются.",
                    Path.Combine(Win, "Logs")),
                Dir("inetc", "Кэш Windows/Internet",
                    "Кэш компонентов Windows. Профили браузеров и сохранённые данные не трогаются.",
                    Path.Combine(Local, "Microsoft", "Windows", "INetCache")),
                Dir("cbstemp", "Временные файлы CBS",
                    "Временные файлы обслуживания компонентов Windows.",
                    Path.Combine(Win, "CbsTemp")),
                Dir("winbt", "Остатки обновления Windows",
                    "Остатки установщика прошлой версии Windows. Удаляются только если папка существует.",
                    Path.Combine(Path.GetPathRoot(Win) ?? "C:\\", "$WINDOWS.~BT")),
                Pattern("etl", "Старые трассировки ETL",
                    "Временные трассировки диагностики. Активные файлы будут пропущены.",
                    new[] { Path.GetTempPath(), Path.Combine(Win, "Temp") }, "*.etl*"),
                BrowserCaches(),
            };

            // DNS-кэш — размер не измеряем, действие лёгкое
            list.Add(new CleanTarget
            {
                Key = "dns", Title = "Сбросить кэш DNS",
                Desc = "Чистит таблицу сопоставления доменов и адресов — иногда чинит «не грузится сайт/сервер».",
                Measure = () => 0,
                Clean = log => { ProcessRunner.Run("ipconfig.exe", "/flushdns"); log?.Invoke("DNS-кэш сброшен"); }
            });

            // Корзина
            list.Add(new CleanTarget
            {
                Key = "recycle", Title = "Очистить корзину",
                Desc = "Удаляет содержимое корзины окончательно. Спрашивает отдельно перед удалением.",
                NeedsConfirm = true,
                Measure = () => 0,
                Clean = log =>
                {
                    ProcessRunner.PowerShell("Clear-RecycleBin -Force -ErrorAction SilentlyContinue; 'OK'", 60000);
                    log?.Invoke("Корзина очищена");
                }
            });

            // WinSxS через DISM — безопасный официальный способ ужать хранилище компонентов
            list.Add(new CleanTarget
            {
                Key = "winsxs", Title = "Ужать хранилище компонентов (WinSxS, DISM)",
                Desc = "Официальная чистка WinSxS: убирает старые версии обновлений. Безопасно, но идёт несколько минут. " +
                       "После — откатить уже установленные обновления будет нельзя.",
                NeedsConfirm = true, DefaultOn = false,
                Measure = () => 0,
                Clean = log =>
                {
                    log?.Invoke("DISM: ужимаю WinSxS (может занять несколько минут)...");
                    var (code, _) = ProcessRunner.Run("dism.exe", "/online /Cleanup-Image /StartComponentCleanup", 600000);
                    log?.Invoke(code == 0 ? "WinSxS ужат" : $"DISM завершился с кодом {code}");
                }
            });

            // Windows.old — только с подтверждением
            string winOld = Path.Combine(Path.GetPathRoot(Win) ?? "C:\\", "Windows.old");
            if (Directory.Exists(winOld))
                list.Add(new CleanTarget
                {
                    Key = "winold", Title = "Удалить Windows.old",
                    Desc = "Папка от прошлой версии Windows (десятки ГБ). Удаление отменяет возможность отката к старой сборке.",
                    NeedsConfirm = true, DefaultOn = false,
                    Measure = () => DirSize(winOld),
                    Clean = log =>
                    {
                        ProcessRunner.Run("cmd.exe", $"/c takeown /F \"{winOld}\" /R /A /D Y", 120000);
                        ProcessRunner.Run("cmd.exe", $"/c icacls \"{winOld}\" /grant administrators:F /T /C", 120000);
                        try
                        {
                            FileSystem.DeleteDirectory(winOld, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            log?.Invoke("Windows.old отправлен в корзину");
                        }
                        catch (Exception ex)
                        {
                            log?.Invoke($"Windows.old не удалось отправить в корзину: {ex.Message}");
                        }
                    }
                });

            return list;
        }

        /// <summary>Предпросмотр: заполняет LastSizeBytes и возвращает сумму.</summary>
        public static long Preview(List<CleanTarget> targets, Action<string> log = null)
        {
            long total = 0;
            foreach (var t in targets)
            {
                try { t.LastSizeBytes = t.Measure?.Invoke() ?? 0; } catch { t.LastSizeBytes = 0; }
                total += t.LastSizeBytes;
                if (t.LastSizeBytes > 0) log?.Invoke($"{t.Title}: ~{Human(t.LastSizeBytes)}");
            }
            log?.Invoke($"ИТОГО освободится примерно: ~{Human(total)}");
            return total;
        }

        public static DeepCleanResult Clean(IEnumerable<CleanTarget> targets, Action<string> log = null)
        {
            long freed = 0; int n = 0; var notes = new List<string>();
            foreach (var t in targets)
            {
                try
                {
                    long before = t.LastSizeBytes;
                    t.Clean?.Invoke(log);
                    freed += before; n++;
                }
                catch (Exception ex) { notes.Add($"{t.Title}: {ex.Message}"); }
            }
            log?.Invoke($"Готово. Освобождено примерно ~{Human(freed)} по {n} целям.");
            return new DeepCleanResult(freed, n, notes);
        }

        // ── helpers ──────────────────────────────────────────────────────
        private static CleanTarget Pattern(string key, string title, string desc, string[] roots, string pattern) => new()
        {
            Key = key, Title = title, Desc = desc,
            Measure = () => roots.Sum(root => PatternSize(root, pattern)),
            Clean = log =>
            {
                int deleted = 0;
                foreach (var root in roots)
                foreach (var file in SafeEnumeratePattern(root, pattern))
                {
                    try
                    {
                        FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        deleted++;
                    }
                    catch { }
                }
                log?.Invoke($"{title}: отправлено в корзину файлов {deleted}");
            }
        };

        private static CleanTarget BrowserCaches()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var roots = new[]
            {
                Path.Combine(Local, "Google", "Chrome", "User Data"),
                Path.Combine(Local, "Microsoft", "Edge", "User Data"),
                Path.Combine(user, "AppData", "Roaming", "Mozilla", "Firefox", "Profiles"),
                Path.Combine(Local, "Opera Software", "Opera Stable")
            };
            var cacheNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Cache", "Code Cache", "GPUCache" };
            return new CleanTarget
            {
                Key = "browsers", Title = "Кэш браузеров",
                Desc = "Чистит только Cache/Code Cache/GPUCache. Service Worker, cookies, пароли и профили не трогаются.",
                Measure = () => roots.Sum(root => BrowserCacheSize(root, cacheNames)),
                Clean = log =>
                {
                    int deleted = 0;
                    foreach (var root in roots)
                    foreach (var dir in SafeEnumerateDirectories(root))
                    {
                        if (!cacheNames.Contains(Path.GetFileName(dir))) continue;
                        foreach (var file in SafeEnumerate(dir))
                        {
                            try
                            {
                                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                                deleted++;
                            }
                            catch { }
                        }
                    }
                    log?.Invoke($"Кэш браузеров: отправлено в корзину файлов {deleted}");
                }
            };
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string root)
        {
            if (!Directory.Exists(root)) return Array.Empty<string>();
            try { return Directory.EnumerateDirectories(root, "*", System.IO.SearchOption.AllDirectories); }
            catch { return Array.Empty<string>(); }
        }

        private static IEnumerable<string> SafeEnumeratePattern(string root, string pattern)
        {
            if (!Directory.Exists(root)) return Array.Empty<string>();
            try { return Directory.EnumerateFiles(root, pattern, System.IO.SearchOption.AllDirectories); }
            catch { return Array.Empty<string>(); }
        }

        private static long PatternSize(string root, string pattern) =>
            SafeEnumeratePattern(root, pattern).Sum(FileLength);

        private static long BrowserCacheSize(string root, HashSet<string> cacheNames)
        {
            return SafeEnumerateDirectories(root)
                .Where(dir => cacheNames.Contains(Path.GetFileName(dir)))
                .SelectMany(SafeEnumerate)
                .Sum(FileLength);
        }

        private static long FileLength(string file)
        {
            try { return new FileInfo(file).Length; } catch { return 0; }
        }

        private static CleanTarget Dir(string key, string title, string desc, string path, bool defaultOn = true) => new()
        {
            Key = key, Title = title, Desc = desc, DefaultOn = defaultOn,
            Measure = () => DirSize(path),
            Clean = log =>
            {
                if (!Directory.Exists(path)) return;
                int f = 0;
                foreach (var file in SafeEnumerate(path))
                {
                    try
                    {
                        FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        f++;
                    }
                    catch { }
                }
                log?.Invoke($"{title}: удалено файлов {f}");
            }
        };

        private static IEnumerable<string> SafeEnumerate(string root)
        {
            IEnumerable<string> files = Array.Empty<string>();
            try { files = Directory.EnumerateFiles(root, "*", System.IO.SearchOption.AllDirectories); } catch { }
            return files;
        }

        private static long DirSize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long sum = 0;
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", System.IO.SearchOption.AllDirectories))
                {
                    try { sum += new FileInfo(f).Length; } catch { }
                }
            }
            catch { }
            return sum;
        }

        public static string Human(long bytes)
        {
            string[] u = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double v = bytes; int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.#} {u[i]}";
        }
    }
}
