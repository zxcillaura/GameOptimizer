using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Cleaners
{
    /// <summary>
    /// Очистка логов и дампов игр
    /// </summary>
    public class GameLogCleaner : ICleaner
    {
        public string Name => "Очистка логов игр";
        public string Description => "Удаляет временные логи, дампы ошибок и кэш некоторых игр";

        public async Task CleanAsync(CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                string[][] logPaths = {
                    new[] { localAppData, "CrashDumps" },
                    new[] { localAppData, "Temp", "Unity" },
                    new[] { appData, "..", "Local", "Temp", "Unity" },
                    new[] { appData, "..", "Local", "Steam", "htmlcache" },
                    new[] { appData, "..", "Local", "Steam", "steamapps", "shadercache" },
                    // Epic Games
                    new[] { localAppData, "Epic Games", "Launcher", "Saved", "Logs" },
                    // Valorant
                    new[] { localAppData, "VALORANT", "Saved", "Logs" },
                    new[] { localAppData, "Riot Games", "VALORANT", "live", "ShooterGame", "Saved", "Logs" },
                    // CS2
                    new[] { localAppData, "Counter-Strike Global Offensive", "game", "csgo", "logs" },
                    // Dota 2
                    new[] { localAppData, "Dota 2", "game", "dota", "logs" }
                };

                foreach (var parts in logPaths)
                {
                    if (token.IsCancellationRequested) break;
                    string path = Path.Combine(parts);
                    if (Directory.Exists(path))
                    {
                        try 
                        { 
                            // Удаляем только файлы, но не папки (чтобы не сломать структуру)
                            foreach (var file in Directory.GetFiles(path))
                            {
                                if (token.IsCancellationRequested) break;
                                try { File.Delete(file); }
                                catch { /* Игнорируем ошибки доступа */ }
                            }
                        }
                        catch { /* Игнорируем ошибки доступа */ }
                    }
                }
            }, token);
        }
    }
}