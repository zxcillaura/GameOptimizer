using System;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public static class RegistryManager
    {
        private const string BackupRoot = @"Software\zxcllaura\Backup";

        /// <summary>
        /// Безопасно записывает значение в реестр, предварительно сохранив оригинал для отката.
        /// </summary>
        public static bool SetValue(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind valueKind)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    // 1. Делаем бэкап оригинального значения, если его еще нет в бэкапе
                    BackupValue(hive, subKey, valueName);

                    // 2. Записываем новое значение
                    using (RegistryKey key = baseKey.CreateSubKey(subKey, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(valueName, value, valueKind);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegistryManager Error] Не удалось записать {subKey}\\{valueName}: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Возвращает все измененные ключи к их первоначальному состоянию.
        /// </summary>
        public static void RestoreAllBackups()
        {
            using (RegistryKey backupKey = Registry.CurrentUser.OpenSubKey(BackupRoot, true))
            {
                if (backupKey == null) return;
                RestoreFlatBackups(backupKey);
            }
        }

        private static void BackupValue(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                string hiveStr = hive.ToString();
                string backupPath = $@"{BackupRoot}\{hiveStr}\{subKey}";

                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (RegistryKey originalKey = baseKey.OpenSubKey(subKey))
                {
                    if (originalKey == null) return;

                    object originalValue = originalKey.GetValue(valueName);
                    if (originalValue == null) return;

                    using (RegistryKey checkKey = Registry.CurrentUser.OpenSubKey(backupPath))
                    {
                        if (checkKey?.GetValue(valueName) != null) return;
                    }

                    using (RegistryKey saveKey = Registry.CurrentUser.CreateSubKey(backupPath, true))
                    {
                        if (saveKey != null)
                        {
                            var valKind = originalKey.GetValueKind(valueName);
                            saveKey.SetValue(valueName, originalValue, valKind);
                            saveKey.SetValue(valueName + "_Kind", (int)valKind, RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Backup Error] Ошибка бэкапа {subKey}\\{valueName}: {ex.Message}");
            }
        }

        private static void RestoreFlatBackups(RegistryKey backupKey)
        {
            RestoreSubKeysRecursive(backupKey, "");
            
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(BackupRoot, false);
            }
            catch { }
        }

        private static void RestoreSubKeysRecursive(RegistryKey currentBackupKey, string currentPath)
        {
            foreach (string subKeyName in currentBackupKey.GetSubKeyNames())
            {
                using (RegistryKey nextKey = currentBackupKey.OpenSubKey(subKeyName))
                {
                    string newPath = string.IsNullOrEmpty(currentPath) ? subKeyName : currentPath + "\\" + subKeyName;
                    
                    if (newPath.StartsWith("LocalMachine") || newPath.StartsWith("CurrentUser"))
                    {
                        ApplyBackupToSystem(newPath, nextKey);
                    }
                    else
                    {
                        RestoreSubKeysRecursive(nextKey, newPath);
                    }
                }
            }
        }

        private static void ApplyBackupToSystem(string backupPath, RegistryKey sourceKey)
        {
            try
            {
                RegistryHive hive = backupPath.StartsWith("LocalMachine") ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
                string realSubKey = backupPath.Substring(backupPath.IndexOf('\\') + 1);

                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (RegistryKey systemKey = baseKey.OpenSubKey(realSubKey, true))
                {
                    if (systemKey == null) return;

                    foreach (string valueName in sourceKey.GetValueNames())
                    {
                        if (valueName.EndsWith("_Kind")) continue;

                        object value = sourceKey.GetValue(valueName);
                        int kindInt = (int)sourceKey.GetValue(valueName + "_Kind", (int)RegistryValueKind.Unknown);
                        RegistryValueKind kind = (RegistryValueKind)kindInt;

                        if (value != null && kind != RegistryValueKind.Unknown)
                        {
                            systemKey.SetValue(valueName, value, kind);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Restore Error] Не удалось откатить {backupPath}: {ex.Message}");
            }
        }
    }

    // Метод расширения для RegistryKey (только один раз!)
    public static class RegistryExtensions
    {
        public static string[] GetSubClassNameTree(this Microsoft.Win32.RegistryKey key)
        {
            return key.GetSubKeyNames();
        }
    }
}