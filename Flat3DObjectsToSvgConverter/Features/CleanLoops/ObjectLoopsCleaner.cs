using Flat3DObjectsToSvgConverter.Models;

namespace Flat3DObjectsToSvgConverter.Features.CleanLoops;

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

