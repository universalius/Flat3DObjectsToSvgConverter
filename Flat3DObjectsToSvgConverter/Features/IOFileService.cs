using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Features
{
    public class IOFileService
    {
        private IOSettings _settings;
        private string _resultsFolderName;

        public IOFileService(IOptions<IOSettings> options)
        {
            _settings = options.Value;
            _resultsFolderName = $"{_settings.ObjFileName} {DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss")}";
        }

        public async Task<string[]> ReadObjFile()
        {
            return await File.ReadAllLinesAsync(GetObjFilePath());
        }

        public void SaveSvg(string fileName, string svg)
        {
            var resultsPath = GetResultsFolderPath();
            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }

            File.WriteAllText(Path.Combine(resultsPath, $"{_settings.ObjFileName}_{fileName}.svg"), svg);
        }

        public void CopyObjFile()
        {
            File.Copy(GetObjFilePath(), Path.Combine(GetResultsFolderPath(), $"{_settings.ObjFileName}.obj"));
        }

        private string GetObjFilePath()
        {
            return Path.Combine(_settings.WorkingFolder, $"{_settings.ObjFileName}.obj");
        }

        private string GetResultsFolderPath()
        {
            return Path.Combine(_settings.WorkingFolder, _resultsFolderName);
        }
    }
}
