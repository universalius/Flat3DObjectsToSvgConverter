using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
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


            var a = new DoublePoint[] {
            new DoublePoint(1,1),
            new DoublePoint(2,2),
            new DoublePoint(3,3),
            };

            var b = a.ToList();
            b.RemoveAt(1);

            var c = b.ToList();
            c.Add(new DoublePoint(4, 4));

            var svgNest = new SvgNest.SvgNest();
            svgNest.parsesvg(inputSvg);
            await svgNest.start();

            File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\result_compacted.svg", svgNest.compactedSvgs.First().OuterXml);


            return null;
        }
    }
}
