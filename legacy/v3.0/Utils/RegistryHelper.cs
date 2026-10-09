using System;
using Microsoft.Win32;

namespace GameOptimizer.Utils
{
    public static class RegistryHelper
    {
        private static RegistryKey GetRootKey(string path, out string subKeyPath)
        {
            subKeyPath = string.Empty;
            if (string.IsNullOrEmpty(path)) return null;

            int firstSlash = path.IndexOf('\\');
            string rootString = firstSlash == -1 ? path : path.Substring(0, firstSlash);
            subKeyPath = firstSlash == -1 ? string.Empty : path.Substring(firstSlash + 1);

            switch (rootString.ToUpper())
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    return Registry.ClassesRoot;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    return Registry.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    return Registry.LocalMachine;
                case "HKEY_USERS":
                case "HKU":
                    return Registry.Users;
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                    return Registry.CurrentConfig;
                default:
                    return null;
            }
        }

        public static object GetValue(string keyPath, string valueName)
        {
            try
            {
                RegistryKey root = GetRootKey(keyPath, out string subKeyPath);
                if (root == null) return null;

                using (RegistryKey key = root.OpenSubKey(subKeyPath))
                {
                    return key?.GetValue(valueName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка чтения реестра ({keyPath}\\{valueName})", ex);
                return null;
            }
        }

        public static bool SetValue(string keyPath, string valueName, string value, RegistryValueKind kind)
        {
            try
            {
                RegistryKey root = GetRootKey(keyPath, out string subKeyPath);
                if (root == null) return false;

                using (RegistryKey key = root.CreateSubKey(subKeyPath))
                {
                    if (key == null) return false;

                    object parsedValue = ParseValue(value, kind);
                    key.SetValue(valueName, parsedValue, kind);
                    Logger.LogInfo($"Реестр изменен: {keyPath}\\{valueName} = {value} ({kind})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка записи в реестр ({keyPath}\\{valueName} = {value})", ex);
                return false;
            }
        }

        private static object ParseValue(string value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return Convert.ToInt32(value);
                case RegistryValueKind.QWord:
                    return Convert.ToInt64(value);
                case RegistryValueKind.Binary:
                    return string.IsNullOrEmpty(value) ? new byte[0] : ConvertHexStringToByteArray(value);
                default:
                    return value;
            }
        }

        private static byte[] ConvertHexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace(",", "").Replace("-", "");
            if (hex.Length % 2 != 0) hex = "0" + hex;
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}