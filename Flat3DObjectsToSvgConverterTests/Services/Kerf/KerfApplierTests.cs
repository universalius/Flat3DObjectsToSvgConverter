using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using Flat3DObjectsToSvgConverter.Services.Kerf;
using FluentAssertions;
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
            new PointF (0,0),
            new PointF (0,1),
            new PointF (1,1),
            new PointF (1,0),
            new PointF (0,0),
        ]);

        var sut = new KerfApplier(Options.Create(_settings));

        // Act
        sut.ApplyKerf([meshObject]);

        var x = (float)_settings.X;
        var y = (float)_settings.Y;
        meshObject.Objects.First().Loops.First().Points.Should().BeEquivalentTo(
        [
            new PointF (0-y,0-x),
            new PointF (0-y,1+x),
            new PointF (1+y,1+x),
            new PointF (1+y,0-x),
            new PointF (0-y,0-x),
        ]);
    }

    [Test]
    public void ShouldApplyKerfForNotOrthogonalLines()
    {
        // Arrange
        var meshObject = GetMeshObjects(
        [
            new PointF (0,0),
            new PointF (1,1),
            new PointF (2,0),
            new PointF (1,-1),
            new PointF (0,0),
        ]);

        var sut = new KerfApplier(Options.Create(_settings));

        // Act
        sut.ApplyKerf([meshObject]);

        var x = (float)_settings.X;
        var y = (float)_settings.Y;
        meshObject.Objects.First().Loops.First().Points.Should().BeEquivalentTo(
        [
            new PointF (0-y,0-x),
            new PointF (0-y,1+x),
            new PointF (1+y,1+x),
            new PointF (1+y,0-x),
            new PointF (0-y,0-x),
        ]);
    }

    private MeshObjects GetMeshObjects(PointF[] points)
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

