using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Services.Kerf;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops;

public class ObjectLoopsCleaner(
    ObjectLoopsAlligner objectLoopsAlligner,
    ObjectLoopsPointsReducer objectLoopsPointsReducer,
    ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover,
    KerfApplier kerfApplier)
{
    public void CleanLoops(IEnumerable<MeshObjects> meshes)
    {
        objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
        objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);

        kerfApplier.ApplyKerf(meshes);

        objectLoopsTinyGapsRemover.ReplaceGapsWithLine(meshes);
    }
}

