using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models
{
    public class SvgNestPair
    {
        public int A { get; set; }
        public int B { get; set; }
        public bool Inside { get; set; }
        public double ARotation { get; set; }
        public double BRotation { get; set; }
    }
}
