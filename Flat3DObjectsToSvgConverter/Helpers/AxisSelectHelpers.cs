using GeometRi;
using ObjParser;
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
            return GetIdByAxis(axis, new Point3d(vertex.X, vertex.Y, vertex.Z));
        }

        public static string GetIdByAxis(string axis, Point3d point)
        {
            if (axis.ToLower() == "x")
                return $"{point.Y} {point.Z}";

            if (axis.ToLower() == "y")
                return $"{point.X} {point.Z}";

            if (axis.ToLower() == "z")
                return $"{point.X} {point.Y}";

            throw new NotImplementedException();
        }

        public static double GetVertCoordinateByAxis(string axis, Vertex vertex)
        {
            return GetCoordinateByAxis(axis, new Point3d(vertex.X, vertex.Y, vertex.Z));
        }

        public static double GetCoordinateByAxis(string axis, Point3d point)
        {
            if (axis.ToLower() == "x")
                return point.X;

            if (axis.ToLower() == "y")
                return point.Y;

            if (axis.ToLower() == "z")
                return point.Z;

            throw new NotImplementedException();
        }

        public static PointF GetPointByAxis(string axis, Vertex vertex, int mmGain = 1)
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

        public static (PointF minPoint, PointF maxPoint) GetXYBoundaries(string axis, Extent size)
        {
            double minX = 0;
            double minY = 0;
            double maxX = 0;
            double maxY = 0;

            if (axis.ToLower() == "x")
            {
                minX = size.YMin;
                maxX = size.YMax;
                minY = size.ZMin;
                maxY = size.ZMax;
            }

            if (axis.ToLower() == "y")
            {
                minX = size.XMin;
                maxX = size.XMax;
                minY = size.ZMin;
                maxY = size.ZMax;
            }

            if (axis.ToLower() == "z")
            {
                minX = size.XMin;
                maxX = size.XMax;
                minY = size.YMin;
                maxY = size.YMax;
            }

            if (minX == 0 && minY == 0)
                throw new NotImplementedException();

            return (new PointF((float)minX, (float)minY), new PointF((float)maxX, (float)maxY));
        }

    }
}
