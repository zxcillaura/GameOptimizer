using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GameOptimizer.Core.Backup;

namespace GameOptimizer.Core.Tweaks
{
    /// <summary>
    /// Remembers the last detected state of every tweak so the control centre can
    /// paint instantly on open, then refresh in the background. Reading 66 items
    /// live takes tens of seconds; the cache removes that wait from the UX.
    /// </summary>
    public static class StateCache
    {
        private sealed class Entry
        {
            public string Status { get; set; } = "Unknown";
            public string Detail { get; set; } = "";
            public bool Revert { get; set; }
        }

        private sealed class Payload
        {
            public string SavedUtc { get; set; } = "";
            public Dictionary<string, Entry> Items { get; set; } = new();
        }

        private static string FilePath => Path.Combine(BackupManager.Root, "state-cache.json");
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        public static Dictionary<string, TweakState> Load(TimeSpan maxAge)
        {
            var result = new Dictionary<string, TweakState>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return result;
                var payload = JsonSerializer.Deserialize<Payload>(File.ReadAllText(FilePath), Json);
                if (payload == null) return result;
                if (!DateTime.TryParse(payload.SavedUtc, out var saved)) return result;
                if (DateTime.UtcNow - saved > maxAge) return result;

                foreach (var kv in payload.Items)
                    result[kv.Key] = new TweakState
                    {
                        Status = Enum.TryParse<TweakStatus>(kv.Value.Status, out var st) ? st : TweakStatus.Unknown,
                        Detail = kv.Value.Detail,
                        RevertAvailable = kv.Value.Revert
                    };
            }
            catch { }
            return result;
        }

        public static void Save(IEnumerable<KeyValuePair<string, TweakState>> states)
        {
            try
            {
                var payload = new Payload { SavedUtc = DateTime.UtcNow.ToString("o") };
                foreach (var kv in states)
                    payload.Items[kv.Key] = new Entry
                    {
                        Status = kv.Value.Status.ToString(),
                        Detail = kv.Value.Detail ?? "",
                        Revert = kv.Value.RevertAvailable
                    };

                Directory.CreateDirectory(BackupManager.Root);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(payload, Json));
            }
            catch { }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
