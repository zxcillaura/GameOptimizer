using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Cleaners
{
    /// <summary>
    /// Очистка кэша шейдеров (D3D, DX, NVIDIA, AMD)
    /// </summary>
    public class ShaderCacheCleaner : ICleaner
    {
        public string Name => "Очистка кэша шейдеров";
        public string Description => "Удаляет папки D3DSCache, DXCache, NVIDIA DXCache и AMD DXCache для устранения статтеров и фризов";

        public async Task CleanAsync(CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                string[] shaderPaths = {
                    Path.Combine(localAppData, "D3DSCache"),
                    Path.Combine(localAppData, "DXCache"),
                    Path.Combine(localAppData, "NVIDIA", "DXCache"),
                    Path.Combine(localAppData, "NVIDIA", "GLCache"),
                    Path.Combine(localAppData, "AMD", "DXCache"),
                    Path.Combine(localAppData, "AMD", "GLCache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NVIDIA Corporation", "NV_Cache")
                };

                foreach (var path in shaderPaths)
                {
                    if (token.IsCancellationRequested) break;
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