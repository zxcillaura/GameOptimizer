using System;
using System.IO;

namespace GameOptimizer.Core
{
    public static class SystemCleaner
    {
        private static long _freed = 0;
        private static int  _files = 0;
        private static int  _dirs  = 0;

        public static (long bytes, int files, int dirs) RunDeepCleanup(Action<string> log)
        {
            _freed = _files = _dirs = 0;
            log("Starting deep system cleanup...");

            string local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string windows = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";

            var targets = new[]
            {
                Path.GetTempPath(),
                Path.Combine(windows, "Temp"),
                // Prefetch and Windows Update caches are managed by Windows and are not deleted.
                @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue",
                Path.Combine(local, "D3DSCache"),
                Path.Combine(local, "DXCache"),
                Path.Combine(local, "NVIDIA", "DXCache"),
                Path.Combine(local, "NVIDIA", "GLCache"),
                Path.Combine(local, "AMD",    "DXCache"),
                Path.Combine(local, "AMD",    "GLCache"),
                // Browser profiles and Explorer data are left untouched to avoid disrupting user sessions.
                Path.Combine(local, "VALORANT", "Saved", "Logs"),
                Path.Combine(local, "Counter-Strike Global Offensive", "game", "csgo", "logs"),
                Path.Combine(local, "Temp"),
            };

            foreach (var dir in targets)
            {
                if (!Directory.Exists(dir)) continue;
                log($"  Cleaning: {Path.GetFileName(dir)}...");
                CleanDir(dir, log);
            }

            double mb = Math.Round((double)_freed / 1024 / 1024, 2);
            double gb = Math.Round(mb / 1024, 2);
            log("=== CLEANUP COMPLETE ===");
            log($"Files deleted: {_files:N0}  |  Folders: {_dirs:N0}");
            log(gb >= 1 ? $"Freed: {gb:F2} GB" : $"Freed: {mb:F2} MB");
            log("Reboot recommended for full effect.");
            return (_freed, _files, _dirs);
        }

        public static (long bytes, int files, int dirs) QuickCleanup(Action<string> log)
        {
            _freed = _files = _dirs = 0;
            log("Quick cleanup of temp files...");
            string windows = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
            string[] quick =
            {
                Path.GetTempPath(),
                Path.Combine(windows, "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
            };
            foreach (var p in quick)
                if (Directory.Exists(p)) CleanDir(p, log);

            double mb = Math.Round((double)_freed / 1024 / 1024, 2);
            log($"[OK] Quick cleanup done. Freed: {mb:F2} MB");
            return (_freed, _files, _dirs);
        }

        public static (long bytes, int files, int dirs) CleanShaderCache(Action<string> log)
        {
            _freed = _files = _dirs = 0;
            log("Cleaning shader cache...");
            string local  = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string[] paths =
            {
                Path.Combine(local,  "D3DSCache"),
                Path.Combine(local,  "DXCache"),
                Path.Combine(local,  "NVIDIA", "DXCache"),
                Path.Combine(local,  "NVIDIA", "GLCache"),
                Path.Combine(local,  "AMD",    "DXCache"),
                Path.Combine(local,  "AMD",    "GLCache"),
                Path.Combine(common, "NVIDIA Corporation", "NV_Cache")
            };
            foreach (var p in paths) if (Directory.Exists(p)) CleanDir(p, log);
            double mb = Math.Round((double)_freed / 1024 / 1024, 2);
            log($"[OK] Shader cache cleared. Freed: {mb:F2} MB");
            return (_freed, _files, _dirs);
        }

        /// <summary>Scans targets and returns total size without deleting anything.</summary>
        public static long ScanSize(Action<string> log)
        {
            log("Scanning system for junk files...");
            long total = 0;
            string local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string windows = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";

            var targets = new[]
            {
                Path.GetTempPath(),
                Path.Combine(windows, "Temp"),
                Path.Combine(local, "D3DSCache"),
                Path.Combine(local, "DXCache"),
                Path.Combine(local, "NVIDIA", "DXCache"),
                Path.Combine(local, "NVIDIA", "GLCache"),
                Path.Combine(local, "AMD", "DXCache"),
                Path.Combine(local, "AMD", "GLCache"),
                Path.Combine(local, "VALORANT", "Saved", "Logs"),
            };

            foreach (var dir in targets)
            {
                if (!Directory.Exists(dir)) continue;
                long size = GetDirSize(dir);
                total += size;
                if (size > 0)
                    log($"  {Path.GetFileName(dir)}: {size / 1024 / 1024:N0} MB");
            }

            double mb = Math.Round((double)total / 1024 / 1024, 2);
            double gb = Math.Round(mb / 1024, 2);
            string sizeStr = gb >= 1 ? $"{gb:F2} GB" : $"{mb:F2} MB";
            log($"[OK] Scan complete. Found: {sizeStr} of junk.");
            return total;
        }

        private static long GetDirSize(string path)
        {
            long size = 0;
            try
            {
                var di = new DirectoryInfo(path);
                foreach (var f in di.GetFiles("*.*", SearchOption.AllDirectories))
                    try { size += f.Length; } catch { }
            }
            catch { }
            return size;
        }

        private static void CleanDir(string path, Action<string>? log = null)
        {
            try
            {
                var di = new DirectoryInfo(path);
                if (!di.Exists) return;
                foreach (var f in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                {
                    try { long s = f.Length; f.Delete(); _freed += s; _files++; } catch { }
                }
                foreach (var d in di.GetDirectories())
                {
                    try { d.Delete(true); _dirs++; } catch { }
                }
            }
            catch { }
        }
    }
}
