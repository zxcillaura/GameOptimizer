using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Managers
{
    /// <summary>
    /// Статический класс для выполнения системных команд
    /// </summary>
    public static class CommandExecutor
    {
        /// <summary>
        /// Выполнить команду асинхронно
        /// </summary>
        /// <param name="command">Команда (например, "powercfg")</param>
        /// <param name="arguments">Аргументы</param>
        /// <param name="runAsAdmin">Запускать с правами администратора</param>
        /// <returns>true, если команда завершилась успешно (код 0)</returns>
        public static async Task<bool> ExecuteAsync(string command, string arguments, bool runAsAdmin = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                if (runAsAdmin)
                {
                    psi.Verb = "runas";
                    psi.UseShellExecute = true; // для runas обязательно
                    psi.RedirectStandardOutput = false;
                    psi.RedirectStandardError = false;
                }

                using var process = new Process { StartInfo = psi };
                process.Start();

                if (!runAsAdmin)
                {
                    // Читаем вывод для логирования
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    // Можно записать вывод в лог, если нужно
                    return process.ExitCode == 0;
                }
                else
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Выполнить команду и вернуть её вывод (stdout)
        /// </summary>
        public static async Task<string> GetOutputAsync(string command, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = new Process { StartInfo = psi };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}