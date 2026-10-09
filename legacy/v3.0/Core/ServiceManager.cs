using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace GameOptimizer.Core
{
    public static class ServiceManager
    {
        /// <summary>
        /// Проверяет, запущена ли служба в данный момент
        /// </summary>
        public static bool IsServiceRunning(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false; // Если службы нет (не установлена)
            }
        }

        /// <summary>
        /// Включает или отключает службу античита
        /// </summary>
        public static void ToggleService(string serviceName, bool start, Action<string> log)
        {
            string actionText = start ? "Запуск" : "Остановка";
            log($"-> {actionText} службы: {serviceName}...");

            try
            {
                // Используем sc.exe (Service Control) через скрытую консоль
                string command = start ? $"sc start {serviceName}" : $"sc stop {serviceName}";
                
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas" // Запрос прав администратора
                };

                Process p = Process.Start(psi);
                p?.WaitForExit();

                log($"[УСПЕХ] Команда ({actionText}) для {serviceName} выполнена.");
                
                if (serviceName.ToLower() == "vgc" && start)
                {
                    log("ВНИМАНИЕ: Для полной активации Vanguard может потребоваться перезагрузка ПК.");
                }
            }
            catch (Exception ex)
            {
                log($"[ОШИБКА] Не удалось изменить состояние службы: {ex.Message}");
            }
        }
    }
}