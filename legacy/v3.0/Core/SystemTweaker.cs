using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public static class SystemTweaker
    {
        public static void ToggleMPO(bool disable, Action<string> log)
        {
            if (disable)
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, RegistryValueKind.DWord);
                log("-> Multi-Plane Overlay (MPO) отключен. Это устранит мерцания и статтеры в играх.");
            }
            else
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 0, RegistryValueKind.DWord);
                log("-> Multi-Plane Overlay (MPO) включен обратно.");
            }
        }

        public static void ToggleNetworkThrottling(bool disable, Action<string> log)
        {
            if (disable)
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                log("-> Network Throttling отключен. Windows больше не будет ограничивать пакеты.");
            }
            else
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                log("-> Network Throttling включен (дефолтное значение 10).");
            }
        }

        public static void ToggleTelemetry(bool disable, Action<string> log)
        {
            if (disable)
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
                log("-> Телеметрия и сбор данных Windows отключены.");
            }
            else
            {
                RegistryManager.SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 3, RegistryValueKind.DWord);
                log("-> Телеметрия возвращена в исходное состояние.");
            }
        }

        public static void ToggleSysMain(bool disable, Action<string> log)
        {
            try
            {
                string startMode = disable ? "disabled" : "auto";
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c sc config SysMain start= {startMode} & sc {(disable ? "stop" : "start")} SysMain")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas"
                };
                Process.Start(psi)?.WaitForExit();
                log(disable ? "-> Служба SysMain (Superfetch) остановлена и отключена. Снижена нагрузка на диск/процессор." : "-> Служба SysMain снова включена.");
            }
            catch (Exception ex)
            {
                log($"[Ошибка SysMain]: {ex.Message}");
            }
        }
    }
}