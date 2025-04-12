using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots
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

                            var segmentsToResize = lines.Where(l => InRange(l.Length, oldSlotWidth, 0.1)).ToArray();

                            if (!segmentsToResize.Any()) return;

                            var isLoopSquare = segmentsToResize.Length == lines.Count;
                            var segmentToResize = segmentsToResize.First();

                            var segmentToResizeIndex = lines.IndexOf(segmentToResize);
                            var prevSegment = segmentToResizeIndex == 0 ? lines.Last() : lines[segmentToResizeIndex - 1];
                            var nextSegment = segmentToResizeIndex == lines.Count - 1 ? lines.First() : lines[segmentToResizeIndex + 1];
                            Segment3d lastSegment = null;

                            if (lines.Count == 4)
                            {
                                var lastSegmentIndex = segmentToResizeIndex + 2;
                                lastSegment = lastSegmentIndex >= lines.Count ? lines.First() : lines[lastSegmentIndex];
                            }

                            var scaleToResizeSegment = newSlotWidth / oldSlotWidth;
                            var shiftValue = (oldSlotWidth - newSlotWidth) / 2;

                            var resizedSegment = segmentToResize.Scale(scaleToResizeSegment);
                            var shiftSideVector = segmentToResize.ToVector.Normalized.Mult(shiftValue);

                            if (isLoopSquare)
                            {
                                var prevResizedSegment = prevSegment.Scale(scaleToResizeSegment);
                                var nextResizedSegment = nextSegment.Scale(scaleToResizeSegment);
                                var lastResizedSegment = lastSegment.Scale(scaleToResizeSegment);

                                var shiftOrtogonalSideVector = prevSegment.ToVector.Normalized.Mult(shiftValue);

                                lines[lines.IndexOf(prevSegment)] = prevResizedSegment.Translate(shiftSideVector);
                                lines[lines.IndexOf(segmentToResize)] = resizedSegment.Translate(shiftOrtogonalSideVector.Mult(-1));
                                lines[lines.IndexOf(nextSegment)] = nextResizedSegment.Translate(shiftSideVector.Mult(-1));
                                lines[lines.IndexOf(lastSegment)] = lastResizedSegment.Translate(shiftOrtogonalSideVector);
                            }
                            else
                            {
                                lines[lines.IndexOf(prevSegment)] = prevSegment.Translate(shiftSideVector);
                                lines[lines.IndexOf(segmentToResize)] = resizedSegment;
                                lines[lines.IndexOf(nextSegment)] = nextSegment.Translate(shiftSideVector.Mult(-1));

                                if (lastSegment != null)
                                {
                                    lines[lines.IndexOf(lastSegment)] = lastSegment.Scale(scaleToResizeSegment);
                                }
                            }

                            Console.WriteLine($"   Starting reduce slot for {mesh.MeshName} and hole {rectangular.Id}");

                            if (lines.Count == 4)
                            {
                                var loop = holeLoops[rectangular.Id];
                                var newPoints = new List<Point3d>
                                {
                                    lines[0].P1
                                };

                                newPoints.AddRange(lines.Select((s, j) => s.P2));

                                loop.Points = newPoints;
                                loop.IsResized = true;
                            }
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
            return value >= comparedValue - tolerance && value <= comparedValue + tolerance;
        }

        //private static Segment3d ScaleAndShiftSegment(this Segment3d segmentToResize, double scaleToResizeSegment, double shift)
        //{
        //    var resizedSegment = segmentToResize.Scale(scaleToResizeSegment);
        //    var shiftVector = segmentToResize.ToVector.Normalized.Mult(shift);
        //    return prevSegment.Translate(shiftVector)
        //}
    }
}
