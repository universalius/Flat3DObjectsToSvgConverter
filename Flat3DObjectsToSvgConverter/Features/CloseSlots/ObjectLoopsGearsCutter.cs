﻿using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using SvgNest.Utils;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots
{
    public class ObjectLoopsGearsCutter
    {
        public ObjectLoopsGearsCutter() { }

        public void CutTeeth(IEnumerable<MeshObjects> meshes)
        {
            meshes.ToList().ForEach(mesh =>
            {

                mesh.Objects.ToList().ForEach(obj =>
                {
                    var mainLoop = obj.Loops.First();
                    var points = mainLoop.Points.ToArray();
                    var bounds = GeometryUtil.GetPolygonBounds(points.Select(p => p.ToDoublePoint()).ToArray());
                    var center = new Point3d(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, 0);
                    var radius = 0.0;

                    if (Math.Abs(bounds.Width - bounds.Height) > 0.01)
                    {
                        var tolerance = 0.01;
                        var point1 = points.First(p => Math.Abs(p.Y - bounds.Y) <= tolerance);
                        var point2 = points.First(p => Math.Abs(p.X - bounds.X) <= tolerance);
                        var point3 = points.First(p => Math.Abs(p.X - (bounds.X + bounds.Width)) <= tolerance);

                        Circle3d circle;
                        try
                        {
                            circle = new Circle3d(point1, point2, point3);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    Failed to cut teeth for mesh {mesh.MeshName}, circle3d throws exception");
                            return;
                        }

                        center = circle.Center;
                        radius = circle.R;
                    }
                    else
                    {
                        radius = bounds.Width / 2;
                    }

                    var radiuses = points.Select((p, i) => new { Point = p, Radius = center.DistanceTo(p), Id = i }).ToArray();
                    var toothTopPoints = radiuses.Where(p => p.Radius >= radius - 0.1 && p.Radius <= radius + 0.1).OrderBy(p => p.Id).ToArray();

                    if (toothTopPoints.Count() > 6)
                    {
                        var toothPointPairs = new List<(Point3d Point, int Id)[]>();

                        for (int i = 0; i < toothTopPoints.Length; i++)
                        {
                            var firstPoint = toothTopPoints[i];

                            if (i == toothTopPoints.Length - 1)
                            {
                                var lastPair = toothPointPairs.Last();
                                if ((lastPair.Length == 1 ? lastPair[0] : lastPair[1]).Id != firstPoint.Id)
                                {
                                    toothPointPairs.Add(new[] { (firstPoint.Point, firstPoint.Id) });
                                    break;
                                }
                            }

                            var secondPointIndex = i + 1;
                            var secondPoint = secondPointIndex >= toothTopPoints.Length ? null : toothTopPoints[secondPointIndex];

                            if (secondPoint.Id - firstPoint.Id == 1)
                            {
                                toothPointPairs.Add(new[] { (firstPoint.Point, firstPoint.Id), (secondPoint.Point, secondPoint.Id) });
                                i++;
                            }
                            else
                            {
                                toothPointPairs.Add(new[] { (firstPoint.Point, firstPoint.Id) });
                            }
                        }

                        if (!toothPointPairs.Any(tpp =>
                        {

                            var pairIndex = toothPointPairs.IndexOf(tpp);
                            var secondPairIndex = pairIndex + 1;

                            if (pairIndex == toothPointPairs.Count - 1)
                                return false;

                            return (tpp.Length == 1 ? tpp[0].Id + 1 : tpp[1].Id + 1) == toothPointPairs[secondPairIndex][0].Id;
                        }))
                        {
                            var loopsPoints = toothPointPairs.Select((tpp, j) =>
                            {
                                if (j == toothPointPairs.Count - 1)
                                    return null;

                                var nextToothIndex = j + 1;
                                var nextToothPair = toothPointPairs[nextToothIndex];
                                return new LoopPoints
                                {
                                    Points = new List<Point3d> {
                                        tpp.Length == 1 ? tpp[0].Point : tpp[1].Point,
                                        nextToothPair[0].Point
                                    }
                                };
                            }).Where(l => l != null).ToList();

                            var allLoops = new List<LoopPoints>();
                            allLoops.AddRange(obj.Loops);
                            allLoops.AddRange(loopsPoints);
                            obj.Loops = allLoops;

                            Console.WriteLine($"    Cut  {loopsPoints.Count} teeth for mesh {mesh.MeshName}");
                        }
                    }
                });
            });

            Console.WriteLine();
        }
    }
}
