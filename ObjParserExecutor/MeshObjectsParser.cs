using ObjParser;
using ObjParser.Types;
using ObjParserExecutor.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjParserExecutor
{
    public class MeshObjectsParser
    {
        public IEnumerable<MeshObject> Parse(Obj obj)
        {
            var xAxisVerts = obj.VertexList.GroupBy(x => x.X);
            var yAxisVerts = obj.VertexList.GroupBy(x => x.Y);
            var zAxisVerts = obj.VertexList.GroupBy(x => x.Z);

            var vertexGroups = new[] {
                new { VertexGroup= xAxisVerts, VertexCount = xAxisVerts.Select(g => g.Count()).Max(), Axis = "x" },
                new { VertexGroup= yAxisVerts, VertexCount = yAxisVerts.Select(g => g.Count()).Max(), Axis = "y" },
                new { VertexGroup= zAxisVerts, VertexCount = zAxisVerts.Select(g => g.Count()).Max(), Axis = "z" },
            };

            var vertCounts = vertexGroups.Select(g => g.VertexCount);
            if (vertCounts.Distinct().Count() != vertCounts.Count())
            {
                Console.WriteLine("Can not distinguish axis");
                Console.ReadKey();
                return null;
            }

            var targetAxisVerts = vertexGroups.First(v => v.VertexCount == vertCounts.Max());
            var targetAxis = targetAxisVerts.Axis;
            var meshObjects = targetAxisVerts.VertexGroup.GroupBy(g => g.Count());

            return meshObjects.Select(mo =>
            {
                var verts = mo.Select(g => g);
                var pararelVerts = verts
                    .SelectMany(g => g.ToList())
                    .Select(v => new
                    {
                        Id = AxisSelectHelpers.GetPararelVertsIdByAxis(targetAxis, v),
                        Vertex = v
                    })
                    .GroupBy(v => v.Id);

                var edgesVertsIndexes = pararelVerts.Select(g => g.Select(v => v.Vertex.Index));

                var targetFaces = obj.FaceList
                    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(evi).Count() == evi.Count()));

                //var b = obj.FaceList
                //    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(new[] {1, 36}).Any()));

                return new MeshObject
                {
                    PlainVerts = verts.First(),
                    Faces = targetFaces,
                    Axis = targetAxis,
                };
            });



            //var distinctTargetFaces = targetFaces.DistinctBy(f => f.VertexIndexList);


        }
    }

    public class MeshObject
    {
        public IEnumerable<Vertex> PlainVerts { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public string Axis { get; set; }
    }
}
