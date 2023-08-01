using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Services
{
    public interface ISvgCompactingService
    {
        Task<string> Compact(string inputSvg);
    }
}