using Flat3DObjectsToSvgConverter.Models.MeshObjectsParser;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using EdgeLoopParser = Flat3DObjectsToSvgConverter.Services.Parse3dObjects.EdgeLoopParser;

namespace Flat3DObjectsToSvgConverterTests.Services.Parse3dObjects.EdgeLoopParser
{
    public class EdgeLoopParserTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task ShouldExcludeFacesThatCrossRectangularLoopInCorner()
        {
            // Arrange
            var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var path = System.IO.Path.Combine($"{directory}\\Services\\Parse3dObjects\\EdgeLoopParser\\OBJs\\", "BoxCutCorner.obj");
            var content = await File.ReadAllLinesAsync(path);
            var mesh = ObjectsToLoopsConverter.ParseObjContent(content).First();
            var meshObject = (new MeshObjectsParser()).Parse(mesh).First();

            var sut = new Flat3DObjectsToSvgConverter.Services.Parse3dObjects.EdgeLoopParser();

            // Act
            var result = sut.GetMeshObjectsLoops(meshObject);
        }
    }
}