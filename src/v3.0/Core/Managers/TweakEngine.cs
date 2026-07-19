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
        private List<TweakDefinition> _tweaks;
        private readonly RegistryManager _registryManager;

        public TweakEngine(string jsonPath)
        {
            _jsonPath = jsonPath;
            _registryManager = new RegistryManager();
            LoadTweaks();
        }

        public List<TweakDefinition> LoadTweaks()
        {
            if (!File.Exists(_jsonPath))
                return new List<TweakDefinition>();

            var json = File.ReadAllText(_jsonPath);
            _tweaks = JsonSerializer.Deserialize<List<TweakDefinition>>(json) ?? new List<TweakDefinition>();
            return _tweaks;
        }

        public List<TweakDefinition> GetTweaksByCategory(string category)
        {
            return _tweaks?.FindAll(t => t.Category == category) ?? new List<TweakDefinition>();
        }

        public bool IsTweakApplied(TweakDefinition tweak)
        {
            if (string.IsNullOrEmpty(tweak.CheckRegistryPath) || string.IsNullOrEmpty(tweak.CheckValueName))
                return false;

            try
            {
                var parts = tweak.CheckRegistryPath.Split('\\');
                string hiveStr = parts[0];
                string subKey = string.Join("\\", parts, 1, parts.Length - 1);

                RegistryHive hive = hiveStr switch
                {
                    "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
                    "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
                    _ => RegistryHive.LocalMachine
                };

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKey);
                if (key == null) return false;

                var currentValue = key.GetValue(tweak.CheckValueName);
                if (currentValue == null) return false;

                var expected = tweak.ExpectedValue;
                if (currentValue is int intVal && expected is int expectedInt)
                    return intVal == expectedInt;
                if (currentValue is string strVal && expected is string expectedStr)
                    return strVal == expectedStr;
                if (currentValue is long longVal && expected is int expectedInt2)
                    return longVal == expectedInt2;
                if (currentValue is uint uintVal && expected is int expectedInt3)
                    return uintVal == expectedInt3;

                return currentValue.Equals(expected);
            }
            catch
            {
                return false;
            }
        }

        public bool ApplyTweak(TweakDefinition tweak)
        {
            try
            {
                foreach (var op in tweak.ApplyOperations)
                {
                    ApplyOperation(op);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Apply Error] {ex.Message}");
                return false;
            }
        }

        public bool RevertTweak(TweakDefinition tweak)
        {
            try
            {
                foreach (var op in tweak.RevertOperations)
                {
                    ApplyOperation(op);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Revert Error] {ex.Message}");
                return false;
            }
        }

        private void ApplyOperation(RegistryOperation op)
        {
            if (string.IsNullOrEmpty(op.KeyPath))
                return;

            var parts = op.KeyPath.Split('\\');
            string hiveStr = parts[0];
            string subKey = string.Join("\\", parts, 1, parts.Length - 1);

            RegistryHive hive = hiveStr switch
            {
                "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
                "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
                _ => RegistryHive.LocalMachine
            };

            // Определяем тип значения
            var kind = op.ValueKind;
            if (kind == RegistryValueKind.Unknown)
            {
                kind = op.Value switch
                {
                    int _ => RegistryValueKind.DWord,
                    string _ => RegistryValueKind.String,
                    long _ => RegistryValueKind.QWord,
                    byte[] _ => RegistryValueKind.Binary,
                    _ => RegistryValueKind.String
                };
            }

            _registryManager.SetValue(hive, subKey, op.ValueName, op.Value, kind);
        }
    }
}