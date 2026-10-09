using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    public static class GameOptimizer
    {
        /// <summary>
        /// Включение игрового режима Windows и планирования HAGS (Hardware-Accelerated GPU Scheduling)
        /// </summary>
        public static void EnableWindowsGameMode(Action<string> log)
        {
            log("-> Активация игрового режима Windows...");
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);

            log("-> Включение аппаратного ускорения GPU (HAGS)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
            
            log("[УСПЕХ] Системные игровые параметры настроены!");
        }

        /// <summary>
        /// Безопасная очистка кэша шейдеров видеокарт NVIDIA и AMD
        /// </summary>
        public static void ClearShaderCache(Action<string> log)
        {
            log("-> Поиск кэша шейдеров видеокарты...");
            int deletedFiles = 0;
            long savedSpace = 0;

            // Пути к кэшу NVIDIA и AMD
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] cachePaths = new string[]
            {
                Path.Combine(localAppData, @"NVIDIA\DXCache"),
                Path.Combine(localAppData, @"NVIDIA\GLCache"),
                Path.Combine(localAppData, @"NVIDIA\NV_Cache"),
                Path.Combine(localAppData, @"AMD\DxCache"),
                Path.Combine(localAppData, @"AMD\OglCache")
            };

            foreach (var path in cachePaths)
            {
                if (!Directory.Exists(path)) continue;

                log($"Очистка директории: {Path.GetFileName(path)}...");
                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        long size = file.Length;
                        file.Delete();
                        deletedFiles++;
                        savedSpace += size;
                    }
                    catch
                    {
                        // Файл занят игрой или системой — пропускаем без краша приложения
                    }
                }
            }

            double spaceMb = Math.Round((double)savedSpace / (1024 * 1024), 2);
            log($"[УСПЕХ] Очищено файлов: {deletedFiles}. Освобождено: {spaceMb} MB.");
        }

        /// <summary>
        /// Оптимизация запуска CS2 и Steam
        /// </summary>
        public static void OptimizeCS2(Action<string> log)
        {
            log("-> Настройка высокого приоритета процессора для cs2.exe...");
            string cs2CpuPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\cs2.exe\PerfOptions";
            // CpuPriorityClass = 3 (Высокий приоритет)
            RegistryManager.SetValue(RegistryHive.LocalMachine, cs2CpuPath, "CpuPriorityClass", 3, RegistryValueKind.DWord);

            log("-> Оптимизация параметров запуска Steam...");
            // Твик для снижения нагрузки самого Steam на процессор во время игры
            string steamPath = @"SOFTWARE\Valve\Steam";
            RegistryManager.SetValue(RegistryHive.CurrentUser, steamPath, "SmoothScroll", 0, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.CurrentUser, steamPath, "DWrite", 0, RegistryValueKind.DWord);

            log("[УСПЕХ] Настройки для CS2 и Steam успешно применены!");
            log("Рекомендуемые параметры запуска в Steam: -high -threads 12 -nojoy +cl_forcepreload 1");
        }
    }
}