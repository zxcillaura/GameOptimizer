using System;
using System.Collections.Generic;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;

namespace GameOptimizer.Core.Tweaks
{
    /// <summary>
    /// Windows applies TcpAckFrequency / TCPNoDelay / TcpDelAckTicks
    /// per network interface, not globally. Writing them to the legacy
    /// SOFTWARE\MSTCPIP\Parameters key (as most guides do) has no effect on a
    /// modern stack, so v7 writes the active interfaces as well.
    /// </summary>
    public static class TcpParams
    {
        private const string IfRoot = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
        private const string Legacy = @"SOFTWARE\MSTCPIP\Parameters";

        private static List<string> _cache;
        private static DateTime _cacheAt = DateTime.MinValue;

        /// <summary>Interface subkeys that actually carry traffic (have IP + gateway).</summary>
        public static List<string> ActiveInterfaceKeys(bool force = false)
        {
            if (!force && _cache != null && (DateTime.Now - _cacheAt).TotalMinutes < 5)
                return _cache;

            var found = new List<string>();
            try
            {
                using var root = Reg.Open(RegistryHive.LocalMachine, IfRoot, false);
                if (root != null)
                {
                    foreach (var name in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(name);
                        if (sub == null) continue;

                        bool hasGateway = sub.GetValue("DhcpDefaultGateway") != null
                                       || sub.GetValue("DefaultGateway") != null;

                        // IPAddress is REG_MULTI_SZ, DhcpIPAddress is REG_SZ — casting
                        // both to string[] silently rejected every DHCP-configured NIC
                        string ip = FirstString(sub.GetValue("IPAddress"))
                                 ?? FirstString(sub.GetValue("DhcpIPAddress"))
                                 ?? "";
                        bool hasIp = ip.Length > 0 && ip != "0.0.0.0";

                        if (hasGateway && hasIp) found.Add(IfRoot + "\\" + name);
                    }
                }
            }
            catch { }

            _cache = found;
            _cacheAt = DateTime.Now;
            return found;
        }

        /// <summary>Reads a registry value that may be REG_SZ or REG_MULTI_SZ.</summary>
        private static string FirstString(object raw)
        {
            switch (raw)
            {
                case string[] arr: return arr.Length > 0 ? arr[0]?.Trim() ?? "" : "";
                case string s:     return s.Trim();
                default:           return null;
            }
        }

        /// <summary>Writes a DWORD both per-interface and to the legacy global key.</summary>
        public static int Write(string name, int value, Action<string> log)
        {
            var keys = ActiveInterfaceKeys();
            int written = 0;

            foreach (var key in keys)
                if (BackupManager.WriteTracked(RegistryHive.LocalMachine, key, name, value, RegistryValueKind.DWord))
                    written++;

            // keep the legacy location in sync for older stacks
            BackupManager.WriteTracked(RegistryHive.LocalMachine, Legacy, name, value, RegistryValueKind.DWord);

            log?.Invoke($"      {name}={value}: интерфейсов {written} из {keys.Count} + глобальный ключ");
            return written;
        }

        /// <summary>Reads the value from the active interfaces (authoritative source).</summary>
        public static (int applied, int total, string detail) Read(string name, int expected)
        {
            var keys = ActiveInterfaceKeys();
            if (keys.Count == 0) return (0, 0, "активный сетевой интерфейс не найден");

            int hit = 0;
            var values = new List<string>();
            foreach (var key in keys)
            {
                var v = Reg.Read(RegistryHive.LocalMachine, key, name);
                values.Add(v?.ToString() ?? "нет");
                if (v is int i && i == expected) hit++;
            }
            return (hit, keys.Count, $"{name} = {string.Join("/", values)}  (интерфейсов: {keys.Count})");
        }
    }
}
