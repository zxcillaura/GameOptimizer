using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Cleaners
{
    /// <summary>
    /// Очистка кэша браузеров (Chrome, Edge)
    /// </summary>
    public class BrowserCacheCleaner : ICleaner
    {
        public string Name => "Очистка кэша браузеров";
        public string Description => "Удаляет кэш и временные файлы из Google Chrome и Microsoft Edge";

        public async Task CleanAsync(CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // Пути к кэшу браузеров
                string[][] browserPaths = {
                    new[] { localAppData, "Google", "Chrome", "User Data", "Default", "Cache" },
                    new[] { localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache" },
                    new[] { localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache" },
                    new[] { localAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache" },
                    new[] { localAppData, "Google", "Chrome", "User Data", "Default", "Service Worker", "CacheStorage" },
                    new[] { localAppData, "Microsoft", "Edge", "User Data", "Default", "Service Worker", "CacheStorage" }
                };

                foreach (var parts in browserPaths)
                {
                    if (token.IsCancellationRequested) break;
                    string path = Path.Combine(parts);
                    if (Directory.Exists(path))
                    {
                        try { Directory.Delete(path, true); }
                        catch { /* Игнорируем ошибки доступа */ }
                    }
                }
            }, token);
        }
    }
}