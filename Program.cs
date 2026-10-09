using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using GameOptimizer.Cli;
using GameOptimizer.UI;
using GameOptimizer.Utils;

namespace GameOptimizer
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // ── headless / CLI mode: GAMEOPTIMIZ1.0.exe /scan | /apply ... ──
            // "/tab:N" is a GUI switch (open on a specific page), not a CLI command.
            if (args != null && args.Length > 0 &&
                (args[0].StartsWith("/") || args[0].StartsWith("-")) &&
                !args[0].StartsWith("/tab", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = CliRunner.Run(args);
                return;
            }

            // ── single instance ────────────────────────────────────────────
            // Two copies would both watch for cs2/dota2, both hold the 0.5 ms timer
            // resolution and both close background processes — the second one would
            // silently sabotage the first.
            bool firstInstance = true;
            System.Threading.Mutex single = null;
            try
            {
                single = new System.Threading.Mutex(true, @"Global\GAMEOPTIMIZ1.0_SingleInstance", out firstInstance);
            }
            catch (Exception)
            {
                firstInstance = true;   // cannot verify -> do not block the user
            }

            if (!firstInstance)
            {
                MessageBox.Show(
                    "GAMEOPTIMIZ 4.0 уже запущен.\n\n" +
                    "Второй экземпляр будет мешать первому: двойное слежение за играми, " +
                    "двойная очистка памяти и конфликт за таймер 0.5 мс.\n\n" +
                    "Переключись на уже открытое окно.",
                    "GAMEOPTIMIZ 4.0", MessageBoxButtons.OK, MessageBoxIcon.Information);
                single?.Dispose();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ShowFatalError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex) ShowFatalError(ex);
            };

            try
            {
                if (!AdminChecker.IsRunningAsAdmin())
                {
                    var result = MessageBox.Show(
                        "GAMEOPTIMIZ 4.0 требует прав администратора для системных твиков.\n\n" +
                        "Нажми ДА, чтобы перезапустить от имени администратора.\n" +
                        "(Только просмотр и оценка железа работают и без прав.)",
                        "Запуск от администратора", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes) return;

                    var fileName = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(fileName))
                        throw new InvalidOperationException("Не удалось определить путь к GAMEOPTIMIZ1.0.exe.");

                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName         = fileName,
                        UseShellExecute  = true,
                        Verb             = "runas",
                        WorkingDirectory = AppContext.BaseDirectory
                    });
                    return;
                }

                Application.Run(new MainForm(StartTab(args)));
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
            finally
            {
                try { single?.ReleaseMutex(); } catch { }
                single?.Dispose();
            }
        }

        /// <summary>Reads "/tab:N" so the app can open directly on a given page.</summary>
        private static int StartTab(string[] args)
        {
            try
            {
                var a = args?.FirstOrDefault(x => x.StartsWith("/tab:", StringComparison.OrdinalIgnoreCase));
                if (a != null && int.TryParse(a.Substring(5), out int n)) return n;
            }
            catch { }
            return 0;
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "GAMEOPTIMIZ-startup-error.txt");
                File.WriteAllText(path, $"GAMEOPTIMIZ 4.0 startup error\r\n{DateTime.Now:O}\r\n\r\n{ex}");
                MessageBox.Show($"GAMEOPTIMIZ 4.0 не смог запуститься.\n\nЛог:\n{path}\n\nОшибка:\n{ex.Message}",
                    "Ошибка запуска GAMEOPTIMIZ 4.0", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
