using ClipperLib;
using SvgLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class LabelSvgGroup
    {
        public string Label { get; set; }
        public SvgGroup Group { get; set; }
        public DoublePoint GroupLocation { get; set; }
        public int ParentGroupId { get; set; }
        public double Width { get; set; }
        public double ScaledWidth { get; set; }
        public double Height { get; set; }
        public double ScaledHeight { get; set; }
    }
}
