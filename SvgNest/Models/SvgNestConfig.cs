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
        public double CurveTolerance { get; set; } = 0.3;
        public int Spacing { get; set; } = 0;
        public int Rotations { get; set; } = 4;
        public int PopulationSize { get; set; } = 10;
        public int MutationRate { get; set; } = 10;
        public bool UseHoles { get; set; } = false;
        public bool ExploreConcave { get; set; } = false;
    }
}
