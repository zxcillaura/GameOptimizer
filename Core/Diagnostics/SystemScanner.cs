using System;
using System.Management;
using Microsoft.Win32;

namespace GameOptimizer.Core.Diagnostics
{
    public sealed class SystemSnapshot
    {
        public string Cpu { get; init; } = "Не определён";
        public string Gpu { get; init; } = "Не определена";
        public string Ram { get; init; } = "Не определена";
        public string Windows { get; init; } = "Не определена";
        public string ActiveNetwork { get; init; } = "Не определён";
        public string SystemDrive { get; init; } = "Не определён";
        public string FreeSpace { get; init; } = "Не определено";
        public bool IsAdmin { get; init; }
        public bool HasWarnings { get; init; }
        public string Summary { get; init; } = "Сканирование завершено.";
    }

    public static class SystemScanner
    {
        public static SystemSnapshot Scan()
        {
            string cpu = Query("select Name from Win32_Processor", "Name");
            string gpu = Query("select Caption from Win32_VideoController", "Caption");
            string ram = QueryRam();
            string windows = ReadWindows();
            string activeNetwork = ReadActiveNetwork();
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "Не определён";
            string freeSpace = ReadFreeSpace(systemDrive);
            bool admin = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(
                System.Security.Principal.WindowsBuiltInRole.Administrator);
            return new SystemSnapshot
            {
                Cpu = cpu, Gpu = gpu, Ram = ram, Windows = windows,
                ActiveNetwork = activeNetwork, SystemDrive = systemDrive, FreeSpace = freeSpace,
                IsAdmin = admin, HasWarnings = !admin,
                Summary = admin ? "Система готова к безопасной оптимизации." : "Нужен запуск от имени администратора для системных изменений."
            };
        }

        private static string Query(string query, string field)
        {
            try { using var searcher = new ManagementObjectSearcher(query); foreach (ManagementObject o in searcher.Get()) return o[field]?.ToString()?.Trim() ?? "Не определён"; }
            catch { }
            return "Не определён";
        }

        private static string QueryRam()
        {
            try { using var searcher = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem"); foreach (ManagementObject o in searcher.Get()) return $"{Convert.ToInt64(o["TotalPhysicalMemory"]) / 1024 / 1024 / 1024} ГБ"; }
            catch { }
            return "Не определена";
        }

        private static string ReadWindows()
        {
            try { using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"); return $"{key?.GetValue("ProductName") ?? "Windows"} (Build {key?.GetValue("CurrentBuildNumber") ?? "?"})"; }
            catch { return Environment.OSVersion.VersionString; }
        }

        private static string ReadActiveNetwork()
        {
            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        return $"{nic.Name} ({nic.NetworkInterfaceType})";
            }
            catch { }
            return "Не определён";
        }

        private static string ReadFreeSpace(string drive)
        {
            try
            {
                var info = new DriveInfo(drive);
                return $"{info.AvailableFreeSpace / 1024d / 1024 / 1024:0.0} ГБ свободно";
            }
            catch { return "Не определено"; }
        }
    }
}
