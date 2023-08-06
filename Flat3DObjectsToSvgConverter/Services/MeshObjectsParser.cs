using ObjParser.Types;
using ObjParserExecutor.Helpers;
using ObjParserExecutor.Models;
using Flat3DObjectsToSvgConverter.Common.Extensions;
using System.Diagnostics;
using GeometRi;
using System.Linq;
using ObjParser;
using Flat3DObjectsToSvgConverter.Helpers;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class MeshObjectsParser
    {
        public IEnumerable<MeshObject> Parse(Mesh mesh)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine($"    Starting parse");

            var obj = mesh.Obj;

            var meshObjects = GetMeshObjects(obj, obj.FaceList.First().VertexIndexList.ToList(), new List<MeshObject>());

            //AllignObjectWithAxis(meshObjects[1]);

            meshObjects.ForEach(AllignObjectWithAxis);

            var meshObjectsLoopsFaces = meshObjects.Select(GetObjectLoopFaces).ToList();

            watch.Stop();
            Console.WriteLine($"    Finished mesh parse, found {meshObjects.Count} objects, took - {watch.ElapsedMilliseconds / 1000.0} sec");

            return meshObjectsLoopsFaces;
        }

        private MeshObject GetObjectLoopFaces(MeshObject obj, int i)
        {
            var xOrientedPlanes = obj.Verts.GroupBy(v => v.X.ToInt()).ToList();
            var yOrientedPlanes = obj.Verts.GroupBy(v => v.Y.ToInt()).ToList();
            var zOrientedPlanes = obj.Verts.GroupBy(v => v.Z.ToInt()).ToList();

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

            if (orderedMeshObjects.Count > 2)
            {
                throw new Exception("Found more then 2 parallel planes for an object");
            }


            //if (mesh.Name.ToLower() == "74_bearing")
            //{
            //    var c = 0;
            //}

            var verts = orderedMeshObjects.SelectMany(g => g.ToList()).ToList();
            var paralelVerts = verts
                .Select(v => new
                {
                    Id = AxisSelectHelpers.GetPararelVertsIdByAxis(targetAxis, v.ToIntCoords()),
                    Vertex = v
                })
                .GroupBy(v => v.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            // can contain edges that match totally by vertexes, so in pair group can be more then 2 verts
            var edgesVerts = paralelVerts.Select(g => g.Select(v => v.Vertex)).ToList();
            var edgesVertsIndexes = edgesVerts.Select(ev => ev.Select(v => v.Index)).ToList();

            var leftVerts = verts.Except(edgesVerts.SelectMany(ev => ev).ToList());
            if (leftVerts.Count() > 2)
            {
                Console.WriteLine($"        Has some none parallel verts");
            }


            // get faces that perpendicular to parralel box planes with most verts in one axis
            var targetFaces = obj.Faces
                    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(evi).Count() == 2))
                    .DistinctBy(f => f.Id).ToList();

            //if (mesh.Name.ToLower() == "74_bearing" && targetFaces.Any(f => f.Id == 264))
            //{
            //    var c = 0;
            //}

            //var b = obj.FaceList
            //    .Where(f => edgesVertsIndexes.Any(evi => f.VertexIndexList.Intersect(new[] { 109 }).Any()));

            Console.WriteLine($"        Finished parse object - {i + 1}");

            var firstVertOfAllEdges = edgesVerts.SelectMany(ev => ev.Select(v => v))
                .GroupBy(v => AxisSelectHelpers.GetVertCoordinateByAxis(targetAxis, v.ToIntCoords()))
                .First()
                .Select(v => v)
                .ToList();

            return new MeshObject
            {
                Verts = firstVertOfAllEdges,
                Faces = targetFaces,
                Axis = targetAxis,
            };
        }

        private void AllignObjectWithAxis(MeshObject meshObject)
        {
            var axisXVector = new Vector3d(1.0, 0.0, 0.0);
            var axisYVector = new Vector3d(0.0, 1.0, 0.0);
            var axisZVector = new Vector3d(0.0, 0.0, 1.0);

            var facesVerts = meshObject.Faces.Select(f =>
            {
                var verts = meshObject.Verts.Where(v => f.VertexIndexList.Contains(v.Index)).ToList();
                var gain = 100000;
                var points = verts.Select(v => new Point3d(v.X * gain, v.Y * gain, v.Z * gain)).ToList();
                var plane = new Plane3d(points[0], points[1], points[2]);
                var normal = plane.Normal;
                var angleX = normal.AngleToDeg(new Line3d());
                var angleY = normal.AngleToDeg(new Line3d(new Point3d(), axisYVector));
                var angleZ = normal.AngleToDeg(new Line3d(new Point3d(), axisZVector));
                var roudedAngles = new Point3d(Math.Round(angleX), Math.Round(angleY), Math.Round(angleZ));
                return new
                {
                    Face = f,
                    Verts = verts,
                    Plane = plane,
                    RoundedAngles = roudedAngles,
                    Angles = new Point3d(
                        normal.X > 0 ? -angleX : angleX,
                        normal.Y > 0 ? -angleY : angleY,
                        normal.Z > 0 ? -angleZ : angleZ
                        ),
                    RotationDirection = new Point3d(
                        normal.X > 0 ? -1 : 1,
                        normal.Y > 0 ? -1 : 1,
                        normal.Z > 0 ? -1 : 1)
                };
            }).ToList();

            var groupedByAngles = facesVerts.GroupBy(fv => $"{fv.RoundedAngles.X}-{fv.RoundedAngles.Y}-{fv.RoundedAngles.Z}");

            var rotatedFacesWithMaxCount = groupedByAngles.Select(g => new { AnglesGroup = g, Count = g.Count() }).MaxBy(g => g.Count);

            if (!IsNormalOrthogonal(rotatedFacesWithMaxCount.AnglesGroup.First().RoundedAngles))
            {
                var rotationVert = rotatedFacesWithMaxCount.AnglesGroup.First().Verts.First();
                var angles = rotatedFacesWithMaxCount.AnglesGroup.Select(g => g.Angles);

                var rotationPontFace = rotatedFacesWithMaxCount.AnglesGroup.First();
                var roundedAngles = rotationPontFace.RoundedAngles;
                var rotationDirection = rotationPontFace.RotationDirection;
                var normalAngles = new AxisAngle[]
                {
                    new AxisAngle{ Axis = "X", Angle= roundedAngles.X, Vector = axisXVector, RotationDirection = (int)rotationDirection.X },
                    new AxisAngle{ Axis = "Y", Angle= roundedAngles.Y, Vector = axisYVector, RotationDirection = (int)rotationDirection.Y },
                    new AxisAngle{ Axis = "Z", Angle= roundedAngles.Z, Vector = axisZVector, RotationDirection = (int)rotationDirection.Z },
                };

                var orthogonalAxe = normalAngles.FirstOrDefault(na => na.Angle % 90 == 0);
                if (orthogonalAxe != null)
                {
                    var rotationAxe = normalAngles.Where(na => na.Axis != orthogonalAxe.Axis).MinBy(na => na.Angle);

                    var rotationPoint = new Point3d(rotationVert.X, rotationVert.Y, rotationVert.Z);

                    //var vert = rotatedFacesWithMaxCount.AnglesGroup.ToArray()[3].Verts.First();

                    //var point = new Point3d(vert.X, vert.Y, vert.Z);

                    //var newPoint = point.Rotate(new Rotation(rotationAxe.Vector, rotationAxe.Angle), rotationPoint);

                    var rotatedPoints = new Dictionary<int, Point3d> { { rotationVert.Index, rotationPoint } };
                    rotatedFacesWithMaxCount.AnglesGroup.ToList().ForEach(faceVerts =>
                    {
                        faceVerts.Verts.ToList().ForEach(vert =>
                        {
                            if (!rotatedPoints.ContainsKey(vert.Index))
                            {
                                var point = new Point3d(vert.X, vert.Y, vert.Z);
                                var newPoint = point.Rotate(
                                    new Rotation(orthogonalAxe.Vector, rotationAxe.RotationDirection * rotationAxe.Angle * Math.PI / 180),
                                    rotationPoint);
                                rotatedPoints.Add(vert.Index, newPoint);
                            }
                        });
                    });

                    meshObject.Verts = rotatedPoints
                        .OrderBy(p => p.Key)
                        .Select(p => new Vertex
                        {
                            X = p.Value.X,
                            Y = p.Value.Y,
                            Z = p.Value.Z,
                            Index = p.Key
                        }).ToList();
                }
            }
        }

        private bool IsOrthogonal(NormalVertex nv)
        {
            var ortogonal = (nv.X == 0 || Math.Abs(nv.X) == 1) &&
                (nv.Y == 0 || Math.Abs(nv.Y) == 1) &&
                (nv.Z == 0 || Math.Abs(nv.Z) == 1);

            if (ortogonal)
            {
                ortogonal = (Math.Abs(nv.X) + Math.Abs(nv.Y) + Math.Abs(nv.Z)) == 1;
            }

            return ortogonal;
        }

        private bool IsNormalOrthogonal(Point3d anglesPoint)
        {
            var angles = new double[] { anglesPoint.X, anglesPoint.Y, anglesPoint.Z };
            return angles.Where(i => i == 0 || i % 90 > 0).Count() == 1;
        }

        private List<MeshObject> GetMeshObjects(Obj obj, List<int> firstFaceVetexIds, List<MeshObject> meshObjects)
        {
            var (objectFacesIds, objectVertsIds) = GetObjectFaces(obj,
                firstFaceVetexIds,
                new List<int>());

            var meshObject = new MeshObject
            {
                Verts = obj.VertexList.Where(v => objectVertsIds.Contains(v.Index)).ToArray(),
                Faces = obj.FaceList.Where(f => objectFacesIds.Contains(f.Id)).ToArray(),
            };

            meshObjects.Add(meshObject);

            var otherFaces = obj.FaceList.Except(meshObjects.SelectMany(mo => mo.Faces).ToList());

            if (otherFaces.Any())
            {
                GetMeshObjects(obj, otherFaces.First().VertexIndexList.ToList(), meshObjects);
            }

            return meshObjects;
        }

        private (List<int> faceIds, List<int> vertIds) GetObjectFaces(Obj obj, List<int> vertIds, List<int> faceIds)
        {
            var neighbourFaces = obj.FaceList
                .Where(f => !faceIds.Contains(f.Id) && f.VertexIndexList.Intersect(vertIds).Any())
                .ToList();

            if (neighbourFaces.Any())
            {
                var neighbourFacesVertIds = neighbourFaces.SelectMany(f => f.VertexIndexList).Distinct().ToList();
                var newVerts = neighbourFacesVertIds.Except(vertIds);
                vertIds.AddRange(newVerts);
                faceIds.AddRange(neighbourFaces.Select(f => f.Id).ToList());
                GetObjectFaces(obj, vertIds, faceIds);
            }

            return (faceIds, vertIds);
        }
    }

    public class MeshObject
    {
        public IEnumerable<Vertex> Verts { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public string Axis { get; set; }
    }

    public class AxisAngle
    {
        public string Axis { get; set; }
        public double Angle { get; set; }
        public Vector3d Vector { get; set; }
        public int RotationDirection { get; set; }
    }
}
