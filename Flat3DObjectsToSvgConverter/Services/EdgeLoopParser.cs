using ObjParser;
using ObjParser.Types;
using ObjParserExecutor.Helpers;
using ObjParserExecutor.Models;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class EdgeLoopParser
    {
        public MeshObjectsLoops GetMeshObjectsLoops(MeshObject meshObject)
        {
            var watch = Stopwatch.StartNew();

            var verts = meshObject.PlainVerts;
            var faces = meshObject.Faces;
            var axis = meshObject.Axis;

            var vertIndexes = verts.Select(v => v.Index);
            var vertsDictionary = verts.ToDictionary(v => v.Index);

            var vertexFaces = verts.Select(v =>
                new VertexFaces
                {
                    Vertex = v,
                    Faces = faces.Where(f => f.VertexIndexList.Contains(v.Index)).ToList()
                }).ToList();

            var objSize = Obj.GetObjSize(verts);
            var objBoundaries = AxisSelectHelpers.GetXYBoundaries(axis, objSize);
            var initialVertex = verts.First(v => AxisSelectHelpers.GetPointByAxis(axis, v).X == objBoundaries.minPoint.X);

            var firstLoopEdges = GetLoopEdges(initialVertex, vertexFaces);

            var allLoopsEdges = GetObjectsLoops(faces, vertexFaces, new List<LoopEdges> { firstLoopEdges });

            var groupedLoops = GroupLoopsByBoundaries(axis, allLoopsEdges);

            var objectsWithHoles = groupedLoops.Where(gl => gl.Children.Any()).Select(g =>
           {
               var loops = new List<LoopPoints> { new LoopPoints { Points = GetLoopPoints(axis, g.Main.Edges) } };
               loops.AddRange(g.Children.Select(child => new LoopPoints { Points = GetLoopPoints(axis, child.Edges) }).ToList());
               return new ObjectLoops { Loops = loops };
           }).ToList();

            var objectsWithOutHoles = groupedLoops.Where(gl => !gl.Children.Any()).Select(gl => new ObjectLoops
            {
                Loops = new[] { new LoopPoints { Points = GetLoopPoints(axis, gl.Main.Edges) } }
            }).ToList();

            var objects = objectsWithHoles.Concat(objectsWithOutHoles);

            watch.Stop();
            Console.WriteLine($"    Converted to loops mesh objects - {objects.Count()}, took - {watch.ElapsedMilliseconds / 1000.0} s");

            return new MeshObjectsLoops { Objects = objects };
        }

        private List<LoopEdges> GetObjectsLoops(IEnumerable<Face> faces, IEnumerable<VertexFaces> vertexFaces,
            List<LoopEdges> loopsEdges)
        {
            var leftFaces = faces.Except(loopsEdges.SelectMany(le => le.Edges.Select(ef => ef.Face))).ToList();
            if (!leftFaces.Any() || leftFaces.Count() < 4) // hole or sub object should have at least 4 sides
            {
                return loopsEdges;
            }

            // perfomance optimisation
            var vertsInLoopsIds = loopsEdges.SelectMany(l => l.Edges.SelectMany(ef => ef.Edge)).Select(v => v.Index).ToList();
            vertexFaces = vertexFaces.Where(vf => !vertsInLoopsIds.Contains(vf.Vertex.Index)).ToList();
            var vertIndexes = vertexFaces.Select(vf => vf.Vertex.Index).ToList();

            var holeFirstVertIndex = leftFaces.First().VertexIndexList.Intersect(vertIndexes).FirstOrDefault();
            if (holeFirstVertIndex == 0)
            {
                //var holeFirstVertFaceVerts = holesFaces.First().VertexIndexList
                throw new Exception("Mesh has defects, some edges not paralel to each other");
            }

            var holeVertexFaces = vertexFaces.First(vf => vf.Vertex.Index == holeFirstVertIndex);
            var holeLoopEdges = GetLoopEdges(holeVertexFaces.Vertex, vertexFaces);

            loopsEdges.Add(holeLoopEdges);

            return GetObjectsLoops(faces, vertexFaces, loopsEdges);
        }

        private LoopEdges GetLoopEdges(Vertex initialVertex, IEnumerable<VertexFaces> vertexFaces)
        {
            var watch = Stopwatch.StartNew();

            var firstVertex = initialVertex;
            EdgeFace edgeFace = null;
            var loopEdgeFaces = new List<EdgeFace>();

            while ((edgeFace?.SecondVertex?.Index ?? 0) != initialVertex.Index)
            {
                edgeFace = GetEdgeFace(firstVertex, vertexFaces, edgeFace?.FirstVertex?.Index, loopEdgeFaces);
                loopEdgeFaces.Add(edgeFace);
                firstVertex = edgeFace.SecondVertex;
            }

            watch.Stop();
            Console.WriteLine($"        GetEdgeLoop edges - {loopEdgeFaces.Count()}, took - {watch.ElapsedMilliseconds / 1000.0} s");

            return new LoopEdges { Edges = loopEdgeFaces };
        }

        private EdgeFace GetEdgeFace(Vertex firstVertex, IEnumerable<VertexFaces> vertexFaces, int? prevVertIndex, List<EdgeFace> edgeFaces)
        {
            var firstVertexFaces = vertexFaces.First(vf => vf.Vertex.Index == firstVertex.Index);
            var nextFace = prevVertIndex == null ?
                firstVertexFaces.Faces.First() :
                firstVertexFaces.Faces.FirstOrDefault(f => !f.VertexIndexList.Contains(prevVertIndex.Value));

            if (nextFace == null)
            {
                //var d = vertexFaces.Where(vf => vf.Faces.Count() < 2).ToList();
                var face = firstVertexFaces.Faces.First();
                var faceVerts = vertexFaces.Where(v => face.VertexIndexList.Contains(v.Vertex.Index));
                throw new Exception("Can not find next face");
            }

            var nextFaceVertIndexes = nextFace.VertexIndexList.Except(new[] { firstVertex.Index }).ToList();
            var nextVertIndexes = vertexFaces.Where(vf => nextFaceVertIndexes.Contains(vf.Vertex.Index)).ToList();

            if (nextVertIndexes.Count() > 1)
            {
                var nextFaces = firstVertexFaces.Faces.Select(f => new { Face = f, Verts = vertexFaces.Where(vf => f.VertexIndexList.Contains(vf.Vertex.Index)) });
                throw new Exception("Found more then 1 second edge vertex");
            }

            return new EdgeFace
            {
                FirstVertex = firstVertex,
                SecondVertex = nextVertIndexes.First().Vertex,
                Face = nextFace
            };
        }

        private IEnumerable<Loops> GroupLoopsByBoundaries(string axis, List<LoopEdges> allLoopsEdges)
        {
            var loopsSizes = allLoopsEdges.Select(lef => new
            {
                Loop = lef,
                Size = Obj.GetObjSize(lef.Edges.SelectMany(ef => ef.Edge).ToList())
            });

            var loops = new List<Loops>();

            var groupedLoops = loopsSizes.Select((secondLoop, i) =>
            {
                var children = loopsSizes.Where(firstLoop => IsFistLoopInsideSecondLoop(axis, firstLoop.Size, secondLoop.Size)).ToList();
                return new Loops
                {
                    Id = i,
                    Main = new LoopEdges { Edges = secondLoop.Loop.Edges },
                    Children = children.Select(x => new LoopEdges { Edges = x.Loop.Edges }).ToList()
                };
            }).ToList();

            var loopsWithChildren = groupedLoops.Where(gl => gl.Children.Any()).ToList();
            var singleLoops = groupedLoops.Except(loopsWithChildren);
            var allChildrensEdges = loopsWithChildren.SelectMany(l => l.Children.SelectMany(le => le.Edges).ToList()).ToList();
            var singleLoopsNotChildOfOthers = singleLoops.Where(l => allChildrensEdges.Intersect(l.Main.Edges).Count() != l.Main.Edges.Count()).ToList();

            return loopsWithChildren.Concat(singleLoopsNotChildOfOthers);
        }

        private bool IsFistLoopInsideSecondLoop(string axis, Extent firstLoopSize, Extent secondLoopSize)
        {
            var firstLoopBoundary = AxisSelectHelpers.GetXYBoundaries(axis, firstLoopSize);
            var secondLoopBoundary = AxisSelectHelpers.GetXYBoundaries(axis, secondLoopSize);

            return firstLoopBoundary.minPoint.X > secondLoopBoundary.minPoint.X && firstLoopBoundary.minPoint.Y > secondLoopBoundary.minPoint.Y &&
            firstLoopBoundary.maxPoint.X < secondLoopBoundary.maxPoint.X && firstLoopBoundary.maxPoint.Y < secondLoopBoundary.maxPoint.Y;
        }

        private IEnumerable<PointF> GetLoopPoints(string axis, IEnumerable<EdgeFace> loopEdgeFaces)
        {
            var mmGain = 1000;
            var points = loopEdgeFaces.Select(ef => AxisSelectHelpers.GetPointByAxis(axis, ef.SecondVertex, mmGain)).ToList();
            var firstVertex = loopEdgeFaces.First().FirstVertex;
            return points.Prepend(AxisSelectHelpers.GetPointByAxis(axis, firstVertex, mmGain));
        }
    }

    public class VertexFaces
    {
        public Vertex Vertex { get; set; }
        public IEnumerable<Face> Faces { get; set; }
    }

    public class EdgeFace
    {
        public Vertex FirstVertex { get; set; }
        public Vertex SecondVertex { get; set; }
        public Face Face { get; set; }

        public Vertex[] Edge => new[] { FirstVertex, SecondVertex };
    }
    public class Loops
    {
        public int Id { get; set; }
        public LoopEdges Main { get; set; }
        public IEnumerable<LoopEdges> Children { get; set; }
    }

    public class LoopEdges
    {
        public IEnumerable<EdgeFace> Edges { get; set; }
    }

    public class LoopPoints
    {
        public IEnumerable<PointF> Points { get; set; }
    }


    public class ObjectLoops
    {
        public IEnumerable<LoopPoints> Loops { get; set; }
    }

    public class MeshObjectsLoops
    {
        public IEnumerable<ObjectLoops> Objects { get; set; }
    }
}

