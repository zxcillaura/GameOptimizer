using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public static class NetworkOptimizer
    {
        // ── DNS ──────────────────────────────────────────────────────────────

        public static void SetCustomDNS(string primaryDns, string secondaryDns, Action<string> log)
        {
            log($"-> Setting DNS: {primaryDns} / {secondaryDns}");
            int count = 0;
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                try
                {
                    log($"   Adapter: {adapter.Name}");
                    RunCmd($"/c netsh interface ipv4 set dns name=\"{adapter.Name}\" static {primaryDns} primary");
                    if (!string.IsNullOrEmpty(secondaryDns))
                        RunCmd($"/c netsh interface ipv4 add dns name=\"{adapter.Name}\" {secondaryDns} index=2");
                    count++;
                }
                catch (Exception ex) { log($"[ERROR] {adapter.Name}: {ex.Message}"); }
            }
            RunCmd("/c ipconfig /flushdns");
            log(count > 0 ? $"[OK] DNS applied to {count} adapters. Cache flushed." : "[ERROR] No active adapters found.");
        }

        public static void ResetDNSToAuto(Action<string> log)
        {
            log("-> Resetting DNS to DHCP...");
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                   (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    RunCmd($"/c netsh interface ipv4 set dns name=\"{adapter.Name}\" dhcp");
            }
            RunCmd("/c ipconfig /flushdns");
            log("[OK] DNS reset to automatic.");
        }

        // ── TCP/UDP Tweaks ────────────────────────────────────────────────────

        public static void ApplyTCPNoDelay(Action<string> log)
        {
            log("-> TCP NoDelay (Nagle OFF)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\MSTCPIP\Parameters",
                "TcpNoDelay", 1, Microsoft.Win32.RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\MSTCPIP\Parameters",
                "TcpAckFrequency", 1, Microsoft.Win32.RegistryValueKind.DWord);
            log("[OK] TCP NoDelay applied.");
        }

        public static void DisableNetworkThrottling(Action<string> log)
        {
            log("-> Network Throttling OFF...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "SystemResponsiveness", 0, Microsoft.Win32.RegistryValueKind.DWord);
            log("[OK] Network Throttling disabled.");
        }

        public static void OptimizeUDPBuffers(Action<string> log)
        {
            log("-> UDP buffer optimization...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\AFD\Parameters",
                "DefaultReceiveWindow", 65536, Microsoft.Win32.RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\AFD\Parameters",
                "DefaultSendWindow", 65536, Microsoft.Win32.RegistryValueKind.DWord);
            log("[OK] UDP buffers set to 64 KB.");
        }

        public static void EnableRSS(Action<string> log)
        {
            log("-> Enabling RSS (Receive Side Scaling)...");
            RunCmd("/c netsh int tcp set global rss=enabled");
            log("[OK] RSS enabled.");
        }

        public static void ResetNetworkStack(Action<string> log)
        {
            log("-> Full network stack reset (Winsock + IP)...");
            RunCmd("/c ipconfig /release");
            RunCmd("/c ipconfig /flushdns");
            RunCmd("/c ipconfig /renew");
            RunCmd("/c netsh winsock reset");
            RunCmd("/c netsh int ip reset");
            log("[OK] Winsock, IP stack, and DNS cache fully reset.");
            log("NOTE: Reboot required for Winsock reset to take effect.");
        }

        // ── Ping ──────────────────────────────────────────────────────────────

        public static long PingServer(string ip)
        {
            try
            {
                var reply = new Ping().Send(ip, 1500);
                return reply?.Status == IPStatus.Success ? reply.RoundtripTime : -1;
            }
            catch { return -1; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void RunCmd(string args)
        {
            var psi = new ProcessStartInfo("cmd.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                Verb = "runas"
            };
            try { Process.Start(psi)?.WaitForExit(); } catch { }
        }
    }
}
