// See https://aka.ms/new-console-template for more information
using ObjParser;
using ObjParserExecutor;
using System.IO;
using System.Linq;

Console.WriteLine("Start parsing!");

var content = await File.ReadAllLinesAsync(@"D:\Виталик\Cat_Hack\ExportFiles\test3.obj");
var contentWitoutComments = content.Where(l => !l.StartsWith("#"));

var meshesText = string.Join(Environment.NewLine, contentWitoutComments)
    .Split("o ")
    .Where(t => !(string.IsNullOrEmpty(t) || t == "\r\n")).ToList();

var meshes = new List<Mesh>();
meshesText.ForEach(t =>
{
    var meshLines = t.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    var obj = new Obj();
    obj.VertexListShift = meshes.Any() ? meshes.Last().Obj.VertexList.Last().Index : 0;
    obj.LoadObj(meshLines.Skip(1));
    meshes.Add(new Mesh
    {
        Name = meshLines[0],
        Obj = obj
    });
});

var meshLoopsPoints = meshes.Select(mesh =>
{
    Console.WriteLine($"Starting process mesh - {mesh.Name}");

    var meshObjectsParser = new MeshObjectsParser();
    var meshObjects = meshObjectsParser.Parse(mesh);

    var edgeLoopParser = new EdgeLoopParser();
    var meshObjectsLoopsPoints = meshObjects.Select(mo => edgeLoopParser.GetEdgeLoopPoints(mo)).ToList();

    Console.WriteLine($"Converted to loops mesh - {mesh.Name}, objects - {meshObjectsLoopsPoints.Count()}");
    Console.WriteLine();

    return new MeshLoopPoints
    {
        MeshName = mesh.Name,
        ObjectsLoopsPoints = meshObjectsLoopsPoints.SelectMany(molp=> molp)
    };
});

var svgConverter = new SvgConverter();

var svg = svgConverter.Convert(meshLoopsPoints);

File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\test.svg", svg);

Console.ReadKey();


