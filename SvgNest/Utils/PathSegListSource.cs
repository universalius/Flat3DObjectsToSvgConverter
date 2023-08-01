using SvgNest.Models.SVGPathSeg;

namespace SvgNest.Utils
{
    public class PathSegListSource
    {
        private string _string;
        private int _currentIndex = 0;
        private int _endIndex;
        private int _previousCommand;

        public PathSegListSource(string @string)
        {
            _string = @string;
            _endIndex = _string.Count();
            _previousCommand = SVGPathSeg.PATHSEG_UNKNOWN;

            //_skipOptionalSpaces();
        }

        public bool InitialCommandIsMoveTo()
        {
            // If the path is empty it is still valid, so return true.
            if (!HasMoreData())
                return true;
            var command = PeekSegmentType();
            // Path must start with moveTo.
            return command == SVGPathSeg.PATHSEG_MOVETO_ABS || command == SVGPathSeg.PATHSEG_MOVETO_REL;
        }

        public bool HasMoreData()
        {
            return _currentIndex < _endIndex;
        }

        public int PeekSegmentType()
        {
            var lookahead = _string[_currentIndex];
            return PathSegTypeFromChar(lookahead.ToString());
        }

        public int PathSegTypeFromChar(string lookahead)
        {
            switch (lookahead)
            {
                case "Z":
                case "z":
                    return SVGPathSeg.PATHSEG_CLOSEPATH;
                case "M":
                    return SVGPathSeg.PATHSEG_MOVETO_ABS;
                case "m":
                    return SVGPathSeg.PATHSEG_MOVETO_REL;
                case "L":
                    return SVGPathSeg.PATHSEG_LINETO_ABS;
                case "l":
                    return SVGPathSeg.PATHSEG_LINETO_REL;
                case "C":
                    return SVGPathSeg.PATHSEG_CURVETO_CUBIC_ABS;
                case "c":
                    return SVGPathSeg.PATHSEG_CURVETO_CUBIC_REL;
                case "Q":
                    return SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_ABS;
                case "q":
                    return SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_REL;
                case "A":
                    return SVGPathSeg.PATHSEG_ARC_ABS;
                case "a":
                    return SVGPathSeg.PATHSEG_ARC_REL;
                case "H":
                    return SVGPathSeg.PATHSEG_LINETO_HORIZONTAL_ABS;
                case "h":
                    return SVGPathSeg.PATHSEG_LINETO_HORIZONTAL_REL;
                case "V":
                    return SVGPathSeg.PATHSEG_LINETO_VERTICAL_ABS;
                case "v":
                    return SVGPathSeg.PATHSEG_LINETO_VERTICAL_REL;
                case "S":
                    return SVGPathSeg.PATHSEG_CURVETO_CUBIC_SMOOTH_ABS;
                case "s":
                    return SVGPathSeg.PATHSEG_CURVETO_CUBIC_SMOOTH_REL;
                case "T":
                    return SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_SMOOTH_ABS;
                case "t":
                    return SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_SMOOTH_REL;
                default:
                    return SVGPathSeg.PATHSEG_UNKNOWN;
            }
        }

        public SVGPathSeg ParseSegment(SVGPathSegList owningPathSegList)
        {
            var lookahead = _string[_currentIndex].ToString();
            var command = PathSegTypeFromChar(lookahead);
            if (command == SVGPathSeg.PATHSEG_UNKNOWN)
            {
                // Possibly an implicit command. Not allowed if this is the first command.
                if (_previousCommand == SVGPathSeg.PATHSEG_UNKNOWN)
                    return null;
                command = NextCommandHelper(lookahead, _previousCommand);
                if (command == SVGPathSeg.PATHSEG_UNKNOWN)
                    return null;
            }
            else
            {
                _currentIndex++;
            }

            _previousCommand = command;

            switch (command)
            {
                //case SVGPathSeg.PATHSEG_MOVETO_REL:
                //    return new SVGPathSegMovetoRel(owningPathSegList, _parseNumber(), _parseNumber());
                case SVGPathSeg.PATHSEG_MOVETO_ABS:
                    return new SVGPathSeg(SVGPathSeg.PATHSEG_MOVETO_ABS, "M", owningPathSegList)
                    {
                        X = ParseNumber().Value,
                        Y = ParseNumber().Value
                    };
                //case SVGPathSeg.PATHSEG_LINETO_REL:
                //    return new SVGPathSegLinetoRel(owningPathSegList, _parseNumber(), _parseNumber());
                case SVGPathSeg.PATHSEG_LINETO_ABS:
                    return new SVGPathSeg(SVGPathSeg.PATHSEG_LINETO_ABS, "L", owningPathSegList)
                    {
                        X = ParseNumber().Value,
                        Y = ParseNumber().Value
                    };

                //return new SVGPathSegLinetoAbs(owningPathSegList, _parseNumber(), _parseNumber()); //!!!
                //case SVGPathSeg.PATHSEG_LINETO_HORIZONTAL_REL:
                //    return new SVGPathSegLinetoHorizontalRel(owningPathSegList, _parseNumber());
                //case SVGPathSeg.PATHSEG_LINETO_HORIZONTAL_ABS:
                //    return new SVGPathSegLinetoHorizontalAbs(owningPathSegList, _parseNumber());
                //case SVGPathSeg.PATHSEG_LINETO_VERTICAL_REL:
                //    return new SVGPathSegLinetoVerticalRel(owningPathSegList, _parseNumber());
                //case SVGPathSeg.PATHSEG_LINETO_VERTICAL_ABS:
                //    return new SVGPathSegLinetoVerticalAbs(owningPathSegList, _parseNumber());
                case SVGPathSeg.PATHSEG_CLOSEPATH:
                    SkipOptionalSpaces();
                    return new SVGPathSeg(SVGPathSeg.PATHSEG_CLOSEPATH, "z", owningPathSegList);

                //return new SVGPathSegClosePath(owningPathSegList); //!!!
                //    case SVGPathSeg.PATHSEG_CURVETO_CUBIC_REL:
                //        var points = { x1: _parseNumber(), y1: _parseNumber(), x2: _parseNumber(), y2: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoCubicRel(owningPathSegList, points.x, points.y, points.x1, points.y1, points.x2, points.y2);
                //        case SVGPathSeg.PATHSEG_CURVETO_CUBIC_ABS:
                //    var points = { x1: _parseNumber(), y1: _parseNumber(), x2: _parseNumber(), y2: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoCubicAbs(owningPathSegList, points.x, points.y, points.x1, points.y1, points.x2, points.y2);
                //        case SVGPathSeg.PATHSEG_CURVETO_CUBIC_SMOOTH_REL:
                //    var points = { x2: _parseNumber(), y2: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoCubicSmoothRel(owningPathSegList, points.x, points.y, points.x2, points.y2);
                //        case SVGPathSeg.PATHSEG_CURVETO_CUBIC_SMOOTH_ABS:
                //    var points = { x2: _parseNumber(), y2: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoCubicSmoothAbs(owningPathSegList, points.x, points.y, points.x2, points.y2);
                //        case SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_REL:
                //    var points = { x1: _parseNumber(), y1: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoQuadraticRel(owningPathSegList, points.x, points.y, points.x1, points.y1);
                //        case SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_ABS:
                //    var points = { x1: _parseNumber(), y1: _parseNumber(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegCurvetoQuadraticAbs(owningPathSegList, points.x, points.y, points.x1, points.y1);
                //        case SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_SMOOTH_REL:
                //    return new SVGPathSegCurvetoQuadraticSmoothRel(owningPathSegList, _parseNumber(), _parseNumber());
                //case SVGPathSeg.PATHSEG_CURVETO_QUADRATIC_SMOOTH_ABS:
                //    return new SVGPathSegCurvetoQuadraticSmoothAbs(owningPathSegList, _parseNumber(), _parseNumber());
                //case SVGPathSeg.PATHSEG_ARC_REL:
                //    var points = { x1: _parseNumber(), y1: _parseNumber(), arcAngle: _parseNumber(), arcLarge: _parseArcFlag(), arcSweep: _parseArcFlag(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegArcRel(owningPathSegList, points.x, points.y, points.x1, points.y1, points.arcAngle, points.arcLarge, points.arcSweep);
                //        case SVGPathSeg.PATHSEG_ARC_ABS:
                //    var points = { x1: _parseNumber(), y1: _parseNumber(), arcAngle: _parseNumber(), arcLarge: _parseArcFlag(), arcSweep: _parseArcFlag(), x: _parseNumber(), y: _parseNumber()};
                //return new SVGPathSegArcAbs(owningPathSegList, points.x, points.y, points.x1, points.y1, points.arcAngle, points.arcLarge, points.arcSweep);
                default:
                    throw new NotImplementedException("Unknown path seg type.");
            }
        }

        // Parse a number from an SVG path. This very closely follows genericParseNumber(...) from Source/core/svg/SVGParserUtilities.cpp.
        // Spec: http://www.w3.org/TR/SVG11/single-page.html#paths-PathDataBNF
        public double? ParseNumber()
        {
            var exponent = 0;
            var integer = 0;
            var frac = 1.0;
            var @decimal = 0.0;
            var sign = 1;
            var expsign = 1;

            var startIndex = _currentIndex;

            SkipOptionalSpaces();

            var getChar = () => _string[_currentIndex].ToString();

            // Read the sign.
            if (_currentIndex < _endIndex && getChar() == "+")
                _currentIndex++;
            else if (_currentIndex < _endIndex && getChar() == "-")
            {
                _currentIndex++;
                sign = -1;
            }

            int charInt;
            if (_currentIndex == _endIndex || !int.TryParse(getChar(), out charInt) && getChar() != ".")
                // The first character of a number must be one of [0-9+-.].
                return null;

            // Read the integer part, build right-to-left.
            var startIntPartIndex = _currentIndex;
            while (_currentIndex < _endIndex && int.TryParse(getChar(), out charInt))
                _currentIndex++; // Advance to first non-digit.

            if (_currentIndex != startIntPartIndex)
            {
                var scanIntPartIndex = _currentIndex - 1;
                var multiplier = 1;
                while (scanIntPartIndex >= startIntPartIndex)
                {
                    integer += multiplier * int.Parse(_string[scanIntPartIndex--].ToString());
                    multiplier *= 10;
                }
            }

            // Read the decimals.
            if (_currentIndex < _endIndex && getChar() == ".")
            {
                _currentIndex++;

                // There must be a least one digit following the .
                if (_currentIndex >= _endIndex || !int.TryParse(getChar(), out charInt))
                    return null;
                while (_currentIndex < _endIndex && int.TryParse(getChar(), out charInt))
                {
                    frac *= 10;
                    @decimal += charInt / frac;
                    _currentIndex += 1;
                }
            }

            // Read the exponent part.
            if (_currentIndex != startIndex && _currentIndex + 1 < _endIndex &&
                getChar().ToLowerInvariant() == "e" &&
                _string[_currentIndex + 1].ToString() != "x" && _string[_currentIndex + 1].ToString() != "m")
            {
                _currentIndex++;

                // Read the sign of the exponent.
                if (getChar() == "+")
                {
                    _currentIndex++;
                }
                else if (getChar() == "-")
                {
                    _currentIndex++;
                    expsign = -1;
                }

                // There must be an exponent.
                if (_currentIndex >= _endIndex || !int.TryParse(getChar(), out charInt))
                    return null;

                while (_currentIndex < _endIndex && int.TryParse(getChar(), out charInt))
                {
                    exponent *= 10;
                    exponent += charInt;
                    _currentIndex++;
                }
            }

            var number = integer + @decimal;
            number *= sign;

            if (exponent != 0)
                number *= Math.Pow(10, expsign * exponent);

            if (startIndex == _currentIndex)
                return null;

            SkipOptionalSpacesOrDelimiter();

            return number;
        }

        private bool IsCurrentSpace()
        {
            var character = _string[_currentIndex].ToString();
            return character == " " || character == "\n" || character == "\t" || character == "\r" || character == "\f";
        }

        private bool SkipOptionalSpaces()
        {
            while (_currentIndex < _endIndex && IsCurrentSpace())
                _currentIndex++;
            return _currentIndex < _endIndex;
        }

        private bool SkipOptionalSpacesOrDelimiter()
        {
            if (_currentIndex < _endIndex && !IsCurrentSpace() && _string[_currentIndex].ToString() != ",")
                return false;
            if (SkipOptionalSpaces())
            {
                if (_currentIndex < _endIndex && _string[_currentIndex].ToString() == ",")
                {
                    _currentIndex++;
                    SkipOptionalSpaces();
                }
            }
            return _currentIndex < _endIndex;
        }

        private int NextCommandHelper(string lookahead, int previousCommand)
        {
            // Check for remaining coordinates in the current command.
            if ((lookahead == "+" || lookahead == "-" || lookahead == "." ||
                int.TryParse(lookahead, out int lookaheadInt) && lookaheadInt >= 0 && lookaheadInt <= 9) && previousCommand != SVGPathSeg.PATHSEG_CLOSEPATH)
            {
                if (previousCommand == SVGPathSeg.PATHSEG_MOVETO_ABS)
                    return SVGPathSeg.PATHSEG_LINETO_ABS;
                if (previousCommand == SVGPathSeg.PATHSEG_MOVETO_REL)
                    return SVGPathSeg.PATHSEG_LINETO_REL;
                return previousCommand;
            }
            return SVGPathSeg.PATHSEG_UNKNOWN;
        }
    }
}
