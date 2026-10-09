using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public enum PresetType { Hard, Soft, Visual }

    public static class OptimizerEngine
    {
        // ── Game Mode & HAGS ──────────────────────────────────────────────────

        public static void EnableWindowsGameMode(Action<string> log)
        {
            log("[GameMode] Enabling Windows Game Mode...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);
            log("[GameMode] HAGS left unchanged; its benefit depends on driver and GPU.");
            log("[OK] Game Mode enabled. Reboot recommended if Windows requests it.");
        }

        // ── Shader Cache ──────────────────────────────────────────────────────

        public static void ClearShaderCache(Action<string> log)
        {
            log("[ShaderCache] Clearing shader cache...");
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string[] paths = {
                System.IO.Path.Combine(local,   "D3DSCache"),
                System.IO.Path.Combine(local,   "DXCache"),
                System.IO.Path.Combine(local,   "NVIDIA", "DXCache"),
                System.IO.Path.Combine(local,   "NVIDIA", "GLCache"),
                System.IO.Path.Combine(local,   "AMD",    "DXCache"),
                System.IO.Path.Combine(local,   "AMD",    "GLCache"),
                System.IO.Path.Combine(common,  "NVIDIA Corporation", "NV_Cache")
            };
            int deleted = 0;
            foreach (var p in paths)
            {
                if (!System.IO.Directory.Exists(p)) continue;
                try
                {
                    System.IO.Directory.Delete(p, true);
                    deleted++;
                    log($"   Deleted: {System.IO.Path.GetFileName(p)}");
                }
                catch { }
            }
            log($"[OK] Cleared {deleted} shader cache folders!");
        }

        // ── CS2 Optimize ──────────────────────────────────────────────────────

        public static void OptimizeCS2(Action<string> log)
        {
            log("[CS2] Optimizing CS2 and Steam...");
            foreach (var proc in Process.GetProcessesByName("cs2"))
            {
                try { proc.PriorityClass = ProcessPriorityClass.High; log($"   Priority cs2.exe (PID {proc.Id}) -> High"); }
                catch { }
            }
            log("[CS2] Process priority changes are intentionally not forced globally.");
            log("[CS2] Configure Reflex, FPS cap and graphics per game for lower latency.");
            log("[OK] CS2 diagnostic complete; no undocumented launch options were added.");
        }

        // ── Presets ───────────────────────────────────────────────────────────

        public static void ApplyPreset(PresetType type, Action<string> log)
        {
            log($"[{DateTime.Now:HH:mm:ss}] Applying preset {type.ToString().ToUpper()}...");
            switch (type)
            {
                case PresetType.Hard:   ApplyHard(log);   break;
                case PresetType.Soft:   ApplySoft(log);   break;
                case PresetType.Visual: ApplyVisual(log); break;
            }
            log($"[{DateTime.Now:HH:mm:ss}] Preset {type.ToString().ToUpper()} applied!");
            log("Reboot recommended for full effect.");
        }

        public static void Rollback(Action<string> log)
        {
            log($"[{DateTime.Now:HH:mm:ss}] Restoring all backed-up registry values...");
            try
            {
                var restored = RegistryManager.RestoreAllBackups(log);
                log(restored ? $"[{DateTime.Now:HH:mm:ss}] All changes reverted successfully." : "[WARN] Backup не найден или откат выполнен не полностью.");
            }
            catch (Exception ex) { log($"[ERROR] {ex.Message}"); }
        }

        // ── HARD — Maximum performance, all tweaks ────────────────────────────

        private static void ApplyHard(Action<string> log)
        {
            log("--- HARD OPTIMIZATION START ---");

            // Use only settings with a documented, reversible Windows behavior.
            log("  Scheduler and GPU task priorities left at Windows defaults.");
            log("  Network stack left unchanged for stability.");

            // 4. GameDVR / GameBar OFF
            log("  Disabling GameDVR and GameBar...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);

            // 5. Mouse acceleration OFF
            log("  Disabling mouse acceleration...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String);

            // 6. Visual effects OFF
            log("  Disabling visual effects...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                "VisualFXSetting", 2, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", 0, RegistryValueKind.DWord);

            // Security, telemetry and Windows Update remain at Windows defaults.
            log("  Security, telemetry and Windows Update left unchanged.");

            // 9. Power plan — Maximum Performance
            log("  Power plan: Maximum Performance...");
            RunCmd("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", log);
            AdvancedOptimizer.ApplySafeHardwareProfile(log);

            // Boot timer changes are hardware/firmware dependent and are not forced.
            log("  Boot timer configuration left unchanged for stability.");

            // Core parking and NTFS policy remain adaptive. Forcing all cores online
            // can increase heat and reduce the CPU's normal boost headroom.
            log("  Core parking and NTFS policies left adaptive.");

            // Security mitigations and IPv6 stay unchanged. Disabling either globally
            // can reduce security or break VPNs, games and dual-stack networks.
            log("  Security mitigations and IPv6 left unchanged.");

            // MPO is driver-specific and remains unchanged.
            log("  MPO left unchanged for GPU compatibility.");

            // HAGS is not forced globally; test it per machine and driver profile.
            log("  HAGS left unchanged for driver compatibility.");

            // Timer resolution is owned by Windows and individual applications;
            // do not hold a process-wide request after the one-click operation.
            log("  Timer resolution left under Windows control.");

            // CPU deep C-States are firmware dependent and remain enabled.
            log("  CPU deep C-States left unchanged for stability.");

            // Global low-latency driver overrides are not used: Reflex/Anti-Lag
            // should be configured per game and global overrides can conflict with VRR.
            log("  GPU latency mode left to per-game driver profiles.");

            // Disable accessibility shortcuts only; mouse polling remains controlled
            // by the mouse firmware/driver, not undocumented Windows registry values.
            AdvancedOptimizer.DisableKeyboardFilters(log);

            // 21. Desktop animations OFF
            AdvancedOptimizer.DisableDesktopAnimations(log);

            // V-Sync is intentionally left to the per-game profile. A global registry
            // override can break G-SYNC/VRR and does not guarantee lower latency.

            log("[OK] HARD optimization complete!");
        }

        // ── SOFT — Safe tweaks only, no experimental ─────────────────────────

        private static void ApplySoft(Action<string> log)
        {
            log("--- SOFT OPTIMIZATION START ---");

            // 1. Game Mode ON
            log("  Windows Game Mode ON...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);

            // Privacy/security services remain enabled. The optimizer never disables
            // Defender, Firewall, Update or telemetry policies globally.
            log("  Security and update services left unchanged.");

            // Keep scheduler and filesystem policies adaptive; they are hardware
            // and workload dependent and do not guarantee lower input latency.
            log("  Scheduler and filesystem policies left at Windows defaults.");

            // 5. GameDVR OFF
            log("  GameDVR OFF...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);

            // 6. Mouse acceleration OFF
            log("  Mouse acceleration OFF...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String);

            log("  Global GPU task priority left at Windows defaults.");

            // Network registry tweaks are intentionally not applied: ping is
            // determined by the route, ISP, server and queueing, not magic registry values.
            log("  Network stack left unchanged.");

            // 9. Power plan — Balanced + cores
            log("  Power plan: Balanced...");
            RunCmd("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", log);
            // MPO is driver-specific; leave it unchanged in the safe preset.
            log("  MPO left unchanged for GPU compatibility.");

            // HAGS remains unchanged because results vary by GPU, driver and game.
            log("  HAGS left unchanged for driver compatibility.");

            log("[OK] SOFT optimization complete!");
        }

        // ── VISUAL — Restore visuals ──────────────────────────────────────────

        private static void ApplyVisual(Action<string> log)
        {
            log("  Enabling ClearType fonts...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "FontSmoothing", "2", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "FontSmoothingType", 2, RegistryValueKind.DWord);
            log("  Transparency ON...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", 1, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                "VisualFXSetting", 1, RegistryValueKind.DWord);
            log("[OK] Visual optimization complete!");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Public helper for one-click fix buttons (FACEIT / Vanguard).</summary>
        public static void RunCommand(string cmd, string description)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
                { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch { /* best-effort, ignore failures */ }
        }

        private static void RunCmd(string cmd, Action<string>? log)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe", $"-Command \"{cmd}\"")
                { UseShellExecute = true, CreateNoWindow = true, Verb = "runas" };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { log?.Invoke($"[WARN] {cmd}: {ex.Message}"); }
        }

        private static void RunBcd(string cmd, Action<string>? log)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
                { UseShellExecute = true, CreateNoWindow = true, Verb = "runas" };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { log?.Invoke($"[WARN] {cmd}: {ex.Message}"); }
        }
    }
}
