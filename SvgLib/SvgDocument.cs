using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SvgLib
{
    public sealed class SvgDocument : SvgContainer
    {
        public readonly XmlDocument _document;

        public SvgDocument(XmlDocument document, XmlElement element)
            : base(element)
        {
            _document = document;
        }

        public static SvgDocument Create()
        {
            var document = new XmlDocument();
            var rootElement = document.CreateElement("svg");
            document.AppendChild(rootElement);
            rootElement.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
            return new SvgDocument(document, rootElement);
        }

        public void Save(Stream stream) => _document.Save(stream);

        public double X
        {
            get => Element.GetAttribute("x", SvgDefaults.Attributes.Position.X);
            set => Element.SetAttribute("x", value);
        }

        public double Y
        {
            get => Element.GetAttribute("y", SvgDefaults.Attributes.Position.Y);
            set => Element.SetAttribute("y", value);
        }

        public double Width
        {
            get => double.Parse(ReplaceUnits("width"));
            set => Element.SetAttribute("width", $"{value}{Units}");
        }

        public double Height
        {
            get => double.Parse(ReplaceUnits("height"));
            set => Element.SetAttribute("height", $"{value}{Units}");
        }

        public SvgViewBox ViewBox
        {
            get => Element.GetAttribute("viewBox", new SvgViewBox());
            set => Element.SetAttribute("viewBox", value.ToString());
        }

        public string Units { get; set; }

        private string ReplaceUnits(string attribute)
        {
            string value = Element.GetAttribute(attribute).ToLowerInvariant();

            var units = new List<string> { "px", "mm", "m", "pt", "cm" };
            units.ForEach(unit =>
            {
                value = value.Replace(unit, string.Empty);
            });


            return value.Trim();
        }

        public SvgDocument Clone(bool deep = false)
        {
            return new SvgDocument(_document.CloneNode(deep) as XmlDocument, Element.CloneNode(deep) as XmlElement);
        }
    }
}
