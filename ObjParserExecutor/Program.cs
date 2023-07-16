// See https://aka.ms/new-console-template for more information
using ObjParser;
using ObjParserExecutor;
using System.IO;
using System.Linq;

Console.WriteLine("Start parsing!");

var content = await File.ReadAllLinesAsync(@"D:\Виталик\Cat_Hack\ExportFiles\test.obj");
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
    var meshObjectsParser = new MeshObjectsParser();
    var meshObjects = meshObjectsParser.Parse(mesh.Obj);

    var edgeLoopParser = new EdgeLoopParser();
    var loopsPoints = meshObjects.Select(mo => edgeLoopParser.GetEdgeLoopPoints(mo));
    return new MeshLoopPoints
    {
        MeshName = mesh.Name,
        ObjectsLoopsPoints = loopsPoints
    };
});

//var testObj = meshes.First().Obj;

//var meshObjectsParser = new MeshObjectsParser();
//var meshObjects = meshObjectsParser.Parse(testObj);

//var edgeLoopParser = new EdgeLoopParser();
//var loopsPoints = meshObjects.Select(mo => edgeLoopParser.GetEdgeLoopPoints(mo));

var svgConverter = new SvgConverter();

var svg = svgConverter.Convert(meshLoopsPoints);

File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\test.svg", svg);

Console.ReadKey();


