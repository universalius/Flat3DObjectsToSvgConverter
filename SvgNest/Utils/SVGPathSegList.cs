using SvgNest.Models.SVGPathSeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SvgNest.Utils
{
    public class SVGPathSegList
    {
        private XmlElement _pathElement;

        public SVGPathSegList(XmlElement pathElement)
        {
            _pathElement = pathElement;
            PathSegList = ParsePath(_pathElement.GetAttribute("d"));

            //// Use a MutationObserver to catch changes to the path's "d" attribute.
            //_mutationObserverConfig = { "attributes": true, "attributeFilter": ["d"] };
            //_pathElementMutationObserver = new MutationObserver(_updateListFromPathMutations.bind(this));
            //_pathElementMutationObserver.observe(_pathElement, _mutationObserverConfig);
        }

        public SVGPathSeg[] PathSegList { get; set; }

        // This closely follows SVGPathParser::parsePath from Source/core/svg/SVGPathParser.cpp.
        private SVGPathSeg[] ParsePath(string pathD)
        {
            if (string.IsNullOrEmpty(pathD) || !pathD.Any())
                return new SVGPathSeg[0];

            var builder = new PathSegListBuilder();
            var source = new PathSegListSource(pathD);

            if (!source.InitialCommandIsMoveTo())
                return new SVGPathSeg[0];
            while (source.HasMoreData())
            {
                var pathSeg = source.ParseSegment(this);
                if (pathSeg == null)
                    return new SVGPathSeg[0];
                builder.appendSegment(pathSeg);
            }

            return builder.PathSegList.ToArray();
        }
    }
}
