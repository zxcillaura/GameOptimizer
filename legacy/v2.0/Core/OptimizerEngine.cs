using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public enum PresetType { Hard, Soft, Visual }

    public static class OptimizerEngine
    {
        // ============================================================
        // МЕТОДЫ ДЛЯ GAMESCONTROL
        // ============================================================

        public static void EnableWindowsGameMode(Action<string> logAction)
        {
            logAction?.Invoke("[GameMode] Включение игрового режима Windows...");
            
            try
            {
                RegistryManager.SetValue(RegistryHive.CurrentUser, 
                    @"Software\Microsoft\GameBar", 
                    "AllowAutoGameMode", 1, RegistryValueKind.DWord);
                
                RegistryManager.SetValue(RegistryHive.CurrentUser, 
                    @"Software\Microsoft\GameBar", 
                    "AutoGameModeEnabled", 1, RegistryValueKind.DWord);
                
                RegistryManager.SetValue(RegistryHive.LocalMachine, 
                    @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", 
                    "HwSchMode", 2, RegistryValueKind.DWord);
                
                logAction?.Invoke("[GameMode] ✅ Игровой режим и HAGS включены!");
                logAction?.Invoke("[GameMode] 🔄 Рекомендуется перезагрузить ПК.");
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[GameMode] ❌ Ошибка: {ex.Message}");
            }
        }

        public static void ClearShaderCache(Action<string> logAction)
        {
            logAction?.Invoke("[ShaderCache] Очистка кэша шейдеров...");
            
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                string[] shaderPaths = {
                    Path.Combine(localAppData, "D3DSCache"),
                    Path.Combine(localAppData, "DXCache"),
                    Path.Combine(localAppData, "NVIDIA", "DXCache"),
                    Path.Combine(localAppData, "NVIDIA", "GLCache"),
                    Path.Combine(localAppData, "AMD", "DXCache"),
                    Path.Combine(localAppData, "AMD", "GLCache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                        "NVIDIA Corporation", "NV_Cache")
                };

                int deletedCount = 0;
                foreach (var path in shaderPaths)
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            Directory.Delete(path, true);
                            deletedCount++;
                            logAction?.Invoke($"[ShaderCache] Удалено: {path}");
                        }
                        catch { }
                    }
                }

                logAction?.Invoke($"[ShaderCache] ✅ Очищено {deletedCount} папок!");
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[ShaderCache] ❌ Ошибка: {ex.Message}");
            }
        }

        public static void OptimizeCS2(Action<string> logAction)
        {
            logAction?.Invoke("[CS2] Оптимизация CS2 и Steam...");
            
            try
            {
                var processes = Process.GetProcessesByName("cs2");
                if (processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        try
                        {
                            proc.PriorityClass = ProcessPriorityClass.High;
                            logAction?.Invoke($"[CS2] ✅ Приоритет CS2 (PID: {proc.Id}) установлен на High");
                        }
                        catch { }
                    }
                }
                else
                {
                    logAction?.Invoke("[CS2] ⚠️ CS2 не запущен.");
                }

                RegistryManager.SetValue(RegistryHive.CurrentUser, 
                    @"Software\Valve\Steam", 
                    "EnableGameOverlay", 0, RegistryValueKind.DWord);
                
                RegistryManager.SetValue(RegistryHive.CurrentUser, 
                    @"Software\Valve\Steam", 
                    "DisableGPUAcceleration", 1, RegistryValueKind.DWord);

                var steamProcesses = Process.GetProcessesByName("steam");
                if (steamProcesses.Length > 0)
                {
                    foreach (var proc in steamProcesses)
                    {
                        try
                        {
                            proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                            logAction?.Invoke($"[CS2] ✅ Приоритет Steam (PID: {proc.Id}) понижен");
                        }
                        catch { }
                    }
                }

                logAction?.Invoke("[CS2] ✅ Оптимизация CS2 завершена!");
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[CS2] ❌ Ошибка: {ex.Message}");
            }
        }

        // ============================================================
        // ОСНОВНЫЕ МЕТОДЫ ДЛЯ ПРИМЕНЕНИЯ ПРОФИЛЕЙ
        // ============================================================

        public static void ApplyPreset(PresetType type, Action<string> logCallback)
        {
            logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Применение профиля {type.ToString().ToUpper()}...");

            switch (type)
            {
                case PresetType.Hard:
                    ApplyHardOptimization(logCallback);
                    break;
                case PresetType.Soft:
                    ApplySoftOptimization(logCallback);
                    break;
                case PresetType.Visual:
                    ApplyVisualOptimization(logCallback);
                    break;
            }

            logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ✅ Профиль {type.ToString().ToUpper()} применён!");
            logCallback?.Invoke("🔄 Рекомендуется перезагрузить ПК.");
        }

        public static void Rollback(Action<string> logCallback)
        {
            logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Запуск восстановления...");
            try
            {
                RegistryManager.RestoreAllBackups();
                logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ✅ Все изменения откачены.");
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"[❌ ОШИБКА] {ex.Message}");
            }
        }

        // ============================================================
        // ПРИВАТНЫЕ МЕТОДЫ ДЛЯ ПРОФИЛЕЙ
        // ============================================================

        private static void ApplyHardOptimization(Action<string> logCallback)
        {
            // === 1. ПЛАНИРОВЩИК ЗАДАЧ (ПРИОРИТЕТ ПРОЦЕССОВ) ===
            logCallback?.Invoke("🔧 Приоритет процессов (Win32PrioritySeparation)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", 
                "Win32PrioritySeparation", 38, RegistryValueKind.DWord);

            // === 2. ОПТИМИЗАЦИЯ СЕТИ (УМЕНЬШЕНИЕ ЗАДЕРЖКИ) ===
            logCallback?.Invoke("🔧 Оптимизация сетевых задержек...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", 
                "NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", 
                "SystemResponsiveness", 0, RegistryValueKind.DWord);

            // === 3. ПРИОРИТЕТ GPU ДЛЯ ИГР ===
            logCallback?.Invoke("🔧 Приоритет GPU для игр...");
            string gameProfilePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
            RegistryManager.SetValue(RegistryHive.LocalMachine, gameProfilePath, "GPU Priority", 8, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine, gameProfilePath, "Priority", 6, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine, gameProfilePath, "Scheduling Category", "High", RegistryValueKind.String);

            // === 4. ОТКЛЮЧЕНИЕ GAMEDVR И GAMEBAR ===
            logCallback?.Invoke("🔧 Отключение GameDVR и GameBar...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR", 
                "AppCaptureEnabled", 0, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\GameBar", 
                "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);

            // === 5. ОТКЛЮЧЕНИЕ УСКОРЕНИЯ МЫШИ ===
            logCallback?.Invoke("🔧 Отключение ускорения мыши...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Mouse", 
                "MouseSpeed", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Mouse", 
                "MouseThreshold1", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Mouse", 
                "MouseThreshold2", "0", RegistryValueKind.String);

            // === 6. ОТКЛЮЧЕНИЕ ВИЗУАЛЬНЫХ ЭФФЕКТОВ ===
            logCallback?.Invoke("🔧 Отключение визуальных эффектов...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", 
                "VisualFXSetting", 2, RegistryValueKind.DWord);

            // === 7. ОТКЛЮЧЕНИЕ АНИМАЦИЙ ОКОН ===
            logCallback?.Invoke("🔧 Отключение анимаций окон...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Desktop", 
                "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);

            // === 8. ОТКЛЮЧЕНИЕ ПРОЗРАЧНОСТИ ===
            logCallback?.Invoke("🔧 Отключение прозрачности окон...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", 
                "EnableTransparency", 0, RegistryValueKind.DWord);

            // === 9. ОТКЛЮЧЕНИЕ ТЕЛЕМЕТРИИ ===
            logCallback?.Invoke("🔧 Отключение телеметрии...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", 
                "AllowTelemetry", 0, RegistryValueKind.DWord);

            // === 10. ОТКЛЮЧЕНИЕ ОБНОВЛЕНИЙ (только для хардкорного режима) ===
            logCallback?.Invoke("🔧 Отключение автоматических обновлений...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", 
                "NoAutoUpdate", 1, RegistryValueKind.DWord);

            // === 11. МАКСИМАЛЬНАЯ СХЕМА ПИТАНИЯ ===
            logCallback?.Invoke("🔧 Включение схемы питания 'Максимальная производительность'...");
            RunPowerShellCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", logCallback);

            // === 12. ОТКЛЮЧЕНИЕ HPET (ВЫСОКОТОЧНЫЙ ТАЙМЕР) ===
            logCallback?.Invoke("🔧 Отключение HPET...");
            RunCommand("bcdedit /set useplatformclock false", logCallback);
            RunCommand("bcdedit /set disabledynamictick yes", logCallback);

            // === 13. ОТКЛЮЧЕНИЕ ЗАЩИТНИКА WINDOWS ===
            logCallback?.Invoke("🔧 Отключение Защитника Windows...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Policies\Microsoft\Windows Defender", 
                "DisableAntiSpyware", 1, RegistryValueKind.DWord);

            // === 14. ОТКЛЮЧЕНИЕ БРАНДМАУЭРА ===
            logCallback?.Invoke("🔧 Отключение брандмауэра...");
            RunCommand("netsh advfirewall set allprofiles state off", logCallback);

            // === 15. ОПТИМИЗАЦИЯ ФАЙЛОВОЙ СИСТЕМЫ NTFS ===
            logCallback?.Invoke("🔧 Оптимизация NTFS...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\FileSystem", 
                "NtfsDisableLastAccessUpdate", 1, RegistryValueKind.DWord);

            // === 16. ОТКЛЮЧЕНИЕ ПАРКОВКИ ЯДЕР CPU ===
            logCallback?.Invoke("🔧 Отключение парковки ядер CPU...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583", 
                "Attributes", 2, RegistryValueKind.DWord);
            RunPowerShellCommand("powercfg -setacvalueindex scheme_current sub_processor CPMAXCORES 100", logCallback);
            RunPowerShellCommand("powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100", logCallback);

            // === 17. ОТКЛЮЧЕНИЕ SPECTRE/MELTDOWN ЗАЩИТЫ ===
            logCallback?.Invoke("🔧 Отключение Spectre/Meltdown защиты...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", 
                "FeatureSettingsOverride", 3, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", 
                "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);

            // === 18. ОТКЛЮЧЕНИЕ IPV6 ===
            logCallback?.Invoke("🔧 Отключение IPv6...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", 
                "DisabledComponents", 0xFF, RegistryValueKind.DWord);

            logCallback?.Invoke("✅ Жесткая оптимизация завершена!");
        }

        private static void ApplySoftOptimization(Action<string> logCallback)
        {
            logCallback?.Invoke("🔧 Включение Игрового режима...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\GameBar", 
                "AllowAutoGameMode", 1, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\GameBar", 
                "AutoGameModeEnabled", 1, RegistryValueKind.DWord);

            logCallback?.Invoke("🔧 Отключение телеметрии...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", 
                "AllowTelemetry", 0, RegistryValueKind.DWord);

            logCallback?.Invoke("🔧 Оптимизация NTFS...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\FileSystem", 
                "NtfsDisableLastAccessUpdate", 1, RegistryValueKind.DWord);

            logCallback?.Invoke("🔧 Оптимизация приоритета процессов...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, 
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", 
                "Win32PrioritySeparation", 26, RegistryValueKind.DWord);

            logCallback?.Invoke("🔧 Включение схемы 'Сбалансированная'...");
            RunPowerShellCommand("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", logCallback);

            logCallback?.Invoke("✅ Мягкая оптимизация завершена!");
        }

        private static void ApplyVisualOptimization(Action<string> logCallback)
        {
            logCallback?.Invoke("🔧 Включение визуальных эффектов...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", 
                "VisualFXSetting", 1, RegistryValueKind.DWord);
            
            logCallback?.Invoke("🔧 Включение ClearType...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Desktop", 
                "FontSmoothing", "2", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Control Panel\Desktop", 
                "FontSmoothingType", 2, RegistryValueKind.DWord);

            logCallback?.Invoke("🔧 Включение прозрачности окон...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, 
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", 
                "EnableTransparency", 1, RegistryValueKind.DWord);

            logCallback?.Invoke("✅ Визуальная оптимизация завершена!");
        }

        // ============================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================================

        private static void RunCommand(string command, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ Ошибка выполнения: {command} - {ex.Message}");
            }
        }

        private static void RunPowerShellCommand(string command, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{command}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ Ошибка PowerShell: {command} - {ex.Message}");
            }
        }
    }
}