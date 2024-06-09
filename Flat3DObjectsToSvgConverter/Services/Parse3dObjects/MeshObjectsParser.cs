using ObjParser.Types;
using ObjParserExecutor.Helpers;
using ObjParserExecutor.Models;
using System.Diagnostics;
using GeometRi;
using ObjParser;
using Flat3DObjectsToSvgConverter.Helpers;
using System.Drawing;
using System.Linq;
using Flat3DObjectsToSvgConverter.Models.MeshObjectsParser;

namespace Flat3DObjectsToSvgConverter.Services.Parse3dObjects
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

            if (mesh.Name == "94_stoper")
            {
                var a = 0;
            }

            meshObjects.ForEach(mo => MakeObjectOrthogonalWithAxises(mo));

            var meshObjectsLoopsFaces = meshObjects.Select((mo, i) => GetObjectLoopFaces(mo, i, mesh)).ToList();

            watch.Stop();
            Console.WriteLine($"    Finished mesh parse, found {meshObjects.Count} objects, took - {watch.ElapsedMilliseconds / 1000.0} sec");

            return meshObjectsLoopsFaces;
        }

        private MeshObject GetObjectLoopFaces(MeshObject obj, int i, Mesh mesh)
        {
            var xOrientedPlanes = GroupVertsByCoordinateWithDelta(obj.Verts, "X");
            var yOrientedPlanes = GroupVertsByCoordinateWithDelta(obj.Verts, "Y");
            var zOrientedPlanes = GroupVertsByCoordinateWithDelta(obj.Verts, "Z");

            var axisesPlanes = new[] {
                new { Planes= xOrientedPlanes, VertexCount = xOrientedPlanes.Select(g => g.Value.Count()).Max(), Axis = "x", PlanesCount = xOrientedPlanes.Keys.Count() },
                new { Planes= yOrientedPlanes, VertexCount = yOrientedPlanes.Select(g => g.Value.Count()).Max(), Axis = "y", PlanesCount = yOrientedPlanes.Keys.Count() },
                new { Planes= zOrientedPlanes, VertexCount = zOrientedPlanes.Select(g => g.Value.Count()).Max(), Axis = "z", PlanesCount = zOrientedPlanes.Keys.Count() },
            };

            var axisesBoxPlanes = axisesPlanes.Where(p => p.PlanesCount % 2 == 0).ToList();
            if (!axisesBoxPlanes.Any())
            {
                throw new Exception("Object box contains more then two paralel planes in one axis, can not distinguish box orientation");
            }

            var maxAxisesVertsCount = axisesBoxPlanes.Select(ap => ap.VertexCount).Max();
            var targetAxisVerts = axisesPlanes.First(p => p.VertexCount == maxAxisesVertsCount);

            var targetAxis = targetAxisVerts.Axis;
            var orderedPlanes = targetAxisVerts.Planes.OrderBy(g => g.Key).ToList(); // parallel vert planes should be in pairs between plains in pair should be 4 mms

            if (orderedPlanes.Count > 2)
            {
                throw new Exception("Found more then 2 parallel planes for an object, pls remove line(s) or dot(s) in loop between parallel planes");
            }

            //if (mesh.Name.ToLower() == "172")
            //{
            //    var c = 0;
            //}

            Console.WriteLine($"        Finished parse object - {i + 1}");

            var vertsOfFirstPlane = orderedPlanes.First().Value;

            return new MeshObject
            {
                Name = mesh.Name,
                Verts = vertsOfFirstPlane,
                Faces = obj.Faces,
                Axis = targetAxis,
            };
        }

        private void MakeObjectOrthogonalWithAxises(MeshObject meshObject, int roundAnglePrecision = 2)
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
                var angleX = normal.AngleToDeg(new Line3d(points[0], _axisXVector));
                var angleY = normal.AngleToDeg(new Line3d(points[0], _axisYVector));
                var angleZ = normal.AngleToDeg(new Line3d(points[0], _axisZVector));
                var roudedAngles = new Point3d(Math.Round(angleX, roundAnglePrecision), Math.Round(angleY, roundAnglePrecision), Math.Round(angleZ, roundAnglePrecision));
                return new RotatedFace
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

            var groupedByAngles = GroupFacesByNormalAngles(facesVerts);

            //var groupedByAngles = facesVerts.GroupBy(fv => $"{fv.RoundedAngles.X}-{fv.RoundedAngles.Y}-{fv.RoundedAngles.Z}");
            var rotatedFacesWithMaxCount = groupedByAngles.Select(g => new { AnglesGroup = g.Value, Count = g.Value.Count() }).MaxBy(g => g.Count);
            var objectRotationAngles = rotatedFacesWithMaxCount.AnglesGroup.First().RoundedAngles;
            var paralelFaces = rotatedFacesWithMaxCount.AnglesGroup.Select(g => g.Face).ToList();

            var axisAngles = new[]
            {
                new { Axis ="X", Angle = objectRotationAngles.X },
                new { Axis ="Y", Angle = objectRotationAngles.Y },
                new { Axis ="Z", Angle = objectRotationAngles.Z },
            };

            if (!IsNormalOrthogonalToAxises(objectRotationAngles))
            {
                var orthogonalAxisAngle = axisAngles.FirstOrDefault(na => na.Angle % 90 == 0);

                if (orthogonalAxisAngle != null)
                {
                    var facesGroupedByNormals = rotatedFacesWithMaxCount.AnglesGroup
                        .GroupBy(g => AxisSelectHelpers.GetIdByAxis(orthogonalAxisAngle.Axis, g.NormalDirection))
                        .Select(g => new { Group = g, Count = g.Count() }).ToList();

                    var oneDirectionOrientedFacesMaxCount = facesGroupedByNormals.Max(g => g.Count);

                    var facesToRotate = facesGroupedByNormals.Where(g => g.Count == oneDirectionOrientedFacesMaxCount)
                        .SelectMany(g => g.Group.ToList()).ToList();
                    paralelFaces = facesToRotate.Select(g => g.Face).ToList();

                    var rotationPointFace = facesToRotate.First();
                    var rotationVert = rotationPointFace.Verts.First();

                    var normalAngles = GetPreciseNormalAngles(facesToRotate.Select(f => f.Angles).ToArray());
                    var normalAxises = GetNormaAxises(normalAngles, rotationPointFace.NormalDirection);

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
                else
                {
                    var closestAxisToBeOrthogonal = new { Axis = "Y" }; // axisAngles.Where(a => a.Axis != "Z").MaxBy(aa => aa.Angle); // normal angle is closest to 90 degrees
                    var firstRotationAxisName = "X"; // GetRotationAxisToMakeClosestAxisOrthogonal(closestAxisToBeOrthogonal.Axis);
                    //var firstRotationAxis = axisAngles.First(aa => aa.Axis == firstRotationAxisName);

                    var facesToRotate = rotatedFacesWithMaxCount.AnglesGroup;
                    var rotationPointFace = facesToRotate.First();
                    var rotationVert = rotationPointFace.Verts.First();

                    var normalAngles = GetPreciseNormalAngles(facesToRotate.Select(f => f.Angles).ToArray());
                    var rotationAngle = 90 - AxisSelectHelpers.GetCoordinateByAxis(closestAxisToBeOrthogonal.Axis, normalAngles);
                    normalAngles.UpdateCoordinateByAxis(closestAxisToBeOrthogonal.Axis, rotationAngle);
                    var normalAxises = GetNormaAxises(normalAngles, rotationPointFace.NormalDirection, true);

                    var rotatedVerts = RotatePoints(facesToRotate,
                        normalAxises.First(na => na.Axis == closestAxisToBeOrthogonal.Axis),
                        normalAxises.First(na => na.Axis == firstRotationAxisName),
                        rotationVert);

                    meshObject.Verts = rotatedVerts;

                    Console.WriteLine($"        Rotated verts over {firstRotationAxisName} axis by {rotationAngle} degrees");


                    MakeObjectOrthogonalWithAxises(meshObject, 0);

                    return;
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

        private bool IsNormalOrthogonalToAxises(Point3d anglesPoint)
        {
            var angles = new double[] { anglesPoint.X, anglesPoint.Y, anglesPoint.Z };
            return angles.Where(i => i == 0 || i % 90 == 0).Count() == 3;
        }

        private NormalToAxisAngle[] GetNormaAxises(Point3d roundedAngles, Point3d normalDirection, bool allAxisesPositive = false)
        {
            var normalAngles = new NormalToAxisAngle[]
            {
                new NormalToAxisAngle
                {
                    Axis = "X",
                    Angle= roundedAngles.X,
                    Vector = allAxisesPositive ? _axisXVector : new Vector3d(-1.0,0,0),
                    NormalPoint =  new Point((int)normalDirection.Z, (int)normalDirection.Y),
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
                    Vector = allAxisesPositive ? _axisYVector : new Vector3d(0,-1.0,0),
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

        private Point3d GetPreciseNormalAngles(IEnumerable<Point3d> paralelFacesAngles)
        {
            var angles = paralelFacesAngles.Select(p => new Point3d(Math.Round(p.X, 2), Math.Round(p.Y, 2), Math.Round(p.Z, 2))).ToArray();
            var xAngle = angles.GroupBy(p => Math.Abs(p.X)).Select(g => new { X = g.Key, Count = g.Count() }).MaxBy(i => i.Count).X;
            var yAngle = angles.GroupBy(p => Math.Abs(p.Y)).Select(g => new { Y = g.Key, Count = g.Count() }).MaxBy(i => i.Count).Y;
            var zAngle = angles.GroupBy(p => Math.Abs(p.Z)).Select(g => new { Z = g.Key, Count = g.Count() }).MaxBy(i => i.Count).Z;

            return new Point3d(xAngle, yAngle, zAngle);
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

        private List<KeyValuePair<string, List<RotatedFace>>> GroupFacesByNormalAngles(List<RotatedFace> rotatedFaces)
        {
            var groupedByAngles = rotatedFaces.GroupBy(fv => $"{fv.RoundedAngles.X}-{fv.RoundedAngles.Y}-{fv.RoundedAngles.Z}");

            var groupsRoundedAngles = groupedByAngles.Select(g => new { Id = g.Key, Angles = g.First().RoundedAngles }).ToList();

            var groupedAnglesIds = new List<string[]>();
            groupsRoundedAngles.ForEach(gra =>
            {
                if (!groupedAnglesIds.SelectMany(g => g).Contains(gra.Id))
                {
                    var roundedAnglesInOneDegreeRange = groupsRoundedAngles.Where(g =>
                        Math.Abs(g.Angles.X - gra.Angles.X) <= 1 && Math.Abs(g.Angles.Y - gra.Angles.Y) <= 1 && Math.Abs(g.Angles.Z - gra.Angles.Z) <= 1
                    );
                    var sameIds = roundedAnglesInOneDegreeRange.Select(g => g.Id).ToArray();
                    groupedAnglesIds.Add(sameIds);
                    //groupsRoundedAngles.RemoveAll(g => sameIds.Contains(g.Id));
                }
            });

            return groupedAnglesIds.Select(ids => new KeyValuePair<string, List<RotatedFace>>(
                ids.First(),
                groupedByAngles.Where(g => ids.Contains(g.Key)).SelectMany(g => g).ToList())
            ).ToList();

        }

        private Dictionary<double, List<Vertex>> GroupVertsByCoordinateWithDelta(IEnumerable<Vertex> verts, string axis)
        {
            var vertsCoordinates = verts.Select(v => new { Id = v.Index, Value = AxisSelectHelpers.GetVertCoordinateByAxis(axis, v).ToInt() }).ToList();
            var processedVerts = new List<int>();
            var groupedVerts = new Dictionary<double, List<Vertex>>();

            vertsCoordinates.ForEach(firstVert =>
            {
                if (!processedVerts.Contains(firstVert.Id))
                {
                    var sameVerts = vertsCoordinates.Where(v =>
                    {
                        var difference = Math.Abs(firstVert.Value) - Math.Abs(v.Value);
                        return Math.Abs(difference) < 100;
                    }).ToList();

                    var sameVertsIds = sameVerts.Select(sv => sv.Id).ToList();
                    groupedVerts.Add(firstVert.Value, verts.Where(v1 => sameVertsIds.Contains(v1.Index)).ToList());
                    processedVerts.AddRange(sameVertsIds);
                }
            });

            return groupedVerts;
        }

        private IEnumerable<Vertex> RotatePoints(List<RotatedFace> facesToRotate, NormalToAxisAngle closestAxis, NormalToAxisAngle rotationlAxis, Vertex rotationVert)
        {
            var isClosestAxisHorizontal = rotationlAxis.OrthogonalAxises.First(a => a.Axis == closestAxis.Axis).Horizontal;

            var rotationDirection = GetRotationDirection(rotationlAxis.NormalPoint, isClosestAxisHorizontal);
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
                            new Rotation(rotationlAxis.Vector, rotationAngle),
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

            return rotatedVerts;
        }

        private string GetRotationAxisToMakeClosestAxisOrthogonal(string axis)
        {
            if (axis == "X")
            {
                return "Y";
            };

            if (axis == "Y")
            {
                return "X";
            };

            return null;
        }
    }
}
