using SvgNest.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.SvgNest
{
    public class SvgNestConfig
    {
        public double curveTolerance = 0.3;
        public int spacing = 0;
        public int rotations = 4;
        public int populationSize = 10;
        public int mutationRate = 10;
        public bool useHoles = false;
        public bool exploreConcave = false;
    }
}
