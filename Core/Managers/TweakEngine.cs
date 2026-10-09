using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using GameOptimizer.Core.Models;

namespace GameOptimizer.Core.Managers
{
    public class TweakEngine
    {
        private readonly string _jsonPath;
        private List<TweakDefinition> _tweaks = new();
        private static readonly HashSet<string> BlockedUnsafeTweaks = new(StringComparer.OrdinalIgnoreCase)
        {
            "mpo-off", "spectre-meltdown-off", "disable-ipv6", "tcp-autotuning-off",
            "disable-hpet", "core-parking-off", "deep-cstates-off"
        };

        public TweakEngine(string jsonPath)
        {
            _jsonPath = jsonPath;
            LoadTweaks();
        }

        public List<TweakDefinition> LoadTweaks()
        {
            if (!File.Exists(_jsonPath)) return _tweaks;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json = File.ReadAllText(_jsonPath);
                _tweaks = JsonSerializer.Deserialize<List<TweakDefinition>>(json, opts) ?? new();
            }
            catch { }
            return _tweaks;
        }

        public List<TweakDefinition> GetAll() => _tweaks;

        public List<TweakDefinition> GetByCategory(string category)
            => _tweaks.FindAll(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        public bool IsTweakApplied(TweakDefinition tweak)
        {
            if (string.IsNullOrEmpty(tweak.CheckRegistryPath) || string.IsNullOrEmpty(tweak.CheckValueName))
                return false;
            try
            {
                var (hive, sub) = ParsePath(tweak.CheckRegistryPath);
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(sub);
                if (key == null) return false;

                var cur = key.GetValue(tweak.CheckValueName);
                if (cur == null) return false;

                var exp = tweak.ExpectedValue;
                if (exp is JsonElement je)
                {
                    exp = je.ValueKind switch
                    {
                        JsonValueKind.Number => je.TryGetInt32(out int i) ? (object)i : je.GetInt64(),
                        JsonValueKind.String => je.GetString()!,
                        _ => exp
                    };
                }

                return cur switch
                {
                    int i   when exp is int  ei => i == ei,
                    int i   when exp is long el => i == el,
                    long l  when exp is int  ei => l == ei,
                    string s when exp is string es => s == es,
                    _                            => cur.Equals(exp)
                };
            }
            catch { return false; }
        }

        public bool ApplyTweak(TweakDefinition tweak)
        {
            if (BlockedUnsafeTweaks.Contains(tweak.Id))
            {
                Console.WriteLine($"[Blocked] Unsafe tweak skipped: {tweak.Id}");
                return false;
            }
            try
            {
                foreach (var op in tweak.ApplyOperations)
                    if (!ApplyOp(op)) return false;
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"[Apply] {ex.Message}"); return false; }
        }

        public bool RevertTweak(TweakDefinition tweak)
        {
            try
            {
                foreach (var op in tweak.RevertOperations)
                    if (!ApplyOp(op)) return false;
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"[Revert] {ex.Message}"); return false; }
        }

        private bool ApplyOp(RegistryOperation op)
        {
            if (string.IsNullOrEmpty(op.KeyPath)) return false;
            var (hive, sub) = ParsePath(op.KeyPath);

            object val = op.Value is JsonElement je
                ? je.ValueKind switch
                {
                    JsonValueKind.Number => je.TryGetInt32(out int i) ? (object)i : je.GetInt64(),
                    JsonValueKind.String => je.GetString()!,
                    _ => op.Value
                }
                : op.Value ?? 0;

            var kind = op.ValueKind == RegistryValueKind.Unknown
                ? val switch
                {
                    int    => RegistryValueKind.DWord,
                    long   => RegistryValueKind.QWord,
                    string => RegistryValueKind.String,
                    byte[] => RegistryValueKind.Binary,
                    _      => RegistryValueKind.DWord
                }
                : op.ValueKind;

            return RegistryManager.SetValue(hive, sub, op.ValueName, val, kind);
        }

        private static (RegistryHive hive, string sub) ParsePath(string path)
        {
            int sep = path.IndexOf('\\');
            string hiveStr = sep == -1 ? path : path[..sep];
            string sub     = sep == -1 ? string.Empty : path[(sep + 1)..];
            var hive = hiveStr switch
            {
                "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
                "HKEY_CURRENT_USER"  => RegistryHive.CurrentUser,
                _                    => RegistryHive.LocalMachine
            };
            return (hive, sub);
        }
    }
}
