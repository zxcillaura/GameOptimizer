using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using GameOptimizer.Core.Models;

namespace GameOptimizer.Core.Managers
{
    public class RegistryManager
    {
        private readonly string _backupFilePath;
        private readonly List<RegistryBackup> _backups;

        public RegistryManager()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GameOptimizer");
            Directory.CreateDirectory(appData);
            _backupFilePath = Path.Combine(appData, "backups.json");
            _backups = LoadBackups();
        }

        private List<RegistryBackup> LoadBackups()
        {
            if (File.Exists(_backupFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_backupFilePath);
                    return JsonSerializer.Deserialize<List<RegistryBackup>>(json) ?? new List<RegistryBackup>();
                }
                catch
                {
                    return new List<RegistryBackup>();
                }
            }
            return new List<RegistryBackup>();
        }

        private void SaveBackups()
        {
            var json = JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_backupFilePath, json);
        }

        /// <summary>
        /// Записать значение в реестр с сохранением бэкапа (автоопределение типа)
        /// </summary>
        public void SetValue(RegistryHive hive, string subKey, string valueName, object value)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(subKey, true);
                if (key == null) return;

                var oldValue = key.GetValue(valueName);
                if (oldValue != null && !oldValue.Equals(value))
                {
                    _backups.Add(new RegistryBackup
                    {
                        KeyPath = $"{hive}\\{subKey}",
                        ValueName = valueName,
                        OldValue = oldValue,
                        BackupDate = DateTime.Now
                    });
                    SaveBackups();
                }

                var kind = value switch
                {
                    int _ => RegistryValueKind.DWord,
                    string _ => RegistryValueKind.String,
                    long _ => RegistryValueKind.QWord,
                    byte[] _ => RegistryValueKind.Binary,
                    _ => RegistryValueKind.String
                };

                key.SetValue(valueName, value, kind);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка записи в реестр: {hive}\\{subKey}\\{valueName}", ex);
            }
        }

        /// <summary>
        /// Записать значение с указанием типа
        /// </summary>
        public void SetValue(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind kind)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(subKey, true);
                if (key == null) return;

                var oldValue = key.GetValue(valueName);
                if (oldValue != null && !oldValue.Equals(value))
                {
                    _backups.Add(new RegistryBackup
                    {
                        KeyPath = $"{hive}\\{subKey}",
                        ValueName = valueName,
                        OldValue = oldValue,
                        BackupDate = DateTime.Now
                    });
                    SaveBackups();
                }

                key.SetValue(valueName, value, kind);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка записи в реестр: {hive}\\{subKey}\\{valueName}", ex);
            }
        }

        /// <summary>
        /// Восстановить все бэкапы
        /// </summary>
        public void RestoreAll()
        {
            foreach (var backup in _backups)
            {
                try
                {
                    var parts = backup.KeyPath.Split('\\');
                    string hiveStr = parts[0];
                    string subKey = string.Join("\\", parts, 1, parts.Length - 1);

                    RegistryHive hive = hiveStr switch
                    {
                        "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
                        "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
                        _ => RegistryHive.LocalMachine
                    };

                    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                    using var key = baseKey.OpenSubKey(subKey, true);
                    if (key != null)
                    {
                        var kind = backup.OldValue switch
                        {
                            int _ => RegistryValueKind.DWord,
                            string _ => RegistryValueKind.String,
                            long _ => RegistryValueKind.QWord,
                            byte[] _ => RegistryValueKind.Binary,
                            _ => RegistryValueKind.String
                        };
                        key.SetValue(backup.ValueName, backup.OldValue, kind);
                    }
                }
                catch { }
            }
            _backups.Clear();
            SaveBackups();
        }

        public bool HasBackups => _backups.Count > 0;
    }
}