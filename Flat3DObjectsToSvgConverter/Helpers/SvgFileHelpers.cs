using SvgLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Helpers
{
    public static class SvgFileHelpers
    {
        public static SvgDocument ParseSvgFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return ParseSvgString(content);
        }

        public static SvgDocument ParseSvgString(string svg)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svg);

            return new SvgDocument(xmlDocument, xmlDocument.DocumentElement);
        }
    }
}
