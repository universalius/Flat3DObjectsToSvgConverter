using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjParserExecutor.Helpers
{
    public static class AxisSelectHelpers
    {
        public static string GetPararelVertsIdByAxis(string axis, Vertex vertex)
        {
            if (axis.ToLower() == "x")
                return $"{vertex.Y} {vertex.Z}";

            if (axis.ToLower() == "y")
                return $"{vertex.X} {vertex.Z}";

            if (axis.ToLower() == "z")
                return $"{vertex.X} {vertex.Y}";

            throw new NotImplementedException();
        }

        public static PointF GetPointByAxis(string axis, Vertex vertex, int mmGain)
        {
            double x = 0;
            double y = 0;

            if (axis.ToLower() == "x")
            {
                x = vertex.Y;
                y = vertex.Z;
            }

            if (axis.ToLower() == "y")
            {
                x = vertex.X;
                y = vertex.Z;
            }

            if (axis.ToLower() == "z")
            {
                x = vertex.X;
                y = vertex.Y;
            }

            if (x == 0 && y == 0)
                throw new NotImplementedException();

            return new PointF((float)(x * mmGain), (float)(y * mmGain));
        }
    }
}
