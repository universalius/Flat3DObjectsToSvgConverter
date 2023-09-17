using SvgLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class SvgLetter
    {
        public string Letter { get; set; }
        public SvgPath Path { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
