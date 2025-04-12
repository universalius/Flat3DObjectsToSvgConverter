using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjParser;

namespace Flat3DObjectsToSvgConverter.Models
{
    public class Mesh
    {
        public string Name { get; set; }
        public Obj Obj { get; set; }
    }
}
