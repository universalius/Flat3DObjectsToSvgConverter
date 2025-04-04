﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class ObjectLoops
    {
        public IEnumerable<LoopPoints> Loops { get; set; }

        public ObjectLoops Clone()
        {
            var clone = new ObjectLoops
            {
                Loops = Loops.ToList().Select(l => l.Clone()).ToArray(),
            };

            return clone;
        }
    }
}
