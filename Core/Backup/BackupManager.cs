using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Backup
{
    // ─────────────────────────────────────────────────────────────────────
    //  Registry helper — every write in v6 goes through here so that the
    //  previous value can always be captured before it is overwritten.
    // ─────────────────────────────────────────────────────────────────────
    public static class Reg
    {
        public static RegistryKey Open(RegistryHive hive, string subKey, bool writable)
        {
            try
            {
                var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                return writable ? baseKey.CreateSubKey(subKey, true) : baseKey.OpenSubKey(subKey);
            }
            catch { return null; }
        }

        public static object Read(RegistryHive hive, string subKey, string name)
        {
            try
            {
                using var k = Open(hive, subKey, false);
                return k?.GetValue(name);
            }
            catch { return null; }
        }

        public static RegistryValueKind KindOf(RegistryHive hive, string subKey, string name)
        {
            try
            {
                using var k = Open(hive, subKey, false);
                if (k == null) return RegistryValueKind.Unknown;
                var names = k.GetValueNames();
                if (!names.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                    return RegistryValueKind.Unknown;
                return k.GetValueKind(name);
            }
            catch { return RegistryValueKind.Unknown; }
        }

        public static bool Write(RegistryHive hive, string subKey, string name, object value, RegistryValueKind kind)
        {
            try
            {
                using var k = Open(hive, subKey, true);
                if (k == null) return false;
                k.SetValue(name, value, kind);
                return true;
            }
            catch { return false; }
        }

        public static bool Delete(RegistryHive hive, string subKey, string name)
        {
            try
            {
                using var k = Open(hive, subKey, true);
                if (k == null) return false;
                k.DeleteValue(name, false);
                return true;
            }
            catch { return false; }
        }
    }

    public static class ValueCodec
    {
        public static string Encode(object value, RegistryValueKind kind)
        {
            if (value == null) return string.Empty;
            try
            {
                switch (kind)
                {
                    case RegistryValueKind.DWord:      return ((int)value).ToString(CultureInfo.InvariantCulture);
                    case RegistryValueKind.QWord:      return ((long)value).ToString(CultureInfo.InvariantCulture);
                    case RegistryValueKind.Binary:     return Convert.ToBase64String((byte[])value);
                    case RegistryValueKind.MultiString: return string.Join("\u0001", (string[])value);
                    default:                           return value.ToString() ?? string.Empty;
                }
            }
            catch { return value.ToString() ?? string.Empty; }
        }

        public static object Decode(string raw, RegistryValueKind kind)
        {
            try
            {
                switch (kind)
                {
                    case RegistryValueKind.DWord:  return int.Parse(raw, CultureInfo.InvariantCulture);
                    case RegistryValueKind.QWord:  return long.Parse(raw, CultureInfo.InvariantCulture);
                    case RegistryValueKind.Binary: return Convert.FromBase64String(raw);
                    case RegistryValueKind.MultiString: return raw.Split('\u0001');
                    default:                       return raw;
                }
            }
            catch { return raw; }
        }

        public static RegistryValueKind ToKind(string name)
            => Enum.TryParse(name, out RegistryValueKind k) ? k : RegistryValueKind.String;
    }

    // ── snapshot model ───────────────────────────────────────────────────
    public sealed class RegRecord
    {
        public string Hive   { get; set; } = string.Empty;
        public string SubKey { get; set; } = string.Empty;
        public string Name   { get; set; } = string.Empty;
        public bool   Existed { get; set; }
        public string Kind   { get; set; } = "Unknown";
        public string Value  { get; set; } = string.Empty;
    }

    public sealed class ServiceRecord
    {
        public string Name      { get; set; } = string.Empty;
        public string StartType { get; set; } = string.Empty;
        public bool   WasRunning { get; set; }
    }

    public sealed class FileRecord
    {
        public string Path    { get; set; } = string.Empty;
        public bool   Existed { get; set; }
        public string Base64  { get; set; } = string.Empty;
    }

    public sealed class BackupSnapshot
    {
        public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("o");
        public string Machine    { get; set; } = Environment.MachineName;
        public string Profile    { get; set; } = string.Empty;
        public string AppVersion { get; set; } = "v1.0.0";
        public List<RegRecord>     Registry { get; set; } = new();
        public List<ServiceRecord> Services { get; set; } = new();
        public List<FileRecord>    Files    { get; set; } = new();
        /// <summary>bcdedit keys captured before change; empty string = value did not exist.</summary>
        public Dictionary<string, string> Boot { get; set; } = new();
        /// <summary>Free-form values a tweak needs to undo itself (previous power plan, DNS, ...).</summary>
        public Dictionary<string, string> Meta { get; set; } = new();
    }

    // ── the backup engine ────────────────────────────────────────────────
    public static class BackupManager
    {
        public static string Root      => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GAMEOPTIMIZv7");
        public static string BackupDir => Path.Combine(Root, "backups");
        public static string LogDir    => Path.Combine(Root, "logs");

        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        public static BackupSnapshot Current { get; private set; } = new();

        public static void Begin(string profile)
        {
            Current = new BackupSnapshot { Profile = profile };
            Directory.CreateDirectory(BackupDir);
            Directory.CreateDirectory(LogDir);
        }

        /// <summary>Capture the current value (or its absence) exactly once.</summary>
        public static void TrackRegistry(RegistryHive hive, string subKey, string name)
        {
            foreach (var r in Current.Registry)
                if (r.Hive == hive.ToString()
                    && string.Equals(r.SubKey, subKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase))
                    return;

            var val  = Reg.Read(hive, subKey, name);
            var kind = Reg.KindOf(hive, subKey, name);

            Current.Registry.Add(new RegRecord
            {
                Hive    = hive.ToString(),
                SubKey  = subKey,
                Name    = name,
                Existed = val != null,
                Kind    = kind.ToString(),
                Value   = val == null ? string.Empty : ValueCodec.Encode(val, kind)
            });
        }

        /// <summary>Capture + write in one step. Returns false when the write failed.</summary>
        public static bool WriteTracked(RegistryHive hive, string subKey, string name, object value, RegistryValueKind kind)
        {
            TrackRegistry(hive, subKey, name);
            return Reg.Write(hive, subKey, name, value, kind);
        }

        public static void TrackService(string name)
        {
            if (Current.Services.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))) return;
            var (type, running) = ServiceQuery(name);
            if (type == null) return;
            Current.Services.Add(new ServiceRecord { Name = name, StartType = type, WasRunning = running });
        }

        /// <summary>Reads a bcdedit value from the active boot entry and remembers it.</summary>
        public static void TrackBcd(string key)
        {
            if (Current.Boot.ContainsKey(key)) return;
            Current.Boot[key] = ReadBcd(key);
        }

        private static string ReadBcd(string key)
        {
            var (code, output) = ProcessRunner.Run("bcdedit.exe", "/enum {current}");
            if (code != 0 || string.IsNullOrWhiteSpace(output)) return string.Empty;
            foreach (var line in output.Split('\n'))
            {
                var t = line.Trim();
                if (!t.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
                var parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return parts[1];
            }
            return string.Empty;
        }

        public static void TrackFile(string path)
        {
            if (Current.Files.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase))) return;
            bool exists = File.Exists(path);
            Current.Files.Add(new FileRecord
            {
                Path    = path,
                Existed = exists,
                Base64  = exists ? Convert.ToBase64String(File.ReadAllBytes(path)) : string.Empty
            });
        }

        public static string Save()
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string name  = $"backup-{stamp}-{Current.Profile}.json";
            string full  = SaveAs(name);
            try { File.WriteAllText(Path.Combine(BackupDir, "latest.json"), JsonSerializer.Serialize(Current, Json)); } catch { }
            return full;
        }

        /// <summary>Writes the pending snapshot under an explicit file name.</summary>
        public static string SaveAs(string fileName)
        {
            Directory.CreateDirectory(BackupDir);
            string full = Path.Combine(BackupDir, fileName);
            File.WriteAllText(full, JsonSerializer.Serialize(Current, Json));
            return full;
        }

        /// <summary>Newest full-profile pass: which profile, when, and where its backup is.</summary>
        public static (string Profile, DateTime At, string Path) LastProfile()
        {
            try
            {
                foreach (var dir in EnumerateBackupDirs())
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (var file in Directory.GetFiles(dir, "backup-*.json").OrderByDescending(f => f))
                    {
                        var snap = Load(file);
                        if (snap == null) continue;
                        DateTime at = DateTime.TryParse(snap.CreatedUtc, out var parsed)
                            ? parsed.ToLocalTime() : DateTime.MinValue;
                        return (snap.Profile ?? "", at, file);
                    }
                }
            }
            catch { }
            return ("", DateTime.MinValue, "");
        }

        public static string LatestBackupPath()
        {
            // v7 keeps its own chain, but still sees backups written by v6 so an
            // earlier optimisation pass stays revertable after the upgrade.
            foreach (var dir in EnumerateBackupDirs())
            {
                try
                {
                    if (!Directory.Exists(dir)) continue;
                    var files = Directory.GetFiles(dir, "backup-*.json").OrderByDescending(f => f).ToArray();
                    if (files.Length > 0) return files[0];
                }
                catch { }
            }
            return null;
        }

        private static IEnumerable<string> EnumerateBackupDirs()
        {
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            yield return BackupDir;
            yield return Path.Combine(common, "GAMEOPTIMIZv6", "backups");
        }

        public static BackupSnapshot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<BackupSnapshot>(File.ReadAllText(path), Json); }
            catch { return null; }
        }

        /// <summary>Restore everything captured in a snapshot. Returns the number of restored items.</summary>
        public static int Restore(string path, Action<string> log)
        {
            var snap = Load(path);
            if (snap == null) { log?.Invoke($"[RESTORE] Не удалось прочитать бэкап: {path}"); return 0; }
            int done = 0;

            log?.Invoke($"[RESTORE] Бэкап от {snap.CreatedUtc} (профиль {snap.Profile})");

            foreach (var f in snap.Files)
            {
                try
                {
                    if (f.Existed)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(f.Path));
                        File.WriteAllBytes(f.Path, Convert.FromBase64String(f.Base64));
                    }
                    else if (File.Exists(f.Path)) File.Delete(f.Path);
                    done++; log?.Invoke($"   файл  {f.Path}");
                }
                catch (Exception ex) { log?.Invoke($"   [ОШИБКА] файл {f.Path}: {ex.Message}"); }
            }

            foreach (var r in snap.Registry)
            {
                try
                {
                    var hive = Enum.Parse<RegistryHive>(r.Hive);
                    var kind = ValueCodec.ToKind(r.Kind);
                    if (r.Existed && kind != RegistryValueKind.Unknown)
                    {
                        Reg.Write(hive, r.SubKey, r.Name, ValueCodec.Decode(r.Value, kind), kind);
                        log?.Invoke($"   реестр {r.Hive}\\{r.SubKey} :: {r.Name} = {r.Value}");
                    }
                    else
                    {
                        Reg.Delete(hive, r.SubKey, r.Name);
                        log?.Invoke($"   реестр {r.Hive}\\{r.SubKey} :: {r.Name} удалён");
                    }
                    done++;
                }
                catch (Exception ex) { log?.Invoke($"   [ОШИБКА] реестр {r.Name}: {ex.Message}"); }
            }

            foreach (var s in snap.Services)
            {
                try
                {
                    string startArg = s.StartType switch
                    {
                        "Automatic" => "auto",
                        "Manual"    => "demand",
                        "Disabled"  => "disabled",
                        "Boot"      => "boot",
                        "System"    => "system",
                        _           => null
                    };
                    if (startArg != null) ProcessRunner.Run("sc.exe", $"config {s.Name} start= {startArg}");
                    if (s.WasRunning)     ProcessRunner.Run("sc.exe", $"start {s.Name}");
                    log?.Invoke($"   служба {s.Name} -> {s.StartType}");
                    done++;
                }
                catch (Exception ex) { log?.Invoke($"   [ОШИБКА] служба {s.Name}: {ex.Message}"); }
            }

            foreach (var kv in snap.Boot)
            {
                try
                {
                    if (string.IsNullOrEmpty(kv.Value))
                    {
                        ProcessRunner.Run("bcdedit.exe", $"/deletevalue {kv.Key}");
                        log?.Invoke($"   загрузчик {kv.Key} -> удалено (как было)");
                    }
                    else
                    {
                        ProcessRunner.Run("bcdedit.exe", $"/set {kv.Key} {kv.Value}");
                        log?.Invoke($"   загрузчик {kv.Key} -> {kv.Value}");
                    }
                    done++;
                }
                catch (Exception ex) { log?.Invoke($"   [ОШИБКА] загрузчик {kv.Key}: {ex.Message}"); }
            }

            log?.Invoke($"[RESTORE] восстановлено элементов: {done}");
            return done;
        }

        private static (string type, bool running) ServiceQuery(string name)
        {
            var (code, output) = ProcessRunner.Run("sc.exe", $"qc {name}");
            if (code != 0 || string.IsNullOrWhiteSpace(output)) return (null, false);
            string type = null;
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("START_TYPE", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.Contains("AUTO_START",     StringComparison.OrdinalIgnoreCase)) type = "Automatic";
                    else if (line.Contains("DEMAND_START", StringComparison.OrdinalIgnoreCase)) type = "Manual";
                    else if (line.Contains("DISABLED",     StringComparison.OrdinalIgnoreCase)) type = "Disabled";
                    else if (line.Contains("BOOT_START",   StringComparison.OrdinalIgnoreCase)) type = "Boot";
                    else if (line.Contains("SYSTEM_START", StringComparison.OrdinalIgnoreCase)) type = "System";
                }
            }
            var q = ProcessRunner.Run("sc.exe", $"query {name}");
            bool running = q.output != null && q.output.Contains("RUNNING");
            return (type, running);
        }
    }
}
