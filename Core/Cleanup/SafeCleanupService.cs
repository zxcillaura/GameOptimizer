using System;
using System.Collections.Generic;
using System.IO;

namespace GameOptimizer.Core.Cleanup
{
    public sealed record CleanupResult(int Files, long Bytes, IReadOnlyList<string> Skipped);

    public static class SafeCleanupService
    {
        public static CleanupResult Run(Action<string>? log = null)
        {
            var roots = new List<string>
            {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "DXCache")
            };
            int files = 0; long bytes = 0; var skipped = new List<string>();
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
                    {
                        try { var length = new FileInfo(file).Length; File.Delete(file); files++; bytes += length; }
                        catch { skipped.Add(file); }
                    }
                    log?.Invoke($"Очищено: {root}");
                }
                catch (Exception ex) { skipped.Add($"{root}: {ex.Message}"); }
            }
            return new CleanupResult(files, bytes, skipped);
        }
    }
}
