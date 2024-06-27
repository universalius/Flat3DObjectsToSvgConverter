using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using SvgNest.Utils;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services
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

                    var points = mainLoop.Points.Select(p => p.ToPoint3d()).ToArray();
                    //var pointsCount = points.Count();
                    //var segments = points.Select((p, j) =>
                    //{
                    //    var nextPointIndex = j + 1;
                    //    return nextPointIndex != pointsCount ?
                    //        new Segment3d(p, points[nextPointIndex]) : null;
                    //}).Where(l => l != null).ToList();

                    var bounds = GeometryUtil.GetPolygonBounds(points.Select(p => p.ToDoublePoint()).ToArray());
                    var center = new Point3d(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, 0);

                    var radiuses = points.Select((p, i) => new { Point = p, Radius = center.DistanceTo(p), Id = i }).ToArray();
                    var maxRadius = radiuses.Max(p => p.Radius);

                    var topToothPoints = radiuses.Where(p => p.Radius >= maxRadius - 0.2 && p.Radius <= maxRadius).OrderBy(p => p.Id).ToArray();

                    if (topToothPoints.Count() > 6)
                    {
                        var toothPointPairs = new List<(Point3d Point, int Id)[]>();

                        for (int i = 0; i < topToothPoints.Length; i++)
                        {
                            var firstPoint = topToothPoints[i];

                            if (i == topToothPoints.Length - 1)
                            {
                                var lastPair = toothPointPairs.Last();
                                if (lastPair[1].Id != firstPoint.Id)
                                {
                                    toothPointPairs.Add(new[] { (firstPoint.Point, firstPoint.Id) });
                                    break;
                                }
                            }
                                
                            var secondPointIndex = i + 1;
                            var secondPoint = secondPointIndex >= topToothPoints.Length ? null : topToothPoints[secondPointIndex];

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


                        //if (!topToothPoints.Any(ttp =>
                        //{





                        //    if (ttp.Id == topToothPoints.Length - 1)
                        //    {
                        //        return false;
                        //    }

                        //    var secondPointIndex = Array.IndexOf(topToothPoints, ttp) + 1;
                        //    var secondPoint = secondPointIndex >= topToothPoints.Length ? null : topToothPoints[secondPointIndex];
                        //    var thirdPointIndex = secondPointIndex + 1;
                        //    var thirdPoint = thirdPointIndex >= topToothPoints.Length ? null : topToothPoints[thirdPointIndex];

                        //    if (secondPoint == null ? false : (ttp.Id + 1) == secondPoint.Id &&
                        //        (thirdPoint == null ? false : (secondPoint.Id + 1) == thirdPoint.Id))
                        //    {
                        //        return true;
                        //    }

                        //    return false;
                        //})



                        //)
                        {
                            var loopsPoints = toothPointPairs.Select((tpp, j) =>
                            {
                                if (j == toothPointPairs.Count - 1)
                                    return null;

                                var nextToothIndex = j + 1;
                                var nextToothPair = toothPointPairs[nextToothIndex];
                                return new LoopPoints
                                {
                                    Points = new List<PointF> {
                                        tpp.Length == 1 ? tpp[0].Point.ToPointF() : tpp[1].Point.ToPointF(),
                                        nextToothPair[0].Point.ToPointF()
                                    }
                                };
                            }).Where(l => l != null).ToList();

                            var allLoops = new List<LoopPoints>();
                            allLoops.AddRange(obj.Loops);
                            allLoops.AddRange(loopsPoints);
                            obj.Loops = allLoops;
                        }
                    }
                });
            });

            Console.WriteLine();
        }
    }
}
