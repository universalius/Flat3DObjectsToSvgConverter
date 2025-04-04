using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Services.Kerf;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops;

public class ObjectLoopsCleaner(
    ObjectLoopsAlligner objectLoopsAlligner,
    ObjectLoopsPointsReducer objectLoopsPointsReducer
    //ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover
    )
{
    public void CleanLoops(IEnumerable<MeshObjects> meshes)
    {
        objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
        objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);
        //objectLoopsTinyGapsRemover.ReplaceGapsWithLine(meshes);
    }
}

