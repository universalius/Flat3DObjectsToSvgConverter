using Flat3DObjectsToSvgConverter.Models;
using GeometRi;

namespace Flat3DObjectsToSvgConverter.Features.CleanLoops;

public class ObjectLoopsAlligner()
{
    public void MakeLoopsPerpendicularToAxis(IEnumerable<MeshObjects> meshes)
    {
        meshes.ToList().ForEach(mesh =>
        {
            mesh.Objects.ToList().ForEach(obj =>
            {
                var holeLoops = obj.Loops.Skip(1);
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
                            new Line3d(new Point3d(p.X, p.Y, 0), new Point3d(points[nextPointIndex].X, points[nextPointIndex].Y, 0)) :
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

                    return nextLineIndex == linesCount ? lines : null;
                }).Where(hl => hl != null).ToList();

                var axisXVector = new Vector3d(1.0, 0.0, 0.0);
                var axisZVector = new Vector3d(0.0, 0.0, 1.0);

                var holeRotationAngles = rectangulars.Select(lines =>
                {
                    var lineAngles = lines.Select(l =>
                    {
                        var angleInRadians = l.AngleTo(axisXVector);
                        var angle = angleInRadians * 180 / Math.PI;
                        return new
                        {
                            RotationAngle = l.Direction.X * l.Direction.Y > 0 ? -angleInRadians : angleInRadians,
                            Angle = angle,
                            l.Direction
                        };
                    });
                    return lineAngles.MinBy(la => la.Angle);
                }).ToList();

                if (holeRotationAngles.Any() && !holeRotationAngles.Any(ra => IsOrtogonal(ra.Angle)))
                {
                    Console.WriteLine($"   Starting rotate object {mesh.MeshName}");

                    var rotationPoint = obj.Loops.First().Points.First();
                    var rotationVector = holeRotationAngles.First();
                    obj.Loops.ToList().ForEach(l =>
                    {
                        var rotatedPoints = l.Points.Select(p =>
                        {
                            var point = new Point3d(p.X, p.Y, 0);
                            var newPoint = point.Rotate(
                                new Rotation(axisZVector, rotationVector.RotationAngle),
                                new Point3d(rotationPoint.X, rotationPoint.Y, 0));

                            return newPoint;
                        });

                        l.Points = rotatedPoints;
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
}
