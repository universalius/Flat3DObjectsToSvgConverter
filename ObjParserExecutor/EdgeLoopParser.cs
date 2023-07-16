using ObjParser.Types;
using ObjParserExecutor.Helpers;
using System.Drawing;

namespace ObjParserExecutor
{
    public class EdgeLoopParser
    {
        public IEnumerable<IEnumerable<PointF>> GetEdgeLoopPoints(MeshObject meshObject)
        {
            var verts = meshObject.PlainVerts;
            var faces = meshObject.Faces;
            var axis = meshObject.Axis;

            var vertIndexes = verts.Select(v => v.Index);
            var vertsDictionary = verts.ToDictionary(v => v.Index);

            var vertexFaces = verts.Select(v =>
                new VertexFaces
                {
                    Vertex = v,
                    Faces = faces.Where(f => f.VertexIndexList.Contains(v.Index))
                });

            var initialVertex = verts.First();
            var mainLoopEdgeFaces = GetEdgeLoop(initialVertex, vertexFaces);

            var allLoopsEdgeFaces = GetHolesEdgeLoops(faces, vertexFaces, new List<IEnumerable<EdgeFace>> { mainLoopEdgeFaces });

            var mmGain = 1000;
            var loopsPoints = allLoopsEdgeFaces.Select(l =>
            {
                var points = l.Select(ef => AxisSelectHelpers.GetPointByAxis(axis, ef.SecondVertex, mmGain));
                var firstVertex = l.First().FirstVertex;
                return points.Prepend(AxisSelectHelpers.GetPointByAxis(axis, firstVertex, mmGain));
            });

            return loopsPoints;
        }

        public List<IEnumerable<EdgeFace>> GetHolesEdgeLoops(IEnumerable<Face> faces, IEnumerable<VertexFaces> vertexFaces,
            List<IEnumerable<EdgeFace>> loopsEdgeFaces)
        {
            var vertIndexes = vertexFaces.Select(vf => vf.Vertex.Index);

            var holesFaces = faces.Except(loopsEdgeFaces.SelectMany(l => l.Select(ef => ef.Face)));

            if (!holesFaces.Any())
            {
                return loopsEdgeFaces;
            }

            var holeFirstVertIndex = holesFaces.First().VertexIndexList.Intersect(vertIndexes).First();
            var holeVertexFaces = vertexFaces.First(vf => vf.Vertex.Index == holeFirstVertIndex);
            var holeLoopEdgeFaces = GetEdgeLoop(holeVertexFaces.Vertex, vertexFaces);

            loopsEdgeFaces.Add(holeLoopEdgeFaces);

            return GetHolesEdgeLoops(faces, vertexFaces, loopsEdgeFaces);
        }

        public IEnumerable<EdgeFace> GetEdgeLoop(Vertex initialVertex, IEnumerable<VertexFaces> vertexFaces)
        {
            var firstVertex = initialVertex;
            EdgeFace edgeFace = null;
            var loopEdgeFaces = new List<EdgeFace>();

            while ((edgeFace?.SecondVertex?.Index ?? 0) != initialVertex.Index)
            {
                edgeFace = GetEdgeFace(firstVertex, vertexFaces, edgeFace?.FirstVertex?.Index);
                loopEdgeFaces.Add(edgeFace);
                firstVertex = edgeFace.SecondVertex;
            }

            return loopEdgeFaces;
        }

        public EdgeFace GetEdgeFace(Vertex firstVertex, IEnumerable<VertexFaces> vertexFaces, int? prevVertIndex)
        {
            var firstVertexFaces = vertexFaces.First(vf => vf.Vertex.Index == firstVertex.Index);
            var nextFace = prevVertIndex == null ?
                firstVertexFaces.Faces.First() :
                firstVertexFaces.Faces.First(f => !f.VertexIndexList.Contains(prevVertIndex.Value));

            var nextFaceVertIndexes = nextFace.VertexIndexList.Except(new[] { firstVertex.Index });
            var nextVertIndexes = vertexFaces.Where(vf => nextFaceVertIndexes.Contains(vf.Vertex.Index));

            if (nextVertIndexes.Count() > 1)
            {
                throw new Exception("Found more then 1 second edge vertex");
            }

            return new EdgeFace
            {
                FirstVertex = firstVertex,
                SecondVertex = nextVertIndexes.First().Vertex,
                Face = nextFace
            };
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
    }
}
