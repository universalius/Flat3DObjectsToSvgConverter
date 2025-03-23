using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models
{
    public class MeshObjects
    {
        public string MeshName { get; set; }

        public IEnumerable<ObjectLoops> Objects { get; set; }

        public MeshObjects Clone()
        {
            var clone = new MeshObjects
            {
                MeshName = MeshName,
                Objects = Objects.ToList().Select(o => o.Clone()).ToArray()
            };

            return clone;
        }
    }
}
