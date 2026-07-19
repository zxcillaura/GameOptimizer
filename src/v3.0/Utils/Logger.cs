using System;
using System.IO;

namespace GameOptimizer.Utils
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogError(string message)
        {
            Log("ERROR", message);
        }

        public static void LogError(string message, Exception ex)
        {
            Log("ERROR", $"{message} | Исключение: {ex.Message}\nТрассировка стека:\n{ex.StackTrace}");
        }

        private static void Log(string level, string message)
        {
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                Console.WriteLine(logLine); // Вывод в консоль отладки
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Игнорируем ошибки записи логов, чтобы приложение не упало в случае проблем с правами
            }
        }
    }
}