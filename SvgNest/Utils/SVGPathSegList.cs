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
            PathSegList = ParsePath(_pathElement.GetAttribute("d")).ToList();

            //// Use a MutationObserver to catch changes to the path's "d" attribute.
            //_mutationObserverConfig = { "attributes": true, "attributeFilter": ["d"] };
            //_pathElementMutationObserver = new MutationObserver(_updateListFromPathMutations.bind(this));
            //_pathElementMutationObserver.observe(_pathElement, _mutationObserverConfig);
        }

        public List<SVGPathSeg> PathSegList { get; set; }

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

        //window.SVGPathSegList.prototype.appendItem = function(newItem)
        //{
        //    this._checkPathSynchronizedToList();

        //    if (newItem._owningPathSegList)
        //    {
        //        // SVG2 spec says to make a copy.
        //        newItem = newItem.clone();
        //    }
        //    this._list.push(newItem);
        //    newItem._owningPathSegList = this;
        //    // TODO: Optimize this to just append to the existing attribute.
        //    this._writeListToPath();
        //    return newItem;
        //}

        public SVGPathSeg AppendItem(SVGPathSeg newItem)
        {
            if (newItem._owningPathSegList != null)
            {
                // SVG2 spec says to make a copy.
                newItem = newItem.Clone();
            }
            PathSegList.Add(newItem);
            newItem._owningPathSegList = this;
            // TODO: Optimize this to just append to the existing attribute.
            this._writeListToPath();
            return newItem;
        }

        // Serialize the list and update the path's 'd' attribute.
        //window.SVGPathSegList.prototype._writeListToPath = function()
        //{
        //    this._pathElementMutationObserver.disconnect();
        //    this._pathElement.setAttribute("d", window.SVGPathSegList._pathSegArrayAsString(this._list));
        //    this._pathElementMutationObserver.observe(this._pathElement, this._mutationObserverConfig);
        //}

        // Serialize the list and update the path's 'd' attribute.
        private void _writeListToPath()
        {
            _pathElement.SetAttribute("d", _pathSegArrayAsString(PathSegList));
        }

        //window.SVGPathSegList._pathSegArrayAsString = function(pathSegArray)
        //{
        //    var string = "";
        //    var first = true;
        //    pathSegArray.forEach(function(pathSeg) {
        //        if (first)
        //        {
        //            first = false;
        //            string += pathSeg._asPathString();
        //        }
        //        else
        //        {
        //            string += " " + pathSeg._asPathString();
        //        }
        //    });
        //    return string;
        //}

        private string _pathSegArrayAsString(List<SVGPathSeg> pathSegArray)
        {
            var segments = pathSegArray.Select(pathSeg => pathSeg._asPathString()).ToArray();
            return string.Join(" ", segments);
        }
    }
}
