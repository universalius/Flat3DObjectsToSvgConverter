using SvgLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class LoopPath
    {
        public SvgPath Path { get; set; }
        public int ParentGroupId { get; set; }
        public string ScaledPath { get; set; }
        public IEnumerable<SvgPath> SiblingPaths { get; set; }
    }
}
