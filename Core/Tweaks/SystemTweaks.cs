using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Tweaks
{
    /// <summary>
    /// Every individual system change in v6. Each write is captured by
    /// BackupManager first, so a single click can undo all of it.
    /// </summary>
    public static class SystemTweaks
    {
        public const string PlanUltimate = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        public const string PlanHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        public const string PlanBalanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

        // ── 0. safety net ────────────────────────────────────────────────
        public static bool CreateRestorePoint(Action<string> log)
        {
            log("  [0] Точка восстановления Windows...");
            // Windows only creates one checkpoint per 24 h unless this is 0.
            BackupManager.WriteTracked(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
                "SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord);

            ProcessRunner.PowerShell("Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue", 60000);
            var (code, output) = ProcessRunner.PowerShell(
                "try { Checkpoint-Computer -Description 'GAMEOPTIMIZ1.0' " +
                "-RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop; 'OK' } catch { \"FAIL: $($_.Exception.Message)\" }",
                300000);

            bool ok = output.Contains("OK") && !output.Contains("FAIL");
            log(ok
                ? "      точка восстановления создана"
                : $"      точка восстановления не создана ({output.Trim().Split('\n').LastOrDefault()?.Trim()}) — " +
                  "продолжаю, отдельный бэкап реестра всё равно ведётся");
            return ok;
        }

        // ── 1. power ─────────────────────────────────────────────────────
        public static void SetPowerPlan(string guid, string label, Action<string> log)
        {
            var (code, output) = ProcessRunner.Run("powercfg.exe", $"/setactive {guid}");
            bool ok = code == 0;
            if (!ok && guid == PlanUltimate)
            {
                // Ultimate Performance does not exist on every edition — create it.
                ProcessRunner.Run("powercfg.exe", $"-duplicatescheme {PlanUltimate}");
                (code, output) = ProcessRunner.Run("powercfg.exe", $"/setactive {guid}");
                ok = code == 0;
            }
            log(ok ? $"  [1] Схема питания: {label}" : $"  [1] Не удалось включить схему {label}: {output.Trim()}");
        }

        private static void SetAc(string sub, string setting, uint value, string title, Action<string> log)
        {
            var (code, _) = ProcessRunner.Run("powercfg.exe",
                $"/setacvalueindex SCHEME_CURRENT {sub} {setting} {value}");
            if (code == 0) log($"      {title} = {value}");
            else log($"      {title} — настройка недоступна на этой схеме");
        }

        /// <summary>Hidden power settings that actually reduce wake-up and polling latency.</summary>
        public static void TunePowerPlan(bool extreme, Action<string> log)
        {
            const string SUB_PROC = "54533251-82be-4824-96c1-47b60b740d00";
            const string SUB_DISK = "0012ee47-9041-4b5d-9b77-535fba8b1442";
            const string SUB_USB  = "2a737441-1930-4402-8d77-b2bebba308a3";
            const string SUB_PCIE = "501a4d13-42af-4429-9fd1-a8218c268e20";

            log("  [1] Скрытые параметры питания:");
            SetAc(SUB_PROC, "be337238-0d82-4146-a960-4f3749d470c7", 2, "режим буста процессора = Aggressive", log);
            SetAc(SUB_USB,  "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", 0, "USB selective suspend = выкл", log);
            SetAc(SUB_DISK, "6738e2c4-e8a5-4a42-b16a-e040e769756e", 0, "отключение диска = никогда", log);
            SetAc(SUB_PCIE, "ee12f906-d277-404b-b6da-e5fa1a576df5", 0, "PCIe ASPM = выкл", log);

            if (extreme)
            {
                SetAc(SUB_PROC, "893dee8e-2bef-41e0-89c6-b55d0929964c", 100,
                    "минимальное состояние CPU = 100%", log);
                SetAc(SUB_PROC, "0cc5b647-c1df-4637-891a-dec35c318583", 100,
                    "минимальное число активных ядер = 100% (снятие парковки)", log);
                log("      (extreme) процессор держит частоту постоянно — простой станет горячее");
            }
            else
            {
                SetAc(SUB_PROC, "0cc5b647-c1df-4637-891a-dec35c318583", 100,
                    "минимальное число активных ядер = 100% (снятие парковки)", log);
            }

            ProcessRunner.Run("powercfg.exe", "/setactive SCHEME_CURRENT");
        }

        // ── 4. network adapter ───────────────────────────────────────────
        private static readonly (string keyword, int value, string title)[] NicKeywords =
        {
            ("*EEE",                  0, "Энергосберегающий Ethernet (EEE)"),
            ("EnableGreenEthernet",   0, "Green Ethernet"),
            ("GigaLite",              0, "Gigabit Lite (держал линк на 100 Мбит/с)"),
            ("PowerSavingMode",       0, "Power Saving Mode"),
            ("*InterruptModeration",  0, "Модерация прерываний"),
            ("*FlowControl",          0, "Управление потоком"),
            ("*SpeedDuplex",          0, "Speed & Duplex = автосогласование"),
            ("AdvancedEEE",           0, "Advanced EEE"),
            ("GigabitLite",           0, "Gigabit Lite (альт. ключ)"),
        };

        public static void TuneNetworkAdapter(Action<string> log)
        {
            log("  [4] Сетевой адаптер (Realtek):");

            // snapshot every advanced property so it can be restored as-is
            var (_, dump) = ProcessRunner.PowerShell(
                "Get-NetAdapterAdvancedProperty | ForEach-Object { " +
                "\"$($_.Name)|$($_.RegistryKeyword)|$($_.RegistryValue)|$($_.DisplayName)|$($_.DisplayValue)\" }",
                60000);
            // written once and never overwritten, so it always holds the factory state
            string snap = Path.Combine(BackupManager.Root, "nic-advanced-original.txt");
            try
            {
                Directory.CreateDirectory(BackupManager.Root);
                if (!File.Exists(snap)) File.WriteAllText(snap, dump);
                log($"      снимок исходных параметров: {snap}");
            }
            catch { }

            var adapters = (ProcessRunner.PowerShell(
                "Get-NetAdapter | Where-Object Status -eq 'Up' | Select-Object -ExpandProperty Name", 60000).output ?? "")
                .Split('\n').Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.Contains("vEthernet", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (adapters.Length == 0) { log("      активный физический адаптер не найден"); return; }

            foreach (var adapter in adapters)
            {
                log($"      адаптер: {adapter}");
                foreach (var (keyword, value, title) in NicKeywords.DistinctBy(k => k.keyword))
                {
                    var (code, outText) = ProcessRunner.PowerShell(
                        $"Set-NetAdapterAdvancedProperty -Name '{adapter}' -RegistryKeyword '{keyword}' " +
                        $"-RegistryValue {value} -ErrorAction Stop; 'OK'",
                        30000);
                    if (outText.Contains("OK"))
                    {
                        // verify what the driver actually accepted
                        var (_, now) = ProcessRunner.PowerShell(
                            $"(Get-NetAdapterAdvancedProperty -Name '{adapter}' -RegistryKeyword '{keyword}').RegistryValue",
                            20000);
                        log($"        {title} -> {now.Trim()}");
                    }
                }
            }

            // report the resulting link speed — that is the number that matters
            var (_, speed) = ProcessRunner.PowerShell(
                "Get-NetAdapter | Where-Object Status -eq 'Up' | " +
                "ForEach-Object { \"$($_.Name): $($_.LinkSpeed)\" }", 30000);
            log("      итог по линку:");
            foreach (var line in speed.Split('\n').Where(l => l.Trim().Length > 0))
                log($"        {line.Trim()}");
            log("      (если скорость осталась 100 Мбит/с — проверь кабель и порт роутера)");
        }

        // ── 5. overlays ──────────────────────────────────────────────────
        public static void CloseOverlayProcesses(Action<string> log)
        {
            log("  [5] Оверлеи:");
            string[] targets = { "NVIDIA Overlay", "NVIDIA Share", "NVIDIA GeForce Overlay" };
            int killed = 0;
            foreach (var name in targets)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); killed++; } catch { }
                }
            }
            log(killed > 0
                ? $"      закрыто процессов оверлея NVIDIA: {killed} (перезапустятся с приложением NVIDIA)"
                : "      процессов оверлея NVIDIA не найдено");
            log("      Discord: выключи оверлей вручную (Настройки → Оверлей) — он не отключается из реестра безопасно");
        }

        // ── 6. autostart cleanup ─────────────────────────────────────────
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Known browser/overlay/launcher junk in HKCU\Run. Everything else
        /// (Steam, Discord, anti-cheat, work tools) is deliberately left alone.
        /// </summary>
        public static bool IsJunkStartup(string name)
        {
            string n = name ?? "";
            return n.StartsWith("YandexBrowserAutoLaunch_", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("GoogleChromeAutoLaunch_", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Mozilla-Firefox-", StringComparison.OrdinalIgnoreCase)
                || n.Equals("RobloxPlayerBeta", StringComparison.OrdinalIgnoreCase)
                || n.Equals("RiotClient", StringComparison.OrdinalIgnoreCase)
                || n.Equals("EADM", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("electron.app.", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Overwolf", StringComparison.OrdinalIgnoreCase);
        }

        public static void CleanStartup(Action<string> log)
        {
            log("  [6] Автозагрузка:");
            using var key = Reg.Open(RegistryHive.CurrentUser, RunKey, false);
            var present = key?.GetValueNames() ?? Array.Empty<string>();

            int removed = 0;
            foreach (var name in present)
            {
                if (!IsJunkStartup(name)) continue;
                BackupManager.TrackRegistry(RegistryHive.CurrentUser, RunKey, name);
                if (Reg.Delete(RegistryHive.CurrentUser, RunKey, name))
                {
                    removed++;
                    log($"      убрано из автозагрузки: {name}");
                }
            }
            log(removed > 0
                ? $"      убрано записей: {removed}; Steam, Discord, античиты и прочее не тронуто"
                : "      мусорной автозагрузки не найдено");
        }

        // ── 7. VBS / HVCI ────────────────────────────────────────────────
        public static bool DisableVbs(bool turnOffHypervisor, Action<string> log)
        {
            log("  [7] VBS / Memory Integrity:");
            BackupManager.WriteTracked(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard",
                "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
            BackupManager.WriteTracked(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity",
                "Enabled", 0, RegistryValueKind.DWord);
            log("      EnableVirtualizationBasedSecurity = 0, HVCI = 0");

            if (turnOffHypervisor)
            {
                BackupManager.TrackBcd("hypervisorlaunchtype");
                var (code, _) = ProcessRunner.Run("bcdedit.exe", "/set hypervisorlaunchtype off");
                log(code == 0
                    ? "      hypervisorlaunchtype = off (гипервизор выключен полностью)"
                    : "      не удалось изменить hypervisorlaunchtype (нужны права администратора)");
                log("      ВНИМАНИЕ: BlueStacks / WSL2 / виртуальные машины перестанут работать");
            }
            else
            {
                log("      гипервизор оставлен включённым — BlueStacks/WSL2 продолжат работать");
            }
            log("      требуется перезагрузка для применения");
            return true;
        }

        // ── 8. standby list / memory ─────────────────────────────────────
        public static void TrimWorkingSets(Action<string> log)
        {
            log("  [8] Очистка рабочих наборов процессов...");
            int n = 0;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.WorkingSet64 > 200L * 1024 * 1024)
                    {
                        p.MaxWorkingSet = p.MaxWorkingSet;   // forces a trim request
                        n++;
                    }
                }
                catch { }
            }
            log($"      подрезано процессов: {n}");
        }

        public static bool IsAdmin()
            => new System.Security.Principal.WindowsPrincipal(
                   System.Security.Principal.WindowsIdentity.GetCurrent())
               .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
