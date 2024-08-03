using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops
{
    public class ObjectLoopsSlotSizeReducer
    {
        public ObjectLoopsSlotSizeReducer() { }

        public void ChangeSlotsSize(IEnumerable<MeshObjects> meshes)
        {
            var oldSlotWidth = 4.0;
            var newSlotWidth = 3.8;
            meshes.ToList().ForEach(mesh =>
            {
                mesh.Objects.ToList().ForEach(obj =>
                {
                    var holeLoops = obj.Loops.Skip(1).ToArray();
                    if (holeLoops == null || !holeLoops.Any())
                    {
                        return;
                    }

                    var rectangulars = holeLoops.Select((hl, i) =>
                    {
                        var points = hl.Points.ToArray();
                        var pointsCount = points.Count();
                        var lines = points.Select((p, j) =>
                        {
                            var nextPointIndex = j + 1;
                            return nextPointIndex != pointsCount ?
                                new Segment3d(new Point3d(p.X, p.Y, 0), new Point3d(points[nextPointIndex].X, points[nextPointIndex].Y, 0)) :
                                null;
                        }).Where(l => l != null).ToList();

                        var nextLineIndex = 1;
                        var linesCount = lines.Count;
                        foreach (var line in lines)
                        {
                            if (nextLineIndex == linesCount)
                            {
                                break;
                            }

                            var angle = line.AngleToDeg(lines[nextLineIndex]); //line.AngleTo(lines[nextLineIndex]) * 180 / Math.PI;
                            if (IsOrtogonal(angle))
                            {
                                nextLineIndex++;
                                continue;
                            }

                            break;
                        }

                        return nextLineIndex == linesCount ? new { Id = i, Lines = lines } : null;
                    }).Where(hl => hl != null).ToList();

                    if (rectangulars.Any())
                    {
                        rectangulars.ForEach(rectangular =>
                        {
                            var lines = rectangular.Lines;

                            if (lines.Count < 4) return;

                            var segmentToResize = lines.FirstOrDefault(l => InRange(l.Length, oldSlotWidth, 0.1));

                            if (segmentToResize == null) return;

                            var segmentToResizeIndex = lines.IndexOf(segmentToResize);
                            var prevSegment = segmentToResizeIndex == 0 ? lines.Last() : lines[segmentToResizeIndex - 1];
                            var nextSegment = segmentToResizeIndex == lines.Count - 1 ? lines.First() : lines[segmentToResizeIndex + 1];

                            var scaleToResizeSegment = newSlotWidth / oldSlotWidth;
                            var resizedSegment = segmentToResize.Scale(scaleToResizeSegment);
                            var shiftVector = segmentToResize.ToVector.Normalized.Mult((oldSlotWidth - newSlotWidth) / 2);

                            lines[lines.IndexOf(prevSegment)] = prevSegment.Translate(shiftVector);
                            lines[lines.IndexOf(segmentToResize)] = resizedSegment;
                            lines[lines.IndexOf(nextSegment)] = nextSegment.Translate(shiftVector.Mult(-1));


                            if (lines.Count == 4)
                            {
                                var lastSegmentIndex = segmentToResizeIndex + 2;
                                var lastSegment = lastSegmentIndex >= lines.Count ? lines.First() : lines[lastSegmentIndex];
                                lines[lines.IndexOf(lastSegment)] = lastSegment.Scale(scaleToResizeSegment);
                            }

                            Console.WriteLine($"   Starting reduce slot for {mesh.MeshName} and hole {rectangular.Id}");

                            var loop = holeLoops[rectangular.Id];
                            var newPoints = new List<PointF>
                            {
                                lines[0].P1.ToPointF()
                            };

                            newPoints.AddRange(lines.Select((s, j) => s.P2.ToPointF()));

                            loop.Points = newPoints;
                        });
                    }
                });
            });

            Console.WriteLine();
        }

        private bool IsOrtogonal(double angle)
        {
            return angle > 89 && angle <= 90 || angle >= 0 && angle <= 1;
        }

        private bool InRange(double value, double comparedValue, double tolerance)
        {
            return value >= (comparedValue - tolerance) && value <= (comparedValue + tolerance);
        }
    }
}
