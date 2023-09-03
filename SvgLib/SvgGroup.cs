using System.Xml;

namespace SvgLib
{
    public sealed class SvgGroup : SvgContainer
    {
        public SvgGroup(SvgDocument svgDocument) : base(null)
        {
            var element = svgDocument._document.CreateElement("g", svgDocument.Element.OwnerDocument.DocumentElement.NamespaceURI);
            Element = element;
        }


        public SvgGroup(XmlElement element)
            : base(element)
        {
        }

        internal static SvgGroup Create(XmlElement parent)
        {
            var element = parent.OwnerDocument.CreateElement("g", parent.OwnerDocument.DocumentElement.NamespaceURI);
            parent.AppendChild(element);
            return new SvgGroup(element);
        }
    }
}
