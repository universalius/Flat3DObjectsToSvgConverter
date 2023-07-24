// See https://aka.ms/new-console-template for more information
using ObjParser;
using ObjParserExecutor;
using ObjParserExecutor.Models;
using Plain3DObjectsToSvgConverter;
using System.Diagnostics;
using System.IO;
using System.Linq;


var objectsLabelsToSvgConverter = new ObjectsLabelsToSvgConverter();
var labelsSvg = await objectsLabelsToSvgConverter.Convert();
File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\test_labels.svg", labelsSvg);

Console.ReadKey();
return;

var watch = Stopwatch.StartNew();

Console.WriteLine("Start parsing!");
Console.WriteLine();

var content = await File.ReadAllLinesAsync(@"D:\Виталик\Cat_Hack\ExportFiles\test1.obj");
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

var meshesObjects = meshes.Select((mesh, i) =>
{
    Console.WriteLine($"Starting process mesh - {mesh.Name}");

    var meshObjectsParser = new MeshObjectsParser();
    var meshObjects = meshObjectsParser.Parse(mesh);

    var edgeLoopParser = new EdgeLoopParser();
    var meshObjectsLoops = meshObjects.Select(mo => edgeLoopParser.GetMeshObjectsLoops(mo))
        .SelectMany(mol => mol.Objects).ToList();

    Console.WriteLine($"Converted to loops mesh - {mesh.Name}, loops - {meshObjectsLoops.Count()}");
    Console.WriteLine();
    Console.WriteLine($"Processed meshes {i + 1}/{meshes.Count}");
    Console.WriteLine();

    return new MeshObjects
    {
        MeshName = mesh.Name,
        Objects = meshObjectsLoops
    };
}).ToList();

var svgConverter = new ObjectsToSvgConverter();

var svg = svgConverter.Convert(meshesObjects);

File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\test.svg", svg);

watch.Stop();

var resultCurvesCount = meshesObjects.SelectMany(mo => mo.Objects.Select(o => o.Loops.Count())).Sum();
Console.WriteLine();
Console.WriteLine($"Finished parsing, processed {meshesObjects.Count()} meshes, generated {resultCurvesCount} curves, " +
    $"took - {watch.ElapsedMilliseconds / 1000.0} sec");

Console.ReadKey();


