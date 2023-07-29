using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using SvgNest;

namespace Plain3DObjectsToSvgConverter.Services
{
    public class JavaScriptService : IJavaScriptService
    {
        private readonly INodeServices _nodeServices;
        private readonly string _scriptFolder;

        public JavaScriptService([FromServices] INodeServices nodeServices) : this(nodeServices, ".")
        {

        }

        public JavaScriptService([FromServices] INodeServices nodeServices, string scriptFolder)
        {
            _nodeServices = nodeServices;
            _scriptFolder = scriptFolder;
        }

        public async Task<string> GetCompactedSvg(string inputSvg)
        {
            //string path = Path.Combine(_scriptFolder, "./Scripts/getCompactedSvg");
            //var result = await _nodeServices.InvokeAsync<string>(path, inputSvg);

            var svgNest = new SvgNest.SvgNest();
            svgNest.parsesvg(inputSvg);

            return null;
        }
    }
}
