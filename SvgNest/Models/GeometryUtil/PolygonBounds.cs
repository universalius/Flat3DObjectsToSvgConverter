﻿using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.GeometryUtil
{
    public class PolygonBounds : DoublePoint
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
