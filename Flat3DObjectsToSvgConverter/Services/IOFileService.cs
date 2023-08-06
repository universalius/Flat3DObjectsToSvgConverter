using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class IOFileService
    {
        private IOSettings _settings;
        private string _resultsFolderName;

        public IOFileService(IOptions<IOSettings> options)
        {
            _settings = options.Value;
            _resultsFolderName = $"{_settings.ObjFileName} {DateTime.Now.ToString().Replace(":", "-")}";
        }

        public async Task<string[]> ReadObjFile()
        {
            return await File.ReadAllLinesAsync(Path.Combine(_settings.WorkingFolder, $"{_settings.ObjFileName}.obj"));
        }

        public void SaveSvg(string fileName, string svg)
        {
            var resultsPath = Path.Combine(_settings.WorkingFolder, _resultsFolderName);
            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }

            File.WriteAllText(Path.Combine(resultsPath, $"{_settings.ObjFileName}_{fileName}.svg"), svg);
        }
    }
}
