using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models
{
    public class Individual
    {
        public List<Node> Placement { get; set; }
        public List<double> Rotation { get; set; }
        public double Fitness { get; set; }
    }
}
