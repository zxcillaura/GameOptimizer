using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Cleaners
{
    /// <summary>
    /// Очистка временных файлов Windows
    /// </summary>
    public class TempCleaner : ICleaner
    {
        public string Name => "Очистка временных файлов";
        public string Description => "Удаляет все файлы из папок %TEMP% и C:\\Windows\\Temp";

        public async Task CleanAsync(CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                string tempPath = Path.GetTempPath();
                DeleteDirectoryContents(tempPath, token);

                string windowsTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp");
                DeleteDirectoryContents(windowsTemp, token);
            }, token);
        }

        private void DeleteDirectoryContents(string path, CancellationToken token)
        {
            if (!Directory.Exists(path)) return;

            // Удаляем файлы
            foreach (var file in Directory.GetFiles(path))
            {
                if (token.IsCancellationRequested) break;
                try { File.Delete(file); }
                catch { /* Игнорируем ошибки доступа */ }
            }

            // Удаляем папки
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (token.IsCancellationRequested) break;
                try { Directory.Delete(dir, true); }
                catch { /* Игнорируем ошибки доступа */ }
            }
        }
    }
}