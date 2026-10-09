using System.Threading;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Cleaners
{
    /// <summary>
    /// Интерфейс для всех чистильщиков системы
    /// </summary>
    public interface ICleaner
    {
        string Name { get; }
        string Description { get; }
        Task CleanAsync(CancellationToken token = default);
    }
}