using System.Xml;

namespace SvgLib
{
    public sealed class SvgGroup : SvgContainer
    {
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
