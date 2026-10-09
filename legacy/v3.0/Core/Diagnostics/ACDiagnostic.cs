using System;
using System.Diagnostics;
using Microsoft.Win32;
using GameOptimizer.Core.Models;

namespace GameOptimizer.Core.Diagnostics
{
    /// <summary>
    /// Диагностика для античитов (TPM, Secure Boot, VBS, Hyper-V)
    /// </summary>
    public class ACDiagnostic
    {
        public CheckResult CheckTPM()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TPM");
                if (key != null)
                {
                    object val = key.GetValue("Enabled");
                    if (val != null && Convert.ToInt32(val) == 1)
                    {
                        return new CheckResult { Status = CheckStatus.Ok, Message = "✅ TPM 2.0 включен", Instruction = "TPM работает корректно" };
                    }
                }
                
                return new CheckResult { Status = CheckStatus.Error, Message = "❌ TPM 2.0 выключен", Instruction = "Для включения TPM 2.0:\n\n1. Перезагрузите компьютер и войдите в BIOS/UEFI\n2. Найдите раздел Security (Безопасность) или Advanced (Дополнительно)\n3. Для Intel найдите 'Intel Platform Trust Technology (PTT)' и включите (Enabled)\n4. Для AMD найдите 'AMD fTPM' и включите (Enabled)\n5. Сохраните настройки (F10) и перезагрузитесь\n6. После загрузки Windows проверка обновится автоматически" };
            }
            catch (Exception ex)
            {
                return new CheckResult { Status = CheckStatus.Error, Message = $"⚠️ Не удалось проверить TPM: {ex.Message}", Instruction = "Возможно, у вас нет TPM модуля." };
            }
        }

        public CheckResult CheckSecureBoot()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                if (key != null)
                {
                    object val = key.GetValue("UEFISecureBootEnabled");
                    if (val != null && Convert.ToInt32(val) == 1)
                    {
                        return new CheckResult { Status = CheckStatus.Ok, Message = "✅ Secure Boot включен", Instruction = "Secure Boot работает корректно" };
                    }
                }
                
                return new CheckResult { Status = CheckStatus.Error, Message = "❌ Secure Boot выключен", Instruction = "Для включения Secure Boot:\n\n1. Перезагрузитесь и войдите в BIOS/UEFI\n2. Найдите раздел Boot (Загрузка) или Security (Безопасность)\n3. Найдите 'Secure Boot' и переведите в Enabled (Включено)\n4. Сохраните настройки (F10) и перезагрузитесь" };
            }
            catch (Exception ex)
            {
                return new CheckResult { Status = CheckStatus.Error, Message = $"⚠️ Не удалось проверить Secure Boot: {ex.Message}", Instruction = "Ваша система может не поддерживать Secure Boot." };
            }
        }

        public CheckResult CheckVBS()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
                if (key != null)
                {
                    object val = key.GetValue("EnableVirtualizationBasedSecurity");
                    if (val != null && Convert.ToInt32(val) == 1)
                    {
                        return new CheckResult { Status = CheckStatus.Ok, Message = "✅ VBS включена", Instruction = "VBS включена" };
                    }
                }
                
                return new CheckResult { Status = CheckStatus.Warning, Message = "⚠️ VBS выключена (может потребоваться для некоторых античитов)", Instruction = "Для включения VBS:\n\n1. Откройте 'Безопасность Windows'\n2. Выберите 'Безопасность устройства' -> 'Изоляция ядра'\n3. Включите 'Целостность памяти'\n4. Перезагрузите компьютер" };
            }
            catch (Exception ex)
            {
                return new CheckResult { Status = CheckStatus.Warning, Message = $"⚠️ Не удалось проверить VBS: {ex.Message}", Instruction = "Не удалось прочитать настройки VBS." };
            }
        }

        public CheckResult CheckHyperV()
        {
            try
            {
                // ПОЛНОСТЬЮ ВЫРЕЗАЛИ АСИНХРОНЩИНУ И МЕРТВЫЕ БЛОКИРОВКИ, ЧТОБЫ ОКНО ЗАПУСКАЛОСЬ СРАЗУ
                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = "/enum",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                bool hvEnabled = output.Contains("hypervisorlaunchtype    Auto") || 
                                 output.Contains("hypervisorlaunchtype    On") ||
                                 output.Contains("hypervisorlaunchtype    auto");
                
                if (hvEnabled)
                {
                    return new CheckResult { Status = CheckStatus.Ok, Message = "✅ Hyper-V включен", Instruction = "Hyper-V включен" };
                }
                else
                {
                    return new CheckResult { Status = CheckStatus.Warning, Message = "⚠️ Hyper-V выключен", Instruction = "Для включения Hyper-V выполните команду: bcdedit /set hypervisorlaunchtype auto и перезагрузитесь" };
                }
            }
            catch (Exception ex)
            {
                return new CheckResult { Status = CheckStatus.Warning, Message = $"⚠️ Не удалось проверить Hyper-V: {ex.Message}", Instruction = "Ошибка проверки" };
            }
        }

        public (CheckResult tpm, CheckResult secureBoot, CheckResult vbs, CheckResult hyperV) CheckAll()
        {
            return (CheckTPM(), CheckSecureBoot(), CheckVBS(), CheckHyperV());
        }
    }
}