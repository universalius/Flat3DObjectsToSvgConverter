using SvgNest.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models
{
    public class SvgNestConfig
    {
        public double CurveTolerance = 0.3;
        public int Spacing = 0;
        public int Rotations = 4;
        public int PopulationSize = 10;
        public int MutationRate = 10;
        public bool UseHoles = false;
        public bool ExploreConcave = false;
    }
}
