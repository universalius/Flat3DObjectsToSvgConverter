using System.Threading.Tasks;

namespace Plain3DObjectsToSvgConverter.Services
{
    public interface IJavaScriptService
    {
        Task<string> GetCompactedSvg(string inputSvg);
    }
}