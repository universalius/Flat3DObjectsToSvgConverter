using SvgLib;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using SvgNest.Utils;
using ClipperLib;
using Plain3DObjectsToSvgConverter.Common.Extensions;


namespace SvgNest
{
    class SvgParserConfig
    {
        public double tolerance { get; set; }
        public double toleranceSvg { get; set; }
    }

    public class SvgParser
    {
        private CultureInfo culture = new CultureInfo("en-US", false);

        public SvgParser()
        {
            conf = new SvgParserConfig
            {
                tolerance = 2, // max bound for bezier->line segment conversion, in native SVG units
                toleranceSvg = 0.005 // fudge factor for browser inaccuracy in SVG unit handling
            };
        }

        // the SVG document
        private SvgDocument svg;

        // the top level SVG element of the SVG document
        private SvgDocument svgRoot;

        private string[] allowedElements = new string[] { "svg", "circle", "ellipse", "path", "polygon", "polyline", "rect", "line" };

        private SvgParserConfig conf;


        //function SvgParser()
        //{
        //    // the SVG document
        //    this.svg;

        //    // the top level SVG element of the SVG document
        //    this.svgRoot;

        //    this.allowedElements = ["svg", "circle", "ellipse", "path", "polygon", "polyline", "rect", "line"];

        //    this.conf = {
        //    tolerance: 2, // max bound for bezier->line segment conversion, in native SVG units
        //        toleranceSvg: 0.005 // fudge factor for browser inaccuracy in SVG unit handling
        //    };
        //}


        //    public  config(config) {
        //this.conf.tolerance = config.tolerance;
        //}

        //public  load(svgString) {

        //    if (!svgString || typeof svgString !== "string")
        //    {
        //        throw Error("invalid SVG string");
        //    }

        //    var jsdom = new JSDOM();
        //    var parser = new jsdom.window.DOMParser();
        //    var svg = parser.parseFromString(svgString, "image/svg+xml");

        //    this.svgRoot = false;

        //    if (svg)
        //    {
        //        this.svg = svg;

        //        for (var i = 0; i < svg.childNodes.Count(); i++)
        //        {
        //            // svg document may start with comments or text nodes
        //            var child = svg.childNodes[i];
        //            if (child.tagName && child.tagName == "svg")
        //            {
        //                this.svgRoot = child;
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        throw new Error("Failed to parse SVG string");
        //    }

        //    if (!this.svgRoot)
        //    {
        //        throw new Error("SVG has no children");
        //    }
        //    return this.svgRoot;
        //}

        public SvgDocument load(string svgString)
        {
            if (string.IsNullOrEmpty(svgString))
            {
                throw new Exception("invalid SVG string");
            }

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svgString);

            svgRoot = new SvgDocument(xmlDocument, xmlDocument.DocumentElement);

            return svgRoot;
        }



        // use the utility functions in this class to prepare the svg for CAD-CAM/nest related operations
        //        public  cleanInput() {

        //    // apply any transformations, so that all path positions etc will be in the same coordinate space
        //    this.applyTransform(this.svgRoot);

        //    // remove any g elements and bring all elements to the top level
        //    this.flatten(this.svgRoot);

        //    // remove any non-contour elements like text
        //    this.filter(this.allowedElements);

        //    // split any compound paths into individual path elements
        //    this.recurse(this.svgRoot, this.splitPath);

        //    return this.svgRoot;

        //}

        // use the utility functions in this class to prepare the svg for CAD-CAM/nest related operations
        public SvgDocument cleanInput()
        {
            // apply any transformations, so that all path positions etc will be in the same coordinate space
            applyTransform(svgRoot.Element);

            // remove any g elements and bring all elements to the top level
            flatten(svgRoot.Element);

            // remove any non-contour elements like text
            filter(svgRoot.Element);

            // split any compound paths into individual path elements
            splitCompoundPaths(svgRoot.Element);

            return svgRoot;
        }

        //// return style node, if any
        //public  getStyle() {
        //    if (!this.svgRoot)
        //    {
        //        return false;
        //    }
        //    for (var i = 0; i < this.svgRoot.childNodes.Count(); i++)
        //    {
        //        var el = this.svgRoot.childNodes[i];
        //        if (el.tagName == "style")
        //        {
        //            return el;
        //        }
        //    }

        //    return false;
        //}

        // set the given path as absolute coords (capital commands)
        // from http://stackoverflow.com/a/9677915/433888
        //        public  pathToAbsolute(path)
        //        {
        //            if (!path || path.tagName != "path")
        //            {
        //                throw Error("invalid path");
        //            }

        //            var seglist = path.pathSegList;
        //            var x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0;

        //            for (var i = 0; i < seglist.numberOfItems; i++)
        //            {
        //                var command = seglist.getItem(i).pathSegTypeAsLetter;
        //                var s = seglist.getItem(i);

        //                if (/[MLHVCSQTA] /.test(command))
        //                {
        //                    if ("x" in s) x = s.x;
        //            if ("y" in s) y = s.y;
        //        }
        //        else
        //{
        //    if ("x1" in s) x1 = x + s.x1;
        //    if ("x2" in s) x2 = x + s.x2;
        //    if ("y1" in s) y1 = y + s.y1;
        //    if ("y2" in s) y2 = y + s.y2;
        //    if ("x" in s) x += s.x;
        //    if ("y" in s) y += s.y;
        //    switch (command)
        //    {
        //        case "m": seglist.replaceItem(path.createSVGPathSegMovetoAbs(x, y), i); break;
        //        case "l": seglist.replaceItem(path.createSVGPathSegLinetoAbs(x, y), i); break;
        //        case "h": seglist.replaceItem(path.createSVGPathSegLinetoHorizontalAbs(x), i); break;
        //        case "v": seglist.replaceItem(path.createSVGPathSegLinetoVerticalAbs(y), i); break;
        //        case "c": seglist.replaceItem(path.createSVGPathSegCurvetoCubicAbs(x, y, x1, y1, x2, y2), i); break;
        //        case "s": seglist.replaceItem(path.createSVGPathSegCurvetoCubicSmoothAbs(x, y, x2, y2), i); break;
        //        case "q": seglist.replaceItem(path.createSVGPathSegCurvetoQuadraticAbs(x, y, x1, y1), i); break;
        //        case "t": seglist.replaceItem(path.createSVGPathSegCurvetoQuadraticSmoothAbs(x, y), i); break;
        //        case "a": seglist.replaceItem(path.createSVGPathSegArcAbs(x, y, s.r1, s.r2, s.angle, s.largeArcFlag, s.sweepFlag), i); break;
        //        case "z": case "Z": x = x0; y = y0; break;
        //    }
        //}
        //// Record the start of a subpath
        //if (command == "M" || command == "m") x0 = x, y0 = y;
        //    }
        //};


        public SVGPathSeg[] pathToAbsolute(XmlElement path)
        {
            if (path == null || path.Name != "path")
            {
                throw new Exception("invalid path");
            }

            var seglist = (new SVGPathSegList(path)).pathSegList;
            double x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            for (var i = 0; i < seglist.Count(); i++)
            {
                var s = seglist[i];
                var command = s.pathSegTypeAsLetter;

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
        //public  transformParse(transformString) {
        //    var operations = {
        //        matrix: true,
        //        scale: true,
        //        rotate: true,
        //        translate: true,
        //        skewX: true,
        //        skewY: true
        //    };

        //var CMD_SPLIT_RE = /\s*(matrix|translate|scale|rotate|skewX|skewY)\s *\(\s * (.+?)\s *\)[\s,] */;
        //var @params_SPLIT_RE = /[\s,] +/;

        //var matrix = new Matrix();
        //var cmd, @params;

        //// Split value into ["", "translate", "10 50", "", "scale", "2", "", "rotate",  "-45", ""]
        //transformString.split(CMD_SPLIT_RE).forEach(function(item) {

        //    // Skip empty elements
        //    if (!item.Count()) { return; }

        //    // remember operation
        //    if (typeof operations[item] !== "undefined")
        //    {
        //        cmd = item;
        //        return;
        //    }

        //        // extract @params & att operation to matrix
        //        @params = item.split(@params_SPLIT_RE).map(function(i) {
        //        return +i || 0;
        //    });

        //    // If @params count is not correct - ignore command
        //    switch (cmd)
        //    {
        //        case "matrix":
        //            if (@params.Count() == 6) {
        //                matrix.matrix (@params);
        //            }
        //            return;

        //        case "scale":
        //            if (@params.Count() == 1) {
        //                matrix.scale (@params[0], @params[0]);
        //            } else if (@params.Count() == 2) {
        //                matrix.scale (@params[0], @params[1]);
        //            }
        //            return;

        //        case "rotate":
        //            if (@params.Count() == 1) {
        //                matrix.rotate (@params[0], 0, 0);
        //            } else if (@params.Count() == 3) {
        //                matrix.rotate (@params[0], @params[1], @params[2]);
        //            }
        //            return;

        //        case "translate":
        //            if (@params.Count() == 1) {
        //                matrix.translate (@params[0], 0);
        //            } else if (@params.Count() == 2) {
        //                matrix.translate (@params[0], @params[1]);
        //            }
        //            return;

        //        case "skewX":
        //            if (@params.Count() == 1) {
        //                matrix.skewX (@params[0]);
        //            }
        //            return;

        //        case "skewY":
        //            if (@params.Count() == 1) {
        //                matrix.skewY (@params[0]);
        //            }
        //            return;
        //    }
        //});

        //return matrix;
        //}


        // takes an SVG transform string and returns corresponding SVGMatrix
        // from https://github.com/fontello/svgpath
        private Matrix transformParse(string transformString)
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
                            matrix.matrix(@params);
                        }
                        return;

                    case "scale":
                        if (@params.Count() == 1)
                        {
                            matrix.scale(@params[0], @params[0]);
                        }
                        else if (@params.Count() == 2)
                        {
                            matrix.scale(@params[0], @params[1]);
                        }
                        return;

                    case "rotate":
                        if (@params.Count() == 1)
                        {
                            matrix.rotate(@params[0], 0, 0);
                        }
                        else if (@params.Count() == 3)
                        {
                            matrix.rotate(@params[0], @params[1], @params[2]);
                        }
                        return;

                    case "translate":
                        if (@params.Count() == 1)
                        {
                            matrix.translate(@params[0], 0);
                        }
                        else if (@params.Count() == 2)
                        {
                            matrix.translate(@params[0], @params[1]);
                        }
                        return;

                    case "skewX":
                        if (@params.Count() == 1)
                        {
                            matrix.skewX(@params[0]);
                        }
                        return;

                    case "skewY":
                        if (@params.Count() == 1)
                        {
                            matrix.skewY(@params[0]);
                        }
                        return;
                }
            });

            return matrix;
        }



        //// recursively apply the transform property to the given element
        //public  applyTransform(element, globalTransform) {

        //    globalTransform = globalTransform || "";

        //    var transformString = element.getAttribute("transform") || "";
        //    transformString = globalTransform + transformString;

        //    var transform, scale, rotate;

        //    if (transformString && transformString.Count() > 0)
        //    {
        //        var transform = this.transformParse(transformString);
        //    }

        //    if (!transform)
        //    {
        //        transform = new Matrix();
        //    }

        //    var tarray = transform.toArray();

        //    // decompose affine matrix to rotate, scale components (translate is just the 3rd column)
        //    var rotate = Math.atan2(tarray[1], tarray[3]) * 180 / Math.PI;
        //    var scale = Math.sqrt(tarray[0] * tarray[0] + tarray[2] * tarray[2]);

        //    if (element.tagName == "g" || element.tagName == "svg" || element.tagName == "defs" || element.tagName == "clipPath")
        //    {
        //        element.removeAttribute("transform");
        //        var children = Array.prototype.slice.call(element.childNodes);

        //        for (var i = 0; i < children.Count(); i++)
        //        {
        //            if (children[i].tagName)
        //            { // skip text nodes
        //                this.applyTransform(children[i], transformString);
        //            }
        //        }
        //    }
        //    else if (transform && !transform.isIdentity())
        //    {
        //        const id = element.getAttribute("id")
        //        const className = element.getAttribute("class")

        //        switch (element.tagName)
        //        {
        //            case "ellipse":
        //                // the goal is to remove the transform property, but an ellipse without a transform will have no rotation
        //                // for the sake of simplicity, we will replace the ellipse with a path, and apply the transform to that path
        //                var path = this.svg.createElementNS(element.namespaceURI, "path");
        //                var move = path.createSVGPathSegMovetoAbs(parseFloat(element.getAttribute("cx")) - parseFloat(element.getAttribute("rx")), element.getAttribute("cy"));
        //                var arc1 = path.createSVGPathSegArcAbs(parseFloat(element.getAttribute("cx")) + parseFloat(element.getAttribute("rx")), element.getAttribute("cy"), element.getAttribute("rx"), element.getAttribute("ry"), 0, 1, 0);
        //                var arc2 = path.createSVGPathSegArcAbs(parseFloat(element.getAttribute("cx")) - parseFloat(element.getAttribute("rx")), element.getAttribute("cy"), element.getAttribute("rx"), element.getAttribute("ry"), 0, 1, 0);

        //                path.pathSegList.appendItem(move);
        //                path.pathSegList.appendItem(arc1);
        //                path.pathSegList.appendItem(arc2);
        //                path.pathSegList.appendItem(path.createSVGPathSegClosePath());

        //                var transformProperty = element.getAttribute("transform");
        //                if (transformProperty)
        //                {
        //                    path.setAttribute("transform", transformProperty);
        //                }

        //                element.parentElement.replaceChild(path, element);

        //                element = path;

        //            case "path":
        //                this.pathToAbsolute(element);
        //                var seglist = element.pathSegList;
        //                var prevx = 0;
        //                var prevy = 0;

        //                let transformedPath = "";

        //                for (var i = 0; i < seglist.numberOfItems; i++)
        //                {
        //                    var s = seglist.getItem(i);
        //                    var command = s.pathSegTypeAsLetter;


        //                    if (command == "H")
        //                    {
        //                        seglist.replaceItem(element.createSVGPathSegLinetoAbs(s.x, prevy), i);
        //                        s = seglist.getItem(i);
        //                    }
        //                    else if (command == "V")
        //                    {
        //                        seglist.replaceItem(element.createSVGPathSegLinetoAbs(prevx, s.y), i);
        //                        s = seglist.getItem(i);
        //                    }
        //                    // currently only works for uniform scale, no skew
        //                    // todo: fully support arbitrary affine transforms...
        //                    else if (command == "A")
        //                    {
        //                        seglist.replaceItem(element.createSVGPathSegArcAbs(s.x, s.y, s.r1 * scale, s.r2 * scale, s.angle + rotate, s.largeArcFlag, s.sweepFlag), i);
        //                        s = seglist.getItem(i);
        //                    }

        //                    const transPoints = { };

        //                    if ("x" in s && "y" in s) {
        //                    var transformed = transform.calc(s.x, s.y);
        //                    prevx = s.x;
        //                    prevy = s.y;
        //                    transPoints.x = transformed[0];
        //                    transPoints.y = transformed[1];
        //                }
        //                if ("x1" in s && "y1" in s) {
        //                    var transformed = transform.calc(s.x1, s.y1);
        //                    transPoints.x1 = transformed[0];
        //                    transPoints.y1 = transformed[1];
        //                }
        //                if ("x2" in s && "y2" in s) {
        //                    var transformed = transform.calc(s.x2, s.y2);
        //                    transPoints.x2 = transformed[0];
        //                    transPoints.y2 = transformed[1];
        //                }

        //                let commandStringTransformed = ``;

        //                //MLHVCSQTA
        //                //H and V are transformed to "L" commands above so we don"t need to handle them. All lowercase (relative) are already handled too (converted to absolute)
        //                switch (command)
        //                {
        //                    case "M":
        //                        commandStringTransformed += `${ command} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "L":
        //                        commandStringTransformed += `${ command} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "C":
        //                        commandStringTransformed += `${ command} ${ transPoints.x1} ${ transPoints.y1}  ${ transPoints.x2} ${ transPoints.y2} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "S":
        //                        commandStringTransformed += `${ command} ${ transPoints.x2} ${ transPoints.y2} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "Q":
        //                        commandStringTransformed += `${ command} ${ transPoints.x1} ${ transPoints.y1} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "T":
        //                        commandStringTransformed += `${ command} ${ transPoints.x} ${ transPoints.y}`;
        //                        break;
        //                    case "A":
        //                        const largeArcFlag = s.largeArcFlag ? 1 : 0;
        //                        const sweepFlag = s.sweepFlag ? 1 : 0;
        //                        commandStringTransformed += `${ command} ${ s.r1} ${ s.r2} ${ s.angle} ${ largeArcFlag} ${ sweepFlag} ${ transPoints.x} ${ transPoints.y}`
        //                            break;
        //                    case "H":
        //                        commandStringTransformed += `L ${ transPoints.x} ${ transPoints.y}`
        //                            break;
        //                    case "V":
        //                        commandStringTransformed += `L ${ transPoints.x} ${ transPoints.y}`
        //                            break;
        //                    case "Z":
        //                    case "z":
        //                        commandStringTransformed += command;
        //                        break;
        //                    default:
        //                        console.log("FOUND COMMAND NOT HANDLED BY COMMAND STRING BUILDER", command);
        //                        break;
        //                }

        //                transformedPath += commandStringTransformed;
        //        }

        //        element.setAttribute("d", transformedPath);
        //        element.removeAttribute("transform");
        //        break;
        //            case "circle":
        //            var transformed = transform.calc(element.getAttribute("cx"), element.getAttribute("cy"));
        //            element.setAttribute("cx", transformed[0]);
        //            element.setAttribute("cy", transformed[1]);

        //            // skew not supported
        //            element.setAttribute("r", element.getAttribute("r") * scale);
        //            break;
        //        case "line":
        //            const transformedStartPt = transform.calc(element.getAttribute("x1"), element.getAttribute("y1"));
        //            const transformedEndPt = transform.calc(element.getAttribute("x2"), element.getAttribute("y2"));
        //            element.setAttribute("x1", transformedStartPt[0].toString());
        //            element.setAttribute("y1", transformedStartPt[1].toString());
        //            element.setAttribute("x2", transformedEndPt[0].toString());
        //            element.setAttribute("y2", transformedEndPt[1].toString());
        //            break;
        //        case "rect":
        //            // similar to the ellipse, we"ll replace rect with polygon
        //            var polygon = this.svg.createElementNS(element.namespaceURI, "polygon");

        //            var p1 = this.svgRoot.createSVGPoint();
        //            var p2 = this.svgRoot.createSVGPoint();
        //            var p3 = this.svgRoot.createSVGPoint();
        //            var p4 = this.svgRoot.createSVGPoint();

        //            p1.x = parseFloat(element.getAttribute("x")) || 0;
        //            p1.y = parseFloat(element.getAttribute("y")) || 0;

        //            p2.x = p1.x + parseFloat(element.getAttribute("width"));
        //            p2.y = p1.y;

        //            p3.x = p2.x;
        //            p3.y = p1.y + parseFloat(element.getAttribute("height"));

        //            p4.x = p1.x;
        //            p4.y = p3.y;

        //            polygon.points.appendItem(p1);
        //            polygon.points.appendItem(p2);
        //            polygon.points.appendItem(p3);
        //            polygon.points.appendItem(p4);

        //            var transformProperty = element.getAttribute("transform");
        //            if (transformProperty)
        //            {
        //                polygon.setAttribute("transform", transformProperty);
        //            }

        //            element.parentElement.replaceChild(polygon, element);
        //            element = polygon;
        //        case "polygon":
        //        case "polyline":
        //            let transformedPoly = ""
        //                for (var i = 0; i < element.points.numberOfItems; i++)
        //            {
        //                var point = element.points.getItem(i);
        //                var transformed = transform.calc(point.x, point.y);
        //                const pointPairString = `${ transformed[0]},${ transformed[1]} `;
        //                transformedPoly += pointPairString;
        //            }

        //            element.setAttribute("points", transformedPoly);
        //            element.removeAttribute("transform");
        //            break;
        //        }
        //        if (id)
        //        {
        //            element.setAttribute("id", id);
        //        }
        //        if (className)
        //        {
        //            element.setAttribute("class", className);
        //        }
        //    }
        //}

        // recursively apply the transform property to the given element
        private void applyTransform(XmlElement element, string globalTransform = "")
        {

            var transformString = element.GetAttribute("transform");
            transformString = globalTransform + transformString;

            Matrix transform = null;

            if (!string.IsNullOrEmpty(transformString))
            {
                transform = transformParse(transformString);
            }

            if (transform == null)
            {
                transform = new Matrix();
            }

            var tarray = transform.toArray();

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
                        applyTransform(child, transformString);
                    }
                });
            }
            else if (transform != null && !transform.isIdentity())
            {
                var id = element.GetAttribute("id");
                var className = element.GetAttribute("class");

                switch (element.Name)
                {
                    case "path":
                        var seglist = this.pathToAbsolute(element);
                        var prevx = 0.0;
                        var prevy = 0.0;
                        var transformedPath = "";

                        for (var i = 0; i < seglist.Length; i++)
                        {
                            var s = seglist[i];
                            var command = s.pathSegTypeAsLetter;

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
                            var transformed = transform.calc(s.X, s.Y, false);
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



        //// bring all child elements to the top level
        //public  flatten(element) {

        //    for (var i = 0; i < element.childNodes.Count(); i++)
        //    {
        //        this.flatten(element.childNodes[i]);
        //    }

        //    if (element.tagName != "svg")
        //    {
        //        while (element.childNodes.Count() > 0)
        //        {
        //            element.parentElement.appendChild(element.childNodes[0]);
        //        }
        //    }
        //}

        // bring all child elements to the top level
        public void flatten(XmlElement element)
        {
            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(this.flatten);

            if (element.Name != "svg")
            {
                childNodes.ForEach(c =>
                {
                    element.ParentNode.AppendChild(c);
                });
            }
        }

        //// remove all elements with tag name not in the whitelist
        //// use this to remove <text>, <g> etc that don"t represent shapes
        //public  filter(whitelist, element) {
        //    if (!whitelist || whitelist.Count() == 0)
        //    {
        //        throw Error("invalid whitelist");
        //    }

        //    element = element || this.svgRoot;

        //    for (var i = 0; i < element.childNodes.Count(); i++)
        //    {
        //        this.filter(whitelist, element.childNodes[i]);
        //    }

        //    if (element.childNodes.Count() == 0 && whitelist.indexOf(element.tagName) < 0)
        //    {
        //        element.parentElement.removeChild(element);
        //    }
        //}

        // remove all elements with tag name not in the whitelist
        // use this to remove <text>, <g> etc that don"t represent shapes
        public void filter(XmlElement element)
        {
            if (!allowedElements.Any())
            {
                throw new Exception("invalid whitelist");
            }

            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(this.filter);

            if (!childNodes.Any() && !allowedElements.Contains(element.Name))
            {
                element.ParentNode.RemoveChild(element);
            }
        }

        //// split a compound path (paths with M, m commands) into an array of paths
        //public  splitPath(path) {
        //    if (!path || path.tagName != "path" || !path.parentElement)
        //    {
        //        return false;
        //    }

        //    var seglist = [];

        //    // make copy of seglist (appending to new path removes it from the original pathseglist)
        //    for (var i = 0; i < path.pathSegList.numberOfItems; i++)
        //    {
        //        seglist.push(path.pathSegList.getItem(i));
        //    }

        //    var x = 0, y = 0, x0 = 0, y0 = 0;
        //    var paths = [];

        //    var p;

        //    var lastM = 0;
        //    for (var i = seglist.Count() - 1; i >= 0; i--)
        //    {
        //        if (i > 0 && seglist[i].pathSegTypeAsLetter == "M" || seglist[i].pathSegTypeAsLetter == "m")
        //        {
        //            lastM = i;
        //            break;
        //        }
        //    }

        //    if (lastM == 0)
        //    {
        //        return false; // only 1 M command, no need to split
        //    }

        //    for (i = 0; i < seglist.Count(); i++)
        //    {
        //        var s = seglist[i];
        //        var command = s.pathSegTypeAsLetter;

        //        if (command == "M" || command == "m")
        //        {
        //            p = path.cloneNode();
        //            p.setAttribute("d", "");
        //            paths.push(p);
        //        }

        //        if (/[MLHVCSQTA] /.test(command))
        //        {
        //            if ("x" in s) x = s.x;
        //    if ("y" in s) y = s.y;

        //    p.pathSegList.appendItem(s);
        //}
        //        else
        //{
        //    if ("x" in s) x += s.x;
        //    if ("y" in s) y += s.y;
        //    if (command == "m")
        //    {
        //        p.pathSegList.appendItem(path.createSVGPathSegMovetoAbs(x, y));
        //    }
        //    else
        //    {
        //        if (command == "Z" || command == "z")
        //        {
        //            x = x0;
        //            y = y0;
        //        }
        //        p.pathSegList.appendItem(s);
        //    }
        //}
        //// Record the start of a subpath
        //if (command == "M" || command == "m")
        //{
        //    x0 = x, y0 = y;
        //}
        //    }

        //    var addedPaths = [];
        //for (i = 0; i < paths.Count(); i++)
        //{
        //    // don"t add trivial paths from sequential M commands
        //    if (paths[i].pathSegList.numberOfItems > 1)
        //    {
        //        path.parentElement.insertBefore(paths[i], path);
        //        addedPaths.push(paths[i]);
        //    }
        //}

        //path.remove();

        //return addedPaths;
        //}

        // split a compound path (paths with M, m commands) into an array of paths
        public void splitPath(XmlElement path)
        {
            if (path == null || path.Name != "path" || path.ParentNode == null)
            {
                return;
            }

            // make copy of seglist (appending to new path removes it from the original pathseglist)
            var seglist = (new SVGPathSegList(path)).pathSegList.Select(seg => seg).ToList();

            double x = 0, y = 0, x0 = 0, y0 = 0;
            //var paths = [];
            //var p;

            var lastM = 0;
            for (var i = seglist.Count() - 1; i >= 0; i--)
            {
                if (i > 0 && seglist[i].pathSegTypeAsLetter == "M" || seglist[i].pathSegTypeAsLetter == "m")
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
            //                    paths.push(p);
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
            //        addedPaths.push(paths[i]);
            //    }
            //}

            //path.remove();

            //return addedPaths;
        }


        //// recursively run the given function on the given element
        //public  recurse(element, func) {
        //    // only operate on original DOM tree, ignore any children that are added. Avoid infinite loops
        //    var children = Array.prototype.slice.call(element.childNodes);
        //    for (var i = 0; i < children.Count(); i++)
        //    {
        //        this.recurse(children[i], func);
        //    }

        //    func(element);
        //}

        // recursively run the given function on the given element
        public void splitCompoundPaths(XmlElement element)
        {
            var childNodes = element.ChildNodes.Cast<XmlElement>().ToList();
            childNodes.ForEach(this.splitCompoundPaths);

            splitPath(element);
        }

        //// return a polygon from the given SVG element in the form of an array of points
        //public  polygonify(element) {
        //    var poly = [];
        //    var i;

        //    switch (element.tagName)
        //    {
        //        case "polygon":
        //        case "polyline":
        //            for (i = 0; i < element.points.numberOfItems; i++)
        //            {
        //                var point = element.points.getItem(i);
        //                poly.push({ x: point.x, y: point.y });
        //    }
        //    break;
        //        case "rect":
        //        var p1 = { };
        //        var p2 = { };
        //        var p3 = { };
        //        var p4 = { };

        //        p1.x = parseFloat(element.getAttribute("x")) || 0;
        //        p1.y = parseFloat(element.getAttribute("y")) || 0;

        //        p2.x = p1.x + parseFloat(element.getAttribute("width"));
        //        p2.y = p1.y;

        //        p3.x = p2.x;
        //        p3.y = p1.y + parseFloat(element.getAttribute("height"));

        //        p4.x = p1.x;
        //        p4.y = p3.y;

        //        poly.push(p1);
        //        poly.push(p2);
        //        poly.push(p3);
        //        poly.push(p4);
        //        break;
        //    case "circle":
        //        var radius = parseFloat(element.getAttribute("r"));
        //        var cx = parseFloat(element.getAttribute("cx"));
        //        var cy = parseFloat(element.getAttribute("cy"));

        //        // num is the smallest number of segments required to approximate the circle to the given tolerance
        //        var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (this.conf.tolerance / radius)));

        //        if (num < 3)
        //        {
        //            num = 3;
        //        }

        //        for (var i = 0; i < num; i++)
        //        {
        //            var theta = i * ((2 * Math.PI) / num);
        //            var point = { };
        //            point.x = radius * Math.cos(theta) + cx;
        //            point.y = radius * Math.sin(theta) + cy;

        //            poly.push(point);
        //        }
        //        break;
        //    case "ellipse":
        //        // same as circle case. There is probably a way to reduce points but for convenience we will just flatten the equivalent circular polygon
        //        var rx = parseFloat(element.getAttribute("rx"))
        //            var ry = parseFloat(element.getAttribute("ry"));
        //        var maxradius = Math.max(rx, ry);

        //        var cx = parseFloat(element.getAttribute("cx"));
        //        var cy = parseFloat(element.getAttribute("cy"));

        //        var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (this.conf.tolerance / maxradius)));

        //        if (num < 3)
        //        {
        //            num = 3;
        //        }

        //        for (var i = 0; i < num; i++)
        //        {
        //            var theta = i * ((2 * Math.PI) / num);
        //            var point = { };
        //            point.x = rx * Math.cos(theta) + cx;
        //            point.y = ry * Math.sin(theta) + cy;

        //            poly.push(point);
        //        }
        //        break;
        //    case "path":
        //        // we"ll assume that splitpath has already been run on this path, and it only has one M/m command 
        //        var seglist = element.pathSegList;

        //        var firstCommand = seglist.getItem(0);
        //        var lastCommand = seglist.getItem(seglist.numberOfItems - 1);

        //        var x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0, prevx = 0, prevy = 0, prevx1 = 0, prevy1 = 0, prevx2 = 0, prevy2 = 0;

        //        for (var i = 0; i < seglist.numberOfItems; i++)
        //        {
        //            var s = seglist.getItem(i);
        //            var command = s.pathSegTypeAsLetter;

        //            prevx = x;
        //            prevy = y;

        //            prevx1 = x1;
        //            prevy1 = y1;

        //            prevx2 = x2;
        //            prevy2 = y2;

        //            if (/[MLHVCSQTA] /.test(command))
        //            {
        //                if ("x1" in s) x1 = s.x1;
        //        if ("x2" in s) x2 = s.x2;
        //        if ("y1" in s) y1 = s.y1;
        //        if ("y2" in s) y2 = s.y2;
        //        if ("x" in s) x = s.x;
        //        if ("y" in s) y = s.y;
        //    }
        //                else
        //    {
        //        if ("x1" in s) x1 = x + s.x1;
        //        if ("x2" in s) x2 = x + s.x2;
        //        if ("y1" in s) y1 = y + s.y1;
        //        if ("y2" in s) y2 = y + s.y2;
        //        if ("x" in s) x += s.x;
        //        if ("y" in s) y += s.y;
        //    }
        //    switch (command)
        //    {
        //        // linear line types
        //        case "m":
        //        case "M":
        //        case "l":
        //        case "L":
        //        case "h":
        //        case "H":
        //        case "v":
        //        case "V":
        //            var point = { };
        //            point.x = x;
        //            point.y = y;
        //            poly.push(point);
        //            break;
        //        // Quadratic Beziers
        //        case "t":
        //        case "T":
        //            // implicit control point
        //            if (i > 0 && /[QqTt] /.test(seglist.getItem(i - 1).pathSegTypeAsLetter))
        //            {
        //                x1 = prevx + (prevx - prevx1);
        //                y1 = prevy + (prevy - prevy1);
        //            }
        //            else
        //            {
        //                x1 = prevx;
        //                y1 = prevy;
        //            }
        //        case "q":
        //        case "Q":
        //            var pointlist = GeometryUtil.QuadraticBezier.linearize({ x: prevx, y: prevy }, { x: x, y: y }, { x: x1, y: y1 }, this.conf.tolerance);
        //            pointlist.shift(); // firstpoint would already be in the poly
        //            for (var j = 0; j < pointlist.Count(); j++)
        //            {
        //                var point = { };
        //                point.x = pointlist[j].x;
        //                point.y = pointlist[j].y;
        //                poly.push(point);
        //            }
        //            break;
        //        case "s":
        //        case "S":
        //            if (i > 0 && /[CcSs] /.test(seglist.getItem(i - 1).pathSegTypeAsLetter))
        //            {
        //                x1 = prevx + (prevx - prevx2);
        //                y1 = prevy + (prevy - prevy2);
        //            }
        //            else
        //            {
        //                x1 = prevx;
        //                y1 = prevy;
        //            }
        //        case "c":
        //        case "C":
        //            var pointlist = GeometryUtil.CubicBezier.linearize({ x: prevx, y: prevy }, { x: x, y: y }, { x: x1, y: y1 }, { x: x2, y: y2 }, this.conf.tolerance);
        //            pointlist.shift(); // firstpoint would already be in the poly
        //            for (var j = 0; j < pointlist.Count(); j++)
        //            {
        //                var point = { };
        //                point.x = pointlist[j].x;
        //                point.y = pointlist[j].y;
        //                poly.push(point);
        //            }
        //            break;
        //        case "a":
        //        case "A":
        //            var pointlist = GeometryUtil.Arc.linearize({ x: prevx, y: prevy }, { x: x, y: y }, s.r1, s.r2, s.angle, s.largeArcFlag, s.sweepFlag, this.conf.tolerance);
        //            pointlist.shift();

        //            for (var j = 0; j < pointlist.Count(); j++)
        //            {
        //                var point = { };
        //                point.x = pointlist[j].x;
        //                point.y = pointlist[j].y;
        //                poly.push(point);
        //            }
        //            break;
        //        case "z": case "Z": x = x0; y = y0; break;
        //    }
        //    // Record the start of a subpath
        //    if (command == "M" || command == "m") x0 = x, y0 = y;
        //}

        //break;
        //    }

        //    // do not include last point if coincident with starting point
        //    while (poly.Count() > 0 && GeometryUtil.almostEqual(poly[0].x, poly[poly.Count() - 1].x, this.conf.toleranceSvg) && GeometryUtil.almostEqual(poly[0].y, poly[poly.Count() - 1].y, this.conf.toleranceSvg))
        //{
        //    poly.pop();
        //}

        //return poly;
        //};

        ////// expose public methods
        ////var parser = new SvgParser();

        ////root.SvgParser = {
        ////    config: parser.config.bind(parser),
        ////    load: parser.load.bind(parser),
        ////    getStyle: parser.getStyle.bind(parser),
        ////    clean: parser.cleanInput.bind(parser),
        ////    polygonify: parser.polygonify.bind(parser)
        ////};

        //module.exports.SvgParser = SvgParser;


        // return a polygon from the given SVG element in the form of an array of points
        public DoublePoint[] polygonify(XmlElement element)
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
                //            poly.push({ x: point.x, y: point.y });
                //}
                //break;
                //    case "rect":
                //    var p1 = { };
                //    var p2 = { };
                //    var p3 = { };
                //    var p4 = { };

                //    p1.x = parseFloat(element.getAttribute("x")) || 0;
                //    p1.y = parseFloat(element.getAttribute("y")) || 0;

                //    p2.x = p1.x + parseFloat(element.getAttribute("width"));
                //    p2.y = p1.y;

                //    p3.x = p2.x;
                //    p3.y = p1.y + parseFloat(element.getAttribute("height"));

                //    p4.x = p1.x;
                //    p4.y = p3.y;

                //    poly.push(p1);
                //    poly.push(p2);
                //    poly.push(p3);
                //    poly.push(p4);
                //    break;
                //case "circle":
                //    var radius = parseFloat(element.getAttribute("r"));
                //    var cx = parseFloat(element.getAttribute("cx"));
                //    var cy = parseFloat(element.getAttribute("cy"));

                //    // num is the smallest number of segments required to approximate the circle to the given tolerance
                //    var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (this.conf.tolerance / radius)));

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

                //        poly.push(point);
                //    }
                //    break;
                //case "ellipse":
                //    // same as circle case. There is probably a way to reduce points but for convenience we will just flatten the equivalent circular polygon
                //    var rx = parseFloat(element.getAttribute("rx"))
                //        var ry = parseFloat(element.getAttribute("ry"));
                //    var maxradius = Math.max(rx, ry);

                //    var cx = parseFloat(element.getAttribute("cx"));
                //    var cy = parseFloat(element.getAttribute("cy"));

                //    var num = Math.ceil((2 * Math.PI) / Math.acos(1 - (this.conf.tolerance / maxradius)));

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

                //        poly.push(point);
                //    }
                //    break;
                case "path":
                    // we"ll assume that splitpath has already been run on this path, and it only has one M/m command 
                    var seglist = (new SVGPathSegList(element)).pathSegList;

                    var firstCommand = seglist.First();
                    var lastCommand = seglist.Last();

                    double x = 0, y = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0, prevx = 0, prevy = 0, prevx1 = 0, prevy1 = 0, prevx2 = 0, prevy2 = 0;

                    for (var i = 0; i < seglist.Length; i++)
                    {
                        var s = seglist[i];
                        var command = s.pathSegTypeAsLetter;

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
                            x = s.X;
                            y = s.Y;
                        }
                        //            else
                        //            {
                        //                if ("x1" in s) x1 = x + s.x1;
                        //        if ("x2" in s) x2 = x + s.x2;
                        //        if ("y1" in s) y1 = y + s.y1;
                        //        if ("y2" in s) y2 = y + s.y2;
                        //        if ("x" in s) x += s.x;
                        //        if ("y" in s) y += s.y;
                        //}
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
                            //// Quadratic Beziers
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
                            //case "q":
                            //case "Q":
                            //    var pointlist = GeometryUtil.QuadraticBezier.linearize({ x: prevx, y: prevy }, { x: x, y: y }, { x: x1, y: y1 }, this.conf.tolerance);
                            //    pointlist.shift(); // firstpoint would already be in the poly
                            //    for (var j = 0; j < pointlist.Count(); j++)
                            //    {
                            //        var point = { };
                            //        point.x = pointlist[j].x;
                            //        point.y = pointlist[j].y;
                            //        poly.push(point);
                            //    }
                            //    break;
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
                            //    var pointlist = GeometryUtil.CubicBezier.linearize({ x: prevx, y: prevy }, { x: x, y: y }, { x: x1, y: y1 }, { x: x2, y: y2 }, this.conf.tolerance);
                            //    pointlist.shift(); // firstpoint would already be in the poly
                            //    for (var j = 0; j < pointlist.Count(); j++)
                            //    {
                            //        var point = { };
                            //        point.x = pointlist[j].x;
                            //        point.y = pointlist[j].y;
                            //        poly.push(point);
                            //    }
                            //    break;
                            //case "a":
                            //case "A":
                            //    var pointlist = GeometryUtil.Arc.linearize({ x: prevx, y: prevy }, { x: x, y: y }, s.r1, s.r2, s.angle, s.largeArcFlag, s.sweepFlag, this.conf.tolerance);
                            //    pointlist.shift();

                            //    for (var j = 0; j < pointlist.Count(); j++)
                            //    {
                            //        var point = { };
                            //        point.x = pointlist[j].x;
                            //        point.y = pointlist[j].y;
                            //        poly.push(point);
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
            while (poly.Count() > 0 && GeometryUtil.almostEqual(poly[0].X, poly[poly.Count() - 1].X, this.conf.toleranceSvg) && 
                GeometryUtil.almostEqual(poly[0].Y, poly[poly.Count() - 1].Y, this.conf.toleranceSvg))
            {
                poly.Pop();
            }

            return poly.ToArray();
        }
    }
}