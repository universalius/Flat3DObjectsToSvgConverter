using Flat3DObjectsToSvgConverter.Models;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops
{
    public class ObjectLoopsCleaner
    {
        private readonly ObjectLoopsAlligner _objectLoopsAlligner;
        private readonly ObjectLoopsPointsReducer _objectLoopsPointsReducer;
        private readonly ObjectLoopsTinyGapsRemover _objectLoopsTinyGapsRemover;

        public ObjectLoopsCleaner(ObjectLoopsAlligner objectLoopsAlligner, 
            ObjectLoopsPointsReducer objectLoopsPointsReducer, 
            ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover)
        {
            _objectLoopsAlligner = objectLoopsAlligner;
            _objectLoopsPointsReducer = objectLoopsPointsReducer;
            _objectLoopsTinyGapsRemover = objectLoopsTinyGapsRemover;
        }

        public void CleanLoops(IEnumerable<MeshObjects> meshes)
        {
            _objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
            _objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);
            _objectLoopsTinyGapsRemover.ReplaceGapsWithLine(meshes);
        }
    }
}
