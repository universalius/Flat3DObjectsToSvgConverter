using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.SVGPathSeg
{
    public class PathSegListBuilder
    {
        public PathSegListBuilder()
        {
            PathSegList = new List<SVGPathSeg>();
        }

        public List<SVGPathSeg> PathSegList { get; set; }

        public void appendSegment(SVGPathSeg pathSeg)
        {
            PathSegList.Add(pathSeg);
        }
    }

}
