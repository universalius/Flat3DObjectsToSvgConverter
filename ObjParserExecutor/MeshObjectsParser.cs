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
            var xOrientedPlanes = obj.VertexList.GroupBy(x => x.X);
            var yOrientedPlanes = obj.VertexList.GroupBy(x => x.Y);
            var zOrientedPlanes = obj.VertexList.GroupBy(x => x.Z);

            var axisPlanes = new[] {
                new { Planes= xOrientedPlanes, VertexCount = xOrientedPlanes.Select(g => g.Count()).Max(), Axis = "x", PlanesCount = xOrientedPlanes.Count() },
                new { Planes= yOrientedPlanes, VertexCount = yOrientedPlanes.Select(g => g.Count()).Max(), Axis = "y", PlanesCount = yOrientedPlanes.Count() },
                new { Planes= zOrientedPlanes, VertexCount = zOrientedPlanes.Select(g => g.Count()).Max(), Axis = "z", PlanesCount = zOrientedPlanes.Count() },
            };

            var targetAxisVerts = axisPlanes.FirstOrDefault(p => p.PlanesCount % 2 == 0);
            if (targetAxisVerts == null)
            {
                throw new Exception($"Mesh {mesh.Name} object consists of one plain only, but extect always two parralel");
            }

            var targetAxis = targetAxisVerts.Axis;
            var orderedMeshObjects = targetAxisVerts.Planes.OrderBy(g => g.Key); // parallel vert planes should be in pairs between plains in pair should be 4 mms

            if (mesh.Name.ToLower() == "74_bearing")
            {
                var c = 0;
            }

            return orderedMeshObjects.Chunks(2).Select((mo, i) =>
            {
                var verts = mo.Select(g => g);
                var paralelVerts = verts
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

                var edgesVertsIndexes = paralelVerts.Select(g => g.Select(v => v.Vertex.Index));

                var targetFaces = obj.FaceList
                    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(evi).Count() == evi.Count()));

                //if (mesh.Name.ToLower() == "74_bearing" && targetFaces.Any(f => f.Id == 264))
                //{
                //    var c = 0;
                //}


                //var b = obj.FaceList
                //    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(new[] { 109 }).Any()));

                Console.WriteLine($"    Finished parse mesh - {mesh.Name}, objectId - {i}");

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
