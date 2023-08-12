﻿using ObjParser.Types;
using ObjParserExecutor.Helpers;
using ObjParserExecutor.Models;
using System.Diagnostics;
using GeometRi;
using ObjParser;
using Flat3DObjectsToSvgConverter.Helpers;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class MeshObjectsParser
    {
        private Vector3d _axisXVector = new Vector3d(1.0, 0.0, 0.0);
        private Vector3d _axisYVector = new Vector3d(0.0, 1.0, 0.0);
        private Vector3d _axisZVector = new Vector3d(0.0, 0.0, 1.0);

        public IEnumerable<MeshObject> Parse(Mesh mesh)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine($"    Starting parse");

            var obj = mesh.Obj;

            var meshObjects = GetMeshObjects(obj, obj.FaceList.First().VertexIndexList.ToList(), new List<MeshObject>());

            //AllignObjectWithAxis(meshObjects[1]);

            if (mesh.Name == "172")
            {
                var a = 0;
            }

            meshObjects.ForEach(AllignObjectWithAxis);

            var meshObjectsLoopsFaces = meshObjects.Select((mo, i) => GetObjectLoopFaces(mo, i, mesh)).ToList();

            watch.Stop();
            Console.WriteLine($"    Finished mesh parse, found {meshObjects.Count} objects, took - {watch.ElapsedMilliseconds / 1000.0} sec");

            return meshObjectsLoopsFaces;
        }

        private MeshObject GetObjectLoopFaces(MeshObject obj, int i, Mesh mesh)
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

            if (mesh.Name.ToLower() == "172")
            {
                var c = 0;
            }

            Console.WriteLine($"        Finished parse object - {i + 1}");

            var firstVertOfAllEdges = obj.Verts
                .GroupBy(v => AxisSelectHelpers.GetVertCoordinateByAxis(targetAxis, v.ToIntCoords()))
                .First()
                .Select(v => v)
                .ToList();

            return new MeshObject
            {
                Verts = firstVertOfAllEdges,
                Faces = obj.Faces,
                Axis = targetAxis,
            };
        }

        private void AllignObjectWithAxis(MeshObject meshObject)
        {
            var facesVerts = meshObject.Faces.Select(f =>
            {
                var vertsDictionary = meshObject.Verts.Where(v => f.VertexIndexList.Contains(v.Index)).ToDictionary(v => v.Index, v => v);
                var verts = f.VertexIndexList.Select(v => vertsDictionary[v]).ToList();
                var points = verts.Select(v => new Point3d(v.X * VertexHelper.ScaleGain, v.Y * VertexHelper.ScaleGain, v.Z * VertexHelper.ScaleGain)).ToList();

                if (points.Count > 4)
                {
                    var last = points.Last();
                    points.Remove(last);
                    points.Insert(0, last);
                }

                var plane = new Plane3d(points[0], points[1], points[2]);
                var normal = plane.Normal;
                var angleX = normal.AngleToDeg(new Line3d());
                var angleY = normal.AngleToDeg(new Line3d(new Point3d(), _axisYVector));
                var angleZ = normal.AngleToDeg(new Line3d(new Point3d(), _axisZVector));
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
                    NormalDirection = new Point3d(
                        normal.X > 0 ? 1 : -1,
                        normal.Y > 0 ? 1 : -1,
                        normal.Z > 0 ? 1 : -1)

                };
            })
            .ToList();

            var groupedByAngles = facesVerts.GroupBy(fv => $"{fv.RoundedAngles.X}-{fv.RoundedAngles.Y}-{fv.RoundedAngles.Z}");
            var rotatedFacesWithMaxCount = groupedByAngles.Select(g => new { AnglesGroup = g, Count = g.Count() }).MaxBy(g => g.Count);
            var objectRotationAngles = rotatedFacesWithMaxCount.AnglesGroup.First().RoundedAngles;
            var paralelFaces = rotatedFacesWithMaxCount.AnglesGroup.Select(g => g.Face).ToList();

            if (!IsNormalOrthogonal(objectRotationAngles))
            {
                var orthogonalAxisAngle = new[]
                {
                    new { Axis ="X", Angle = objectRotationAngles.X },
                    new { Axis ="Y", Angle = objectRotationAngles.Y },
                    new { Axis ="Z", Angle = objectRotationAngles.Z },
                }
                .FirstOrDefault(na => na.Angle % 90 == 0);

                if (orthogonalAxisAngle != null)
                {
                    var facesGroupedByNormals = rotatedFacesWithMaxCount.AnglesGroup
                        .GroupBy(g => AxisSelectHelpers.GetIdByAxis(orthogonalAxisAngle.Axis, g.NormalDirection))
                        .Select(g => new { Group = g, Count = g.Count() }).ToList();

                    var oneDirectionOrientedFacesMaxCount = facesGroupedByNormals.Max(g => g.Count);

                    var facesToRotate = facesGroupedByNormals.Where(g => g.Count == oneDirectionOrientedFacesMaxCount)
                        .SelectMany(g => g.Group.ToList()).ToList();
                    paralelFaces = facesToRotate.Select(g => g.Face).ToList();

                    //var angles = rotatedFacesWithMaxCount.AnglesGroup.Select(g => g.Angles);

                    var rotationPointFace = facesToRotate.First();
                    var rotationVert = rotationPointFace.Verts.First();

                    var normalAxises = GetNormaAngles(rotationPointFace.RoundedAngles, rotationPointFace.NormalDirection);

                    var orthogonalAxis = normalAxises.FirstOrDefault(na => na.Axis == orthogonalAxisAngle.Axis);
                    var closestAxis = normalAxises.Where(na => na.Axis != orthogonalAxis.Axis).MinBy(na => na.Angle);
                    var isClosestAxisHorizontal = orthogonalAxis.OrthogonalAxises.First(a => a.Axis == closestAxis.Axis).Horizontal;

                    var rotationDirection = GetRotationDirection(orthogonalAxis.NormalPoint, isClosestAxisHorizontal);
                    var rotationAngle = rotationDirection * closestAxis.Angle * Math.PI / 180;
                    var rotationPoint = new Point3d(rotationVert.X, rotationVert.Y, rotationVert.Z);
                    var rotatedPoints = new Dictionary<int, Point3d> { { rotationVert.Index, rotationPoint } };

                    facesToRotate.ForEach(faceVerts =>
                    {
                        faceVerts.Verts.ToList().ForEach(vert =>
                        {
                            if (!rotatedPoints.ContainsKey(vert.Index))
                            {
                                var point = new Point3d(vert.X, vert.Y, vert.Z);
                                var newPoint = point.Rotate(
                                    new Rotation(orthogonalAxis.Vector, rotationAngle),
                                    rotationPoint);
                                rotatedPoints.Add(vert.Index, newPoint);
                            }
                        });
                    });

                    var rotatedVerts = rotatedPoints
                        .OrderBy(p => p.Key)
                        .Select(p => new Vertex
                        {
                            X = p.Value.X,
                            Y = p.Value.Y,
                            Z = p.Value.Z,
                            Index = p.Key
                        }).ToList();

                    var rotatedVertsIds = rotatedVerts.Select(v => v.Index).ToArray();
                    var leftVerts = meshObject.Verts.Where(v => !rotatedVertsIds.Contains(v.Index)).ToList();
                    if (leftVerts.Any())
                    {
                        var leftVertsIds = leftVerts.Select(v => v.Index).ToList();
                        var leftFaces = facesVerts.Where(fv => fv.Face.VertexIndexList.Intersect(leftVertsIds).Any()).ToList();
                        Console.WriteLine($"        Has some none rotated verts");
                    }

                    meshObject.Verts = rotatedVerts;
                }
            }

            var loopFaces = meshObject.Faces.Except(paralelFaces);
            var loopVertsIndexes = loopFaces.SelectMany(f => f.VertexIndexList).Distinct();
            var loopVerts = meshObject.Verts.Where(v => loopVertsIndexes.Contains(v.Index)).ToList();
            meshObject.Verts = loopVerts;
            meshObject.Faces = loopFaces;
        }

        private int GetRotationDirection(Point normalPoint, bool toHorizontalAxis)
        {
            int direction = 0;
            if (toHorizontalAxis)
            {
                direction = normalPoint.X * normalPoint.Y > 0 ? -1 : 1;
            }
            else
            {
                direction = normalPoint.X * normalPoint.Y > 0 ? 1 : -1;
            }
            return direction;
        }

        private bool IsNormalOrthogonal(Point3d anglesPoint)
        {
            var angles = new double[] { anglesPoint.X, anglesPoint.Y, anglesPoint.Z };
            return angles.Where(i => i == 0 || i % 90 > 0).Count() == 1;
        }

        private NormalToAxisAngle[] GetNormaAngles(Point3d roundedAngles, Point3d normalDirection)
        {
            var normalAngles = new NormalToAxisAngle[]
            {
                new NormalToAxisAngle
                {
                    Axis = "X",
                    Angle= roundedAngles.X,
                    Vector = new Vector3d(-1.0,0,0),
                    NormalPoint = new Point((int)normalDirection.Z, (int)normalDirection.Y),
                    OrthogonalAxises = new List<AxisOrientation>
                    {
                        new AxisOrientation
                        {
                            Axis = "Y",
                            Horizontal = false
                        },
                        new AxisOrientation
                        {
                            Axis = "Z",
                            Horizontal = true
                        },

                    }
                },
                new NormalToAxisAngle
                {
                    Axis = "Y",
                    Angle= roundedAngles.Y,
                    Vector = _axisYVector,
                    NormalPoint = new Point((int)normalDirection.X, (int)normalDirection.Z),
                    OrthogonalAxises = new List<AxisOrientation>
                    {
                        new AxisOrientation
                        {
                            Axis = "X",
                            Horizontal = true
                        },
                        new AxisOrientation
                        {
                            Axis = "Z",
                            Horizontal = false
                        },

                    }
                },
                new NormalToAxisAngle
                {
                    Axis = "Z",
                    Angle= roundedAngles.Z,
                    Vector = _axisZVector,
                    NormalPoint = new Point((int)normalDirection.X, (int)normalDirection.Y),
                    OrthogonalAxises = new List<AxisOrientation>
                    {
                        new AxisOrientation
                        {
                            Axis = "X",
                            Horizontal = true
                        },
                        new AxisOrientation
                        {
                            Axis = "Y",
                            Horizontal = false
                        },

                    }
                },
            };

            return normalAngles;
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

    public class NormalToAxisAngle
    {
        public string Axis { get; set; }
        public double Angle { get; set; }
        public Vector3d Vector { get; set; }
        public Point NormalPoint { get; set; }
        public List<AxisOrientation> OrthogonalAxises { get; set; }
    }

    public class AxisOrientation
    {
        public string Axis { get; set; }
        public bool Horizontal { get; set; }
    }
}
