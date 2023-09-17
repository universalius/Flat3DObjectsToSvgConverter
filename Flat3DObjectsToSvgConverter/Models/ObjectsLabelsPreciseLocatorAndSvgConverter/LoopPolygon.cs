using Flat3DObjectsToSvgConverter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class LoopPolygon
    {
        public LoopPath LoopPath { get; set; }
        public LabelSvgGroup LabelLetters { get; set; }
        public PathPolygon Polygon { get; set; }
        public IEnumerable<LoopPolygon> Neighbors { get; set; }
    }
}
