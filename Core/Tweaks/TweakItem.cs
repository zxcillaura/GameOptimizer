using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Hardware;

namespace GameOptimizer.Core.Tweaks
{
    public enum TweakRisk { Safe, Caution, Risky }
    /// <summary>OneShot = a run-once action with no persistent state to read back.</summary>
    public enum TweakStatus { Applied, NotApplied, Partial, Unknown, Unsupported, OneShot }

    public sealed class TweakState
    {
        public TweakStatus Status { get; set; } = TweakStatus.Unknown;
        public string Detail { get; set; } = "";
        public bool RevertAvailable { get; set; }

        public string StatusText => Status switch
        {
            TweakStatus.Applied     => "включено",
            TweakStatus.NotApplied  => "выключено",
            TweakStatus.Partial     => "частично",
            TweakStatus.Unsupported => "недоступно",
            TweakStatus.OneShot     => "разовое",
            _                       => "неизвестно"
        };
    }

    /// <summary>One individually controllable change: its own state, apply and undo.</summary>
    public abstract class TweakItem
    {
        public string Id    { get; set; } = "";
        public string Group { get; set; } = "Прочее";
        public string Title { get; set; } = "";
        public string Desc  { get; set; } = "";
        public TweakRisk Risk { get; set; } = TweakRisk.Safe;
        public bool RequiresReboot { get; set; }
        /// <summary>Shown as a hint chip, e.g. "ломает BlueStacks".</summary>
        public string Note { get; set; } = "";

        /// <summary>
        /// 1.0 кросс-железность: возвращает null, если твик подходит железу,
        /// или человекочитаемую причину, почему он не для этой системы
        /// (например "только для NVIDIA" / "не нужно на ноутбуке").
        /// Неподходящие твики UI прячет или дизейблит с этой подписью.
        /// </summary>
        public Func<RigReport, string> NotForReason { get; set; }

        public string Applicability(RigReport rig)
        {
            if (NotForReason == null || rig == null) return null;
            try { return NotForReason(rig); } catch { return null; }
        }

        public abstract TweakState Detect();
        public abstract bool Apply(Action<string> log);
        /// <summary>Undo for anything a registry/file snapshot cannot capture.</summary>
        public virtual bool RevertExtra(BackupSnapshot snap, Action<string> log) => true;

        public TweakState DetectSafe()
        {
            TweakState s;
            try { s = Detect() ?? new TweakState(); }
            catch (Exception ex) { s = new TweakState { Status = TweakStatus.Unknown, Detail = ex.Message }; }
            s.RevertAvailable = TweakRunner.HasBackup(Id);
            return s;
        }
    }

    // ── declarative registry tweak ───────────────────────────────────────
    public sealed class RegWrite
    {
        public RegistryHive Hive = RegistryHive.LocalMachine;
        public string Sub  = "";
        public string Name = "";
        public object Value = 1;
        public RegistryValueKind Kind = RegistryValueKind.DWord;
        public string Label = "";
    }

    public sealed class RegistryTweak : TweakItem
    {
        public List<RegWrite> Writes = new();
        /// <summary>Optional rule over the current values (keyed by Label) for non-equality cases.</summary>
        public Func<Dictionary<string, object>, bool> Rule;

        public override TweakState Detect()
        {
            var cur = new Dictionary<string, object>();
            int missing = 0, match = 0;

            foreach (var w in Writes)
            {
                var v = Reg.Read(w.Hive, w.Sub, w.Name);
                string key = string.IsNullOrEmpty(w.Label) ? w.Name : w.Label;
                cur[key] = v;
                if (v == null) missing++;
                else if (Same(v, w.Value)) match++;
            }

            bool applied = Rule != null ? Rule(cur) : (missing == 0 && match == Writes.Count);
            var st = new TweakState
            {
                Status = applied ? TweakStatus.Applied
                       : (match > 0 ? TweakStatus.Partial
                       : (missing == Writes.Count ? TweakStatus.NotApplied : TweakStatus.NotApplied)),
                Detail = string.Join("  •  ", cur.Select(kv => $"{kv.Key} = {Fmt(kv.Value)}"))
            };
            if (st.Detail.Length > 200) st.Detail = st.Detail.Substring(0, 197) + "...";
            return st;
        }

        public override bool Apply(Action<string> log)
        {
            bool ok = true;
            foreach (var w in Writes)
                ok &= BackupManager.WriteTracked(w.Hive, w.Sub, w.Name, w.Value, w.Kind);
            log?.Invoke($"      {Title}: {string.Join(", ", Writes.Select(w => $"{w.Name}={Fmt(w.Value)}"))}");
            return ok;
        }

        private static string Fmt(object v) => v == null ? "(нет)" : v.ToString();

        private static bool Same(object a, object b)
        {
            if (a == null || b == null) return false;
            if (a is int ai && b is int bi) return ai == bi;
            return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── imperative tweak ────────────────────────────────────────────────
    public sealed class ActionTweak : TweakItem
    {
        public Func<TweakState> Detector;
        public Func<Action<string>, bool> Action;
        public Func<BackupSnapshot, Action<string>, bool> Undo;

        public override TweakState Detect() =>
            Detector?.Invoke() ?? new TweakState { Status = TweakStatus.Unsupported, Detail = "состояние не читается" };

        public override bool Apply(Action<string> log) => Action == null || Action(log);

        public override bool RevertExtra(BackupSnapshot snap, Action<string> log)
            => Undo == null || Undo(snap, log);
    }

    // ── apply / revert one item at a time ───────────────────────────────
    public static class TweakRunner
    {
        public static string Safe(string id) =>
            new string((id ?? "tweak").Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());

        public static string BackupPathFor(string id) =>
            Path.Combine(BackupManager.BackupDir, $"tweak-{Safe(id)}.json");

        public static bool HasBackup(string id)
        {
            try { return File.Exists(BackupPathFor(id)); } catch { return false; }
        }

        /// <summary>
        /// Applies one tweak. The first apply of a tweak permanently records the
        /// machine state it was in, so "откатить" always returns to the original.
        /// </summary>
        public static (bool ok, string backup) ApplyOne(TweakItem item, Action<string> log)
        {
            bool pristine = !HasBackup(item.Id);
            BackupManager.Begin("tweak:" + item.Id);
            bool ok = false;
            try { ok = item.Apply(log); }
            catch (Exception ex) { log?.Invoke($"   [ОШИБКА] {item.Title}: {ex.Message}"); }

            string path = BackupPathFor(item.Id);
            if (pristine)
            {
                try
                {
                    path = BackupManager.SaveAs($"tweak-{Safe(item.Id)}.json");
                    log?.Invoke($"      исходное состояние сохранено: {Path.GetFileName(path)}");
                }
                catch (Exception ex) { log?.Invoke($"      [предупреждение] не удалось сохранить бэкап: {ex.Message}"); }
            }
            return (ok, path);
        }

        public static bool RevertOne(TweakItem item, Action<string> log)
        {
            string path = BackupPathFor(item.Id);
            if (!File.Exists(path))
            {
                log?.Invoke($"   «{item.Title}»: откатывать нечего — твик ещё ни разу не применялся этим профилем");
                return false;
            }

            BackupSnapshot snap = null;
            try { snap = BackupManager.Load(path); } catch { }

            log?.Invoke($"   откат «{item.Title}» из {Path.GetFileName(path)}");
            BackupManager.Restore(path, log);
            try { item.RevertExtra(snap, log); }
            catch (Exception ex) { log?.Invoke($"      [ОШИБКА] {ex.Message}"); }
            return true;
        }

        public static (int applied, int failed) ApplyMany(IEnumerable<TweakItem> items, Action<string> log)
        {
            int ok = 0, bad = 0;
            foreach (var i in items)
            {
                log?.Invoke($"   ▶ {i.Title}");
                var (success, _) = ApplyOne(i, log);
                if (success) ok++; else bad++;
            }
            return (ok, bad);
        }
    }
}
