using System;
using System.IO;
using System.Collections.Generic;

namespace GameOptimizer.Core
{
    public static class SystemCleaner
    {
        private static long _totalFreedSpace = 0;
        private static int _totalDeletedFiles = 0;
        private static int _totalDeletedFolders = 0;

        public static void RunDeepCleanup(Action<string> log)
        {
            _totalFreedSpace = 0;
            _totalDeletedFiles = 0;
            _totalDeletedFolders = 0;

            log("🧹 Запуск глубокой очистки системы...");
            log("📋 Сканирование временных файлов и системного мусора...");

            // Список безопасных директорий для очистки
            List<string> targetDirectories = new List<string>
            {
                // Временные файлы пользователя
                Path.GetTempPath(),
                
                // Системные временные файлы
                @"C:\Windows\Temp",
                
                // Кэш Prefetch (ускоряет загрузку программ)
                @"C:\Windows\Prefetch",
                
                // Кэш обновлений Windows (после установки обновлений уже не нужен)
                @"C:\Windows\SoftwareDistribution\Download",
                
                // Кэш иконок и эскизов (исправляет глюки проводника)
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    @"Microsoft\Windows\Explorer"
                ),
                
                // Отчеты об ошибках Windows (WER)
                @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue",
                
                // Кэш шейдеров DirectX
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "D3DSCache"
                ),
                
                // Кэш шейдеров DirectX (альтернативная папка)
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DXCache"
                ),
                
                // Кэш браузера Edge (если используете)
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\User Data\Default\Cache"
                ),
                
                // Временные файлы Internet Explorer
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
                    "Content.IE5"
                ),
                
                // Корзина (только если запущено от администратора)
                // @"C:\$Recycle.Bin"
            };

            // Очищаем каждую папку
            foreach (string dir in targetDirectories)
            {
                if (Directory.Exists(dir))
                {
                    log($"📁 Обработка: {dir}...");
                    CleanDirectory(dir);
                }
                else
                {
                    log($"ℹ️ Папка не найдена: {dir}");
                }
            }

            // Очищаем временные файлы Internet Explorer (дополнительно)
            CleanIETempFiles(log);

            // Выводим итоги
            double mbFreed = Math.Round((double)_totalFreedSpace / (1024 * 1024), 2);
            double gbFreed = Math.Round(mbFreed / 1024, 2);

            log("");
            log("✅ ==================== ОЧИСТКА ЗАВЕРШЕНА ====================");
            log($"📊 Удалено файлов: {_totalDeletedFiles:N0}");
            log($"📁 Удалено папок: {_totalDeletedFolders:N0}");
            
            if (gbFreed >= 1)
                log($"💾 Освобождено места: {gbFreed:F2} GB");
            else
                log($"💾 Освобождено места: {mbFreed:F2} MB");
            
            log("ℹ️ Рекомендуется перезагрузить ПК для полного эффекта.");
            log("============================================================");
        }

        private static void CleanDirectory(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);

                // Пропускаем, если папка не существует
                if (!di.Exists) return;

                // 1. Удаляем файлы
                try
                {
                    foreach (FileInfo file in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            long size = file.Length;
                            file.Delete();
                            _totalFreedSpace += size;
                            _totalDeletedFiles++;
                        }
                        catch (Exception)
                        {
                            // Файл занят - пропускаем
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ошибка доступа к папке - пропускаем
                }

                // 2. Удаляем подпапки (рекурсивно)
                try
                {
                    foreach (DirectoryInfo subDir in di.GetDirectories())
                    {
                        try
                        {
                            subDir.Delete(true);
                            _totalDeletedFolders++;
                        }
                        catch (Exception)
                        {
                            // Папка занята - пропускаем
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ошибка доступа - пропускаем
                }
            }
            catch (Exception)
            {
                // Папка недоступна - пропускаем
            }
        }

        private static void CleanIETempFiles(Action<string> log)
        {
            try
            {
                // Очищаем временные файлы Internet Explorer через COM (если доступно)
                string ieCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
                    "Content.IE5"
                );

                if (Directory.Exists(ieCachePath))
                {
                    CleanDirectory(ieCachePath);
                    log("🧹 Очистка кэша Internet Explorer...");
                }
            }
            catch
            {
                // Пропускаем ошибки
            }
        }

        /// <summary>
        /// Быстрая очистка только временных файлов (без Prefetch и кэша обновлений)
        /// </summary>
        public static void QuickCleanup(Action<string> log)
        {
            log("⚡ Быстрая очистка временных файлов...");

            string[] quickPaths = new string[]
            {
                Path.GetTempPath(),
                @"C:\Windows\Temp",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp"
                )
            };

            foreach (string path in quickPaths)
            {
                if (Directory.Exists(path))
                {
                    CleanDirectory(path);
                }
            }

            double mbFreed = Math.Round((double)_totalFreedSpace / (1024 * 1024), 2);
            log($"✅ Быстрая очистка завершена! Освобождено: {mbFreed} MB");
        }

        /// <summary>
        /// Очистка только кэша шейдеров
        /// </summary>
        public static void CleanShaderCache(Action<string> log)
        {
            log("🎮 Очистка кэша шейдеров...");

            string[] shaderPaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "GLCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "GLCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NVIDIA Corporation", "NV_Cache")
            };

            foreach (string path in shaderPaths)
            {
                if (Directory.Exists(path))
                {
                    CleanDirectory(path);
                }
            }

            double mbFreed = Math.Round((double)_totalFreedSpace / (1024 * 1024), 2);
            log($"✅ Очистка кэша шейдеров завершена! Освобождено: {mbFreed} MB");
        }
    }
}