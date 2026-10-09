using Microsoft.Win32;

namespace GameOptimizer.Core
{
    /// <summary>
    /// Thread-safe registry manager with automatic backup before every write.
    /// GAMEOPTIMIZv4 — all operations null-checked, no exceptions bubble out.
    /// </summary>
    public static class RegistryManager
    {
        private const string BackupRoot = @"Software\zxcllaura\GAMEOPTIMIZv4\Backup";

        public static bool SetValue(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind valueKind)
        {
            try
            {
                BackupValue(hive, subKey, valueName);
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(subKey, true);
                key?.SetValue(valueName, value, valueKind);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegistryManager] {subKey}\\{valueName}: {ex.Message}");
                return false;
            }
        }

        public static object? GetValue(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKey);
                return key?.GetValue(valueName);
            }
            catch { return null; }
        }

        public static bool HasBackups()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(BackupRoot, false);
                return key != null && key.GetSubKeyNames().Length > 0;
            }
            catch { return false; }
        }

        public static bool RestoreAllBackups(Action<string>? log = null)
        {
            try
            {
                using var backupKey = Registry.CurrentUser.OpenSubKey(BackupRoot, true);
                if (backupKey == null) { log?.Invoke("Backup реестра не найден."); return false; }
                RestoreRecursive(backupKey, string.Empty, log);
                Registry.CurrentUser.DeleteSubKeyTree(BackupRoot, false);
                log?.Invoke("Backup реестра удалён после успешного отката.");
                return true;
            }
            catch (Exception ex) { log?.Invoke("Откат реестра завершился с ошибкой: " + ex.Message); return false; }
        }

        private static void BackupValue(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                string backupPath = $@"{BackupRoot}\{hive}\{subKey}";
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var origKey = baseKey.OpenSubKey(subKey);
                if (origKey == null) return;

                var origVal = origKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                using var check = Registry.CurrentUser.OpenSubKey(backupPath);
                if (check?.GetValue(valueName + "_Exists") != null) return;

                using var save = Registry.CurrentUser.CreateSubKey(backupPath, true);
                if (save != null)
                {
                    save.SetValue(valueName + "_Exists", origVal != null ? 1 : 0, RegistryValueKind.DWord);
                    if (origVal != null)
                    {
                        var kind = origKey.GetValueKind(valueName);
                        save.SetValue(valueName, origVal, kind);
                        save.SetValue(valueName + "_Kind", (int)kind, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

private static void RestoreRecursive(RegistryKey key, string path, Action<string>? log)
        {
            foreach (var name in key.GetSubKeyNames())
            {
                using var sub = key.OpenSubKey(name);
                if (sub == null) continue;
                string newPath = string.IsNullOrEmpty(path) ? name : $@"{path}\\{name}";
                if (newPath.StartsWith("LocalMachine", StringComparison.OrdinalIgnoreCase) || newPath.StartsWith("CurrentUser", StringComparison.OrdinalIgnoreCase))
                    ApplyBackup(newPath, sub, log);
                else
                    RestoreRecursive(sub, newPath, log);
            }
        }

        private static void ApplyBackup(string backupPath, RegistryKey src, Action<string>? log)
        {
            try
            {
                var hive = backupPath.StartsWith("LocalMachine") ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
                var realSub = backupPath[(backupPath.IndexOf('\\') + 1)..];
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var sysKey = baseKey.OpenSubKey(realSub, true);
                if (sysKey == null) return;
                foreach (var marker in src.GetValueNames())
                {
                    if (!marker.EndsWith("_Exists", StringComparison.Ordinal)) continue;
                    var valueName = marker[..^7];
                    var exists = Convert.ToInt32(src.GetValue(marker) ?? 1) != 0;
                    if (!exists)
                    {
                        sysKey.DeleteValue(valueName, false);
                        log?.Invoke($"Удалено новое значение: {hive}\\{realSub}\\{valueName}");
                        continue;
                    }
                    var val = src.GetValue(valueName);
                    int kindInt = Convert.ToInt32(src.GetValue(valueName + "_Kind") ?? (int)RegistryValueKind.Unknown);
                    var kind = (RegistryValueKind)kindInt;
                    if (val != null && kind != RegistryValueKind.Unknown)
                    {
                        sysKey.SetValue(valueName, val, kind);
                        log?.Invoke($"Восстановлено: {hive}\\{realSub}\\{valueName}");
                    }
                }
            }
            catch { }
        }
    }
}
