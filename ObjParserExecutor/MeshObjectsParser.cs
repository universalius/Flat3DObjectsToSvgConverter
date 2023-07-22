using ObjParser;
using ObjParser.Types;
using ObjParserExecutor.Extensions;
using ObjParserExecutor.Helpers;
using ObjParserExecutor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjParserExecutor
{
    public class MeshObjectsParser
    {
        public IEnumerable<MeshObject> Parse(Mesh mesh)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine($"    Starting parse");

            var obj = mesh.Obj;
            var xOrientedPlanes = obj.VertexList.GroupBy(x => x.X).ToList();
            var yOrientedPlanes = obj.VertexList.GroupBy(x => x.Y).ToList();
            var zOrientedPlanes = obj.VertexList.GroupBy(x => x.Z).ToList();

            var axisesPlanes = new[] {
                new { Planes= xOrientedPlanes, VertexCount = xOrientedPlanes.Select(g => g.Count()).Max(), Axis = "x", PlanesCount = xOrientedPlanes.Count() },
                new { Planes= yOrientedPlanes, VertexCount = yOrientedPlanes.Select(g => g.Count()).Max(), Axis = "y", PlanesCount = yOrientedPlanes.Count() },
                new { Planes= zOrientedPlanes, VertexCount = zOrientedPlanes.Select(g => g.Count()).Max(), Axis = "z", PlanesCount = zOrientedPlanes.Count() },
            };

            var axisesBoxPlanes = axisesPlanes.Where(p => p.PlanesCount % 2 == 0).ToList();
            if (!axisesBoxPlanes.Any())
            {
                throw new Exception("Object box contains more then two paralel planes in one axis, can not distinguish box orientation");
            }

            var maxAxisesVertsCount = axisesBoxPlanes.Select(ap => ap.VertexCount).Max();
            var targetAxisVerts = axisesPlanes.First(p => p.VertexCount == maxAxisesVertsCount);

            var targetAxis = targetAxisVerts.Axis;
            var orderedMeshObjects = targetAxisVerts.Planes.OrderBy(g => g.Key).ToList(); // parallel vert planes should be in pairs between plains in pair should be 4 mms

            //if (mesh.Name.ToLower() == "74_bearing")
            //{
            //    var c = 0;
            //}

            var meshObjects = orderedMeshObjects.Chunks(2).Select((mo, i) =>
            {
                var verts = mo.Select(g => g).ToList();
                var paralelVerts = verts
                    .SelectMany(g => g.ToList())
                    .Select(v => new
                    {
                        Id = AxisSelectHelpers.GetPararelVertsIdByAxis(targetAxis, v),
                        Vertex = v
                    })
                    .GroupBy(v => v.Id)
                    .Where(g => g.Count() > 1)
                    .ToList();

                // can contain edges that match totally by vertexes, so in pair group can be more then 2 verts
                var edgesVerts = paralelVerts.Select(g => g.Select(v => v.Vertex)).ToList();
                var edgesVertsIndexes = edgesVerts.Select(ev => ev.Select(v => v.Index)).ToList();

                // get faces that perpendicular to parralel box planes with most verts in one axis
                var targetFaces = obj.FaceList
                    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(evi).Count() == 2))
                    .DistinctBy(f => f.Id).ToList();

                //if (mesh.Name.ToLower() == "74_bearing" && targetFaces.Any(f => f.Id == 264))
                //{
                //    var c = 0;
                //}

                //var b = obj.FaceList
                //    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(new[] { 109 }).Any()));

                Console.WriteLine($"        Finished parse object - {i+1}");

                var firstVertOfAllEdges = edgesVerts.SelectMany(ev => ev.Select(v => v))
                    .GroupBy(v => AxisSelectHelpers.GetVertCoordinateByAxis(targetAxis, v))
                    .First()
                    .Select(v => v)
                    .ToList();

                return new MeshObject
                {
                    PlainVerts = firstVertOfAllEdges,
                    Faces = targetFaces,
                    Axis = targetAxis,
                };
            }).ToList();

            watch.Stop();
            Console.WriteLine($"    Finished mesh parse, found {meshObjects.Count} objects, took - {watch.ElapsedMilliseconds / 1000.0} sec");

            return meshObjects;
        }
    }

    public class MeshObject
    {
        public IEnumerable<Vertex> PlainVerts { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public string Axis { get; set; }
    }
}
