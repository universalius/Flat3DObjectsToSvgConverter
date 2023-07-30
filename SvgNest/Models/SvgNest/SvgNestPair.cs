using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.SvgNest
{
    public class SvgNestPair
    {
        public int A { get; set; }
        public int B { get; set; }
        public bool inside { get; set; }
        public double Arotation { get; set; }
        public double Brotation { get; set; }
    }
}
