using ObjParser;
using ObjParser.Types;
using ObjParserExecutor.Extensions;
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
        public IEnumerable<MeshObject> Parse(Mesh mesh)
        {
            var obj = mesh.Obj;
            var xLines = obj.VertexList.GroupBy(x => x.X);
            var yLines = obj.VertexList.GroupBy(x => x.Y);
            var zLines = obj.VertexList.GroupBy(x => x.Z);

            var axisPlanes = new[] {
                new { Lines= xLines, VertexCount = xLines.Select(g => g.Count()).Max(), Axis = "x", LinesCount = xLines.Count() },
                new { Lines= yLines, VertexCount = yLines.Select(g => g.Count()).Max(), Axis = "y", LinesCount = yLines.Count() },
                new { Lines= zLines, VertexCount = zLines.Select(g => g.Count()).Max(), Axis = "z", LinesCount = zLines.Count() },
            };

            var targetAxisVerts = axisPlanes.FirstOrDefault(p => p.LinesCount % 2 == 0); //vertexGroups.First(v => v.VertexCount == vertCounts.Max());
            if (targetAxisVerts == null)
            {
                throw new Exception($"Mesh {mesh.Name} object consists of one plain only, but extect always two parralel");
            }

            var targetAxis = targetAxisVerts.Axis;
            //var meshObjects = targetAxisVerts.VertexGroup.GroupBy(g => g.Count());

            var orderedMeshObjects = targetAxisVerts.Lines.OrderBy(g => g.Key); // parallel vert planes should be in pairs between plains in pair should be 4 mms

            if (mesh.Name.ToLower() == "74_bearing")
            {
                var c = 0;
            }

            return orderedMeshObjects.Chunks(2).Select((mo, i) =>
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

                //if (Math.Abs(pararelVerts.First().Key - pararelVerts.Last().Key) ! = 4 )
                //{

                //}

                var edgesVertsIndexes = pararelVerts.Select(g => g.Select(v => v.Vertex.Index));

                var targetFaces = obj.FaceList
                    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(evi).Count() == evi.Count()));

                if (mesh.Name.ToLower() == "74_bearing" && targetFaces.Any(f => f.Id == 264))
                {
                    var c = 0;
                }


                //var b = obj.FaceList
                //    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(new[] { 109 }).Any()));

                Console.WriteLine($"    Finished parse mesh - {mesh.Name}, object - {i}");

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
