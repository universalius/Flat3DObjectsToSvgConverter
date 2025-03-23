using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using Flat3DObjectsToSvgConverter.Services.Kerf;
using FluentAssertions;
using GeometRi;
using Microsoft.Extensions.Options;
using System.Drawing;

namespace Flat3DObjectsToSvgConverterTests.Services.Kerf;

[TestFixture]
class KerfApplierTests
{
    private readonly KerfSettings _settings = new KerfSettings
    {
        X = 0.1,
        Y = 0.15,
        XY = 0.05
    };

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ShouldApplyKerfForVerticalAndHorizontalLines()
    {
        // Arrange
        var meshObject = GetMeshObjects(
        [
            new Point3d (0,0,0),
            new Point3d (0,1,0),
            new Point3d (1,1,0),
            new Point3d (1,0,0),
            new Point3d (0,0,0),
        ]);

        var sut = new KerfApplier(Options.Create(_settings), null, null);

        // Act
        sut.ApplyKerf([meshObject]);

        var x = (float)_settings.X;
        var y = (float)_settings.Y;
        meshObject.Objects.First().Loops.First().Points.Should().BeEquivalentTo(
        [
            new Point3d (0-y,0-x,0),
            new Point3d (0-y,1+x,0),
            new Point3d (1+y,1+x,0),
            new Point3d (1+y,0-x,0),
            new Point3d (0-y,0-x,0),
        ]);
    }

    [Test]
    public void ShouldApplyKerfForNotOrthogonalLines()
    {
        // Arrange
        var meshObject = GetMeshObjects(
        [
            new Point3d (0,0,0),
            new Point3d (1,1,0),
            new Point3d (2,0,0),
            new Point3d (1,-1,0),
            new Point3d (0,0,0),
        ]);

        var sut = new KerfApplier(Options.Create(_settings), null, null);

        // Act
        sut.ApplyKerf([meshObject]);

        var x = (float)_settings.X;
        var y = (float)_settings.Y;
        meshObject.Objects.First().Loops.First().Points.Should().BeEquivalentTo(
        [
            new Point3d (0-y,0-x,0),
            new Point3d (0-y,1+x,0),
            new Point3d (1+y,1+x,0),
            new Point3d (1+y,0-x,0),
            new Point3d (0-y,0-x,0),
        ]);
    }

    private MeshObjects GetMeshObjects(Point3d[] points)
    {
        return new MeshObjects
        {
            Objects =
            [
                new ObjectLoops
                {
                   Loops =
                   [
                       new LoopPoints
                       {
                           Points = points
                       }
                   ]
                }
            ]
        };
    }
}

