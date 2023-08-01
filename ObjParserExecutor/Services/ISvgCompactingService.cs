using System.Threading.Tasks;

namespace Plain3DObjectsToSvgConverter.Services
{
    public interface ISvgCompactingService
    {
        Task<string> Compact(string inputSvg);
    }
}