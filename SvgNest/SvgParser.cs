using SvgLib;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using SvgNest.Utils;
using ClipperLib;
using Flat3DObjectsToSvgConverter.Common.Extensions;
using SvgNest.Models;
using SvgNest.Models.SVGPathSeg;

namespace SvgNest
{
    public class SvgParser
    {
        private CultureInfo culture = new CultureInfo("en-US", false);

        public SvgParser()
        {
            _conf = new SvgParserConfig
            {
                Tolerance = 2, // max bound for bezier->line segment conversion, in native SVG units
                ToleranceSvg = 0.005 // fudge factor for browser inaccuracy in SVG unit handling
            };
        }

        // the SVG document
        private SvgDocument _svg;

        // the top level SVG element of the SVG document
        private SvgDocument _svgRoot;

        private string[] _allowedElements = new string[] { "svg", "circle", "ellipse", "path", "polygon", "polyline", "rect", "line" };

        private SvgParserConfig _conf;

        public SvgDocument Load(string svgString)
        {
            if (string.IsNullOrEmpty(svgString))
            {
                throw new Exception("invalid SVG string");
            }

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svgString);

            _svgRoot = new SvgDocument(xmlDocument, xmlDocument.DocumentElement);

            return _svgRoot;
        }

        // use the utility functions in this class to prepare the svg for CAD-CAM/nest related operations
        public SvgDocument CleanInput()
        {
            // apply any transformations, so that all path positions etc will be in the same coordinate space
            ApplyTransform(_svgRoot.Element);

            // remove any g elements and bring all elements to the top level
            Flatten(_svgRoot.Element);

            // remove any non-contour elements like text
            Filter(_svgRoot.Element);

            // split any compound paths into individual path elements
            SplitCompoundPaths(_svgRoot.Element);

            return _svgRoot;
        }

        public SVGPathSeg[] PathToAbsolute(XmlElement path)
        {
            if (path == null || path.Name != "path")
            {
                throw new Exception("invalid path");
            }

            var seglist = (new SVGPathSegList(path)).PathSegList;
            double x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            for (var i = 0; i < seglist.Count(); i++)
            {
                var s = seglist[i];
                var command = s.PathSegTypeAsLetter;

                if (Regex.IsMatch(command, @"[MLHVCSQTA]"))
                {
                    x = s.X;
                    y = s.Y;
                }
                else
                {
                    //if ("x1" in s) x1 = x + s.x1;
                    //if ("x2" in s) x2 = x + s.x2;
                    //if ("y1" in s) y1 = y + s.y1;
                    //if ("y2" in s) y2 = y + s.y2;
                    //if ("x" in s) x += s.x;
                    //if ("y" in s) y += s.y;
                    switch (command)
                    {
                        //case "m": seglist.replaceItem(path.createSVGPathSegMovetoAbs(x, y), i); break;
                        //case "l": seglist.replaceItem(path.createSVGPathSegLinetoAbs(x, y), i); break;
                        //case "h": seglist.replaceItem(path.createSVGPathSegLinetoHorizontalAbs(x), i); break;
                        //case "v": seglist.replaceItem(path.createSVGPathSegLinetoVerticalAbs(y), i); break;
                        //case "c": seglist.replaceItem(path.createSVGPathSegCurvetoCubicAbs(x, y, x1, y1, x2, y2), i); break;
                        //case "s": seglist.replaceItem(path.createSVGPathSegCurvetoCubicSmoothAbs(x, y, x2, y2), i); break;
                        //case "q": seglist.replaceItem(path.createSVGPathSegCurvetoQuadraticAbs(x, y, x1, y1), i); break;
                        //case "t": seglist.replaceItem(path.createSVGPathSegCurvetoQuadraticSmoothAbs(x, y), i); break;
                        //case "a": seglist.replaceItem(path.createSVGPathSegArcAbs(x, y, s.r1, s.r2, s.angle, s.largeArcFlag, s.sweepFlag), i); break;
                        case "z":
                        case "Z":
                            x = x0;
                            y = y0;
                            break;
                    }
                }
                // Record the start of a subpath
                if (command == "M" || command == "m")
                {
                    x0 = x;
                    y0 = y;
                }
            }

            return seglist;
        }

        // takes an SVG transform string and returns corresponding SVGMatrix
        // from https://github.com/fontello/svgpath
        private Matrix TransformParse(string transformString)
        {
            var operations = new[]{
                "matrix",
                "scale",
                "rotate",
                "translate",
                "skewX",
                "skewY"
            };

            var CMD_SPLIT_RE = @"\s*(matrix|translate|scale|rotate|skewX|skewY)\s*\(\s*(.+?)\s*\)[\s,]*";
            var params_SPLIT_RE = @"[\s,]+";

            var matrix = new Matrix();
            string cmd = null;
            double[] @params;

            // Split value into ["", "translate", "10 50", "", "scale", "2", "", "rotate",  "-45", ""]
            new Regex(CMD_SPLIT_RE).Split(transformString).ToList().ForEach(item =>
            {

                // Skip empty elements
                if (!item.Any()) { return; }

                // remember operation
                if (operations.Contains(item))
                {
                    cmd = item;
                    return;
                }

                // extract @params & att operation to matrix
                @params = new Regex(params_SPLIT_RE).Split(item).Select(m => double.Parse(m, new CultureInfo("en-US", false))).ToArray();


                // If @params count is not correct - ignore command
                switch (cmd)
                {
                    case "matrix":
                        if (@params.Count() == 6)
                        {
                            matrix.Create(@params);
                        }
                        return;

                    case "scale":
                        if (@params.Count() == 1)
                        {
                            matrix.Scale(@params[0], @params[0]);
                        }
                        else if (@params.Count() == 2)
                        {
                            matrix.Scale(@params[0], @params[1]);
                        }
                        return;

                    case "rotate":
                        if (@params.Count() == 1)
                        {
                            matrix.Rotate(@params[0], 0, 0);
                        }
                        else if (@params.Count() == 3)
                        {
                            matrix.Rotate(@params[0], @params[1], @params[2]);
                        }
                        return;

                    case "translate":
                        if (@params.Count() == 1)
                        {
                            matrix.Translate(@params[0], 0);
                        }
                        else if (@params.Count() == 2)
                        {
                            matrix.Translate(@params[0], @params[1]);
                        }
                        return;

                    case "skewX":
                        if (@params.Count() == 1)
                        {
                            matrix.SkewX(@params[0]);
                        }
                        return;

                    case "skewY":
                        if (@params.Count() == 1)
                        {
                            matrix.SkewY(@params[0]);
                        }
                        return;
                }
            });

            return matrix;
        }

        // recursively apply the transform property to the given element
        public void ApplyTransform(XmlElement element, string globalTransform = "")
        {

            var transformString = element.GetAttribute("transform");
            transformString = globalTransform + transformString;

            Matrix transform = null;

            if (!string.IsNullOrEmpty(transformString))
            {
                transform = TransformParse(transformString);
            }

            if (transform == null)
            {
                transform = new Matrix();
            }

            var tarray = transform.ToArray();

            // decompose affine matrix to rotate, scale components (translate is just the 3rd column)
            var rotate = Math.Atan2(tarray[1], tarray[3]) * 180 / Math.PI;
            var scale = Math.Sqrt(tarray[0] * tarray[0] + tarray[2] * tarray[2]);
            if (element.Name == "g" || element.Name == "svg" || element.Name == "defs" || element.Name == "clipPath")
            {
                element.RemoveAttribute("transform");
                var children = element.ChildNodes.Cast<XmlElement>().ToList();

                children.ForEach(child =>
                {
                    if (!string.IsNullOrEmpty(child.Name))
                    { // skip text nodes
                        ApplyTransform(child, transformString);
                    }
                });
            }
            else if (transform != null && !transform.IsIdentity())
            {
                var id = element.GetAttribute("id");
                var className = element.GetAttribute("class");

                switch (element.Name)
                {
                    case "path":
                        var seglist = PathToAbsolute(element);
                        var prevx = 0.0;
                        var prevy = 0.0;
                        var transformedPath = "";

                        for (var i = 0; i < seglist.Length; i++)
                        {
                            var s = seglist[i];
                            var command = s.PathSegTypeAsLetter;

                            //if (command == "H")
                            //{
                            //    seglist.replaceItem(element.createSVGPathSegLinetoAbs(s.x, prevy), i);
                            //    s = seglist.getItem(i);
                            //}
                            //else if (command == "V")
                            //{
                            //    seglist.replaceItem(element.createSVGPathSegLinetoAbs(prevx, s.y), i);
                            //    s = seglist.getItem(i);
                            //}
                            //// currently only works for uniform scale, no skew
                            //// todo: fully support arbitrary affine transforms...
                            //else if (command == "A")
                            //{
                            //    seglist.replaceItem(element.createSVGPathSegArcAbs(s.x, s.y, s.r1 * scale, s.r2 * scale, s.angle + rotate, s.largeArcFlag, s.sweepFlag), i);
                            //    s = seglist.getItem(i);
                            //}

                            //if ("x" in s && "y" in s) {
                            var transformed = transform.Calc(s.X, s.Y, false);
                            prevx = s.X;
                            prevy = s.Y;

                            var transPoints = new { x = transformed[0], y = transformed[1] };

                            //}
                            //if ("x1" in s && "y1" in s) {
                            //    var transformed = transform.calc(s.x1, s.y1);
                            //    transPoints.x1 = transformed[0];
                            //    transPoints.y1 = transformed[1];
                            //}
                            //if ("x2" in s && "y2" in s) {
                            //    var transformed = transform.calc(s.x2, s.y2);
                            //    transPoints.x2 = transformed[0];
                            //    transPoints.y2 = transformed[1];
                            //}

                            var commandStringTransformed = "";

                            //MLHVCSQTA
                            //H and V are transformed to "L" commands above so we don"t need to handle them. All lowercase (relative) are already handled too (converted to absolute)
                            switch (command)
                            {
                                case "M":
                                    commandStringTransformed += $"{command} {transPoints.x.ToString(culture)} {transPoints.y.ToString(culture)}";
                                    break;
                                case "L":
                                    commandStringTransformed += $"{command} {transPoints.x.ToString(culture)} {transPoints.y.ToString(culture)}";
                                    break;
                                //case "C":
                                //    commandStringTransformed += `${ command} ${ transPoints.x1} ${ transPoints.y1}  ${ transPoints.x2} ${ transPoints.y2} ${ transPoints.x} ${ transPoints.y}`;
                                //    break;
                                //case "S":
                                //    commandStringTransformed += `${ command} ${ transPoints.x2} ${ transPoints.y2} ${ transPoints.x} ${ transPoints.y}`;
                                //    break;
                                //case "Q":
                                //    commandStringTransformed += `${ command} ${ transPoints.x1} ${ transPoints.y1} ${ transPoints.x} ${ transPoints.y}`;
                                //    break;
                                //case "T":
                                //    commandStringTransformed += `${ command} ${ transPoints.x} ${ transPoints.y}`;
                                //    break;
                                //case "A":
                                //    const largeArcFlag = s.largeArcFlag ? 1 : 0;
                                //    const sweepFlag = s.sweepFlag ? 1 : 0;
                                //    commandStringTransformed += `${ command} ${ s.r1} ${ s.r2} ${ s.angle} ${ largeArcFlag} ${ sweepFlag} ${ transPoints.x} ${ transPoints.y}`
                                //break;
                                //case "H":
                                //    commandStringTransformed += `L ${ transPoints.x} ${ transPoints.y}`
                                //break;
                                //case "V":
                                //    commandStringTransformed += `L ${ transPoints.x} ${ transPoints.y}`
                                //break;
                                case "Z":
                                case "z":
                                    commandStringTransformed += command;
                                    break;
                                default:
                                    throw new Exception($"FOUND COMMAND NOT HANDLED BY COMMAND STRING BUILDER - {command}");
                            }

                            transformedPath += commandStringTransformed;
                        }

                        element.SetAttribute("d", transformedPath);
                        element.RemoveAttribute("transform");
                        break;
                }
                if (!string.IsNullOrEmpty(id))
                {
                    element.SetAttribute("id", id);
                }
                if (!string.IsNullOrEmpty(className))
                {
                    element.SetAttribute("class", className);
                }
            }
        }

        // bring all child elements to the top level
        public void Flatten(XmlElement element)
        {
            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(Flatten);

            if (element.Name != "svg")
            {
                childNodes.ForEach(c =>
                {
                    element.ParentNode.AppendChild(c);
                });
            }
        }

        // remove all elements with tag name not in the whitelist
        // use this to remove <text>, <g> etc that don"t represent shapes
        public void Filter(XmlElement element)
        {
            if (!_allowedElements.Any())
            {
                throw new Exception("invalid whitelist");
            }

            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(Filter);

            if (!childNodes.Any() && !_allowedElements.Contains(element.Name))
            {
                element.ParentNode.RemoveChild(element);
            }
        }

        // split a compound path (paths with M, m commands) into an array of paths
        public void SplitPath(XmlElement path)
        {
            if (path == null || path.Name != "path" || path.ParentNode == null)
            {
                return;
            }

            // make copy of seglist (appending to new path removes it from the original pathseglist)
            var seglist = (new SVGPathSegList(path)).PathSegList.Select(seg => seg).ToList();

            double x = 0, y = 0, x0 = 0, y0 = 0;
            //var paths = [];
            //var p;

            var lastM = 0;
            for (var i = seglist.Count() - 1; i >= 0; i--)
            {
                if (i > 0 && seglist[i].PathSegTypeAsLetter == "M" || seglist[i].PathSegTypeAsLetter == "m")
                {
                    lastM = i;
                    break;
                }
            }

            if (lastM == 0)
            {
                return; // only 1 M command, no need to split
            }

            throw new Exception("Path contains more then one M");

            //            for (i = 0; i < seglist.Count(); i++)
            //            {
            //                var s = seglist[i];
            //                var command = s.pathSegTypeAsLetter;

            //                if (command == "M" || command == "m")
            //                {
            //                    p = path.cloneNode();
            //                    p.setAttribute("d", "");
            //                    paths.Add(p);
            //                }

            //                if (/[MLHVCSQTA] /.test(command))
            //                {
            //                    if ("x" in s) x = s.x;
            //            if ("y" in s) y = s.y;

            //            p.pathSegList.appendItem(s);
            //        }
            //                else
            //        {
            //            if ("x" in s) x += s.x;
            //            if ("y" in s) y += s.y;
            //            if (command == "m")
            //            {
            //                p.pathSegList.appendItem(path.createSVGPathSegMovetoAbs(x, y));
            //            }
            //            else
            //            {
            //                if (command == "Z" || command == "z")
            //                {
            //                    x = x0;
            //                    y = y0;
            //                }
            //p.pathSegList.appendItem(s);
            //            }
            //        }
            //        // Record the start of a subpath
            //        if (command == "M" || command == "m")
            //{
            //    x0 = x, y0 = y;
            //}
            //            }

            //            var addedPaths = [];
            //for (i = 0; i < paths.Count(); i++)
            //{
            //    // don"t add trivial paths from sequential M commands
            //    if (paths[i].pathSegList.numberOfItems > 1)
            //    {
            //        path.parentElement.insertBefore(paths[i], path);
            //        addedPaths.Add(paths[i]);
            //    }
            //}

            //path.remove();

            //return addedPaths;
        }

        // recursively run the given function on the given element
        public void SplitCompoundPaths(XmlElement element)
        {
            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(SplitCompoundPaths);

            SplitPath(element);
        }

        // return a polygon from the given SVG element in the form of an array of points
        public DoublePoint[] Polygonify(XmlElement element)
        {
            var poly = new List<DoublePoint>();
            //int i;

            switch (element.Name)
            {
                //    case "polygon":
                //    case "polyline":
                //        for (i = 0; i < element.points.numberOfItems; i++)
                //        {
                //            var point = element.points.getItem(i);
                //            poly.Add({ x: point.x, y: point.y });
                //}
                //break;
                case "rect":
                    var p1 = new DoublePoint(double.Parse(element.GetAttribute("x"), culture),
                        double.Parse(element.GetAttribute("y"), culture));

                    var p2 = new DoublePoint(
                        p1.X + double.Parse(element.GetAttribute("width"), culture),
                        p1.Y);

                    var p3 = new DoublePoint(
                        p2.X,
                        p1.Y + double.Parse(element.GetAttribute("height"), culture));

                    var p4 = new DoublePoint(p1.X, p3.Y);

                    poly.Add(p1);
                    poly.Add(p2);
                    poly.Add(p3);
                    poly.Add(p4);
                    break;
                //case "circle":
                //    var radius = parseFloat(element.getAttribute("r"));
                //    var cx = parseFloat(element.getAttribute("cx"));
                //    var cy = parseFloat(element.getAttribute("cy"));

                //    // num is the smallest number of segments required to approximate the circle to the given tolerance
                //    var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (conf.tolerance / radius)));

                //    if (num < 3)
                //    {
                //        num = 3;
                //    }

                //    for (var i = 0; i < num; i++)
                //    {
                //        var theta = i * ((2 * Math.PI) / num);
                //        var point = { };
                //        point.x = radius * Math.cos(theta) + cx;
                //        point.y = radius * Math.sin(theta) + cy;

                //        poly.Add(point);
                //    }
                //    break;
                //case "ellipse":
                //    // same as circle case. There is probably a way to reduce points but for convenience we will just flatten the equivalent circular polygon
                //    var rx = parseFloat(element.getAttribute("rx"))
                //        var ry = parseFloat(element.getAttribute("ry"));
                //    var maxradius = Math.max(rx, ry);

                //    var cx = parseFloat(element.getAttribute("cx"));
                //    var cy = parseFloat(element.getAttribute("cy"));

                //    var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (conf.tolerance / maxradius)));

                //    if (num < 3)
                //    {
                //        num = 3;
                //    }

                //    for (var i = 0; i < num; i++)
                //    {
                //        var theta = i * ((2 * Math.PI) / num);
                //        var point = { };
                //        point.x = rx * Math.cos(theta) + cx;
                //        point.y = ry * Math.sin(theta) + cy;

                //        poly.Add(point);
                //    }
                //    break;
                case "path":
                    // we"ll assume that splitpath has already been run on this path, and it only has one M/m command 
                    var seglist = (new SVGPathSegList(element)).PathSegList;

                    var firstCommand = seglist.First();
                    var lastCommand = seglist.Last();

                    double x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0, prevx = 0, prevy = 0, prevx1 = 0, prevy1 = 0, prevx2 = 0, prevy2 = 0;

                    for (var i = 0; i < seglist.Length; i++)
                    {
                        var s = seglist[i];
                        var command = s.PathSegTypeAsLetter;

                        prevx = x;
                        prevy = y;

                        prevx1 = x1;
                        prevy1 = y1;

                        prevx2 = x2;
                        prevy2 = y2;

                        if (Regex.IsMatch(command, @"[MLHVCSQTA]"))
                        {
                            //if ("x1" in s) x1 = s.x1;
                            //if ("x2" in s) x2 = s.x2;
                            //if ("y1" in s) y1 = s.y1;
                            //if ("y2" in s) y2 = s.y2;
                            //if ("x" in s) x = s.x;
                            //if ("y" in s) y = s.y;

                            if (new string[] { "Q" }.Contains(s.PathSegTypeAsLetter))
                            {
                                var s1 = s as SVGPathSegCurvetoQuadratic;
                                x1 = s1.X1;
                                y1 = s1.Y1;
                            }

                            x = s.X;
                            y = s.Y;
                        }
                        else
                        {
                            // if ("x1" in s) x1 = x + s.x1;
                            // if ("x2" in s) x2 = x + s.x2;
                            // if ("y1" in s) y1 = y + s.y1;
                            // if ("y2" in s) y2 = y + s.y2;
                            // if ("x" in s) x += s.x;
                            // if ("y" in s) y += s.y;

                            if (new string[] { "q" }.Contains(s.PathSegTypeAsLetter))
                            {
                                var s1 = s as SVGPathSegCurvetoQuadratic;
                                x1 += s1.X1;
                                y1 += s1.Y1;
                            }
                            x += s.X;
                            y += s.Y;
                        }

                        switch (command)
                        {
                            // linear line types
                            case "m":
                            case "M":
                            case "l":
                            case "L":
                            case "h":
                            case "H":
                            case "v":
                            case "V":
                                poly.Add(new DoublePoint(x, y));
                                break;
                            // Quadratic Beziers
                            //case "t":
                            //case "T":
                            //    // implicit control point
                            //    if (i > 0 && /[QqTt] /.test(seglist.getItem(i - 1).pathSegTypeAsLetter))
                            //    {
                            //        x1 = prevx + (prevx - prevx1);
                            //        y1 = prevy + (prevy - prevy1);
                            //    }
                            //    else
                            //    {
                            //        x1 = prevx;
                            //        y1 = prevy;
                            //    }
                            case "q":
                            case "Q":
                                var pointlist = GeometryUtil.LinearizeQuadraticBezier(
                                    new DoublePoint(prevx, prevy), new DoublePoint(x, y), new DoublePoint(x1, y1),
                                    _conf.Tolerance);
                                pointlist.RemoveAt(0); // firstpoint would already be in the poly
                                poly.AddRange(pointlist);
                                break;
                            //case "s":
                            //case "S":
                            //    if (i > 0 && /[CcSs] /.test(seglist.getItem(i - 1).pathSegTypeAsLetter))
                            //    {
                            //        x1 = prevx + (prevx - prevx2);
                            //        y1 = prevy + (prevy - prevy2);
                            //    }
                            //    else
                            //    {
                            //        x1 = prevx;
                            //        y1 = prevy;
                            //    }
                            //case "c":
                            //case "C":
                            //    var pointlist = GeometryUtil.CubicBezier.linearize({ x: prevx, y: prevy }, { x: x, y: y }, { x: x1, y: y1 }, { x: x2, y: y2 }, conf.tolerance);
                            //    pointlist.shift(); // firstpoint would already be in the poly
                            //    for (var j = 0; j < pointlist.Count(); j++)
                            //    {
                            //        var point = { };
                            //        point.x = pointlist[j].x;
                            //        point.y = pointlist[j].y;
                            //        poly.Add(point);
                            //    }
                            //    break;
                            //case "a":
                            //case "A":
                            //    var pointlist = GeometryUtil.Arc.linearize({ x: prevx, y: prevy }, { x: x, y: y }, s.r1, s.r2, s.angle, s.largeArcFlag, s.sweepFlag, conf.tolerance);
                            //    pointlist.shift();

                            //    for (var j = 0; j < pointlist.Count(); j++)
                            //    {
                            //        var point = { };
                            //        point.x = pointlist[j].x;
                            //        point.y = pointlist[j].y;
                            //        poly.Add(point);
                            //    }
                            //    break;
                            case "z":
                            case "Z":
                                x = x0;
                                y = y0;
                                break;
                        }
                        // Record the start of a subpath
                        if (command == "M" || command == "m")
                        {
                            x0 = x;
                            y0 = y;
                        }
                    }

                    break;
            }

            // do not include last point if coincident with starting point
            while (poly.Count() > 0 && GeometryUtil.AlmostEqual(poly[0].X, poly[poly.Count() - 1].X, _conf.ToleranceSvg) &&
                GeometryUtil.AlmostEqual(poly[0].Y, poly[poly.Count() - 1].Y, _conf.ToleranceSvg))
            {
                poly.Pop();
            }

            return poly.ToArray();
        }
    }
}