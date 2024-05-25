using Flat3DObjectsToSvgConverter.Models;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops
{
    public class ObjectLoopsCleaner
    {
        private readonly ObjectLoopsAlligner _objectLoopsAlligner;
        private readonly ObjectLoopsPointsReducer _objectLoopsPointsReducer;
        private readonly ObjectLoopsTinyGapsRemover _objectLoopsTinyGapsRemover;
        private readonly ObjectLoopsSlotsReducer _objectLoopsSlotsReducer;

        public ObjectLoopsCleaner(ObjectLoopsAlligner objectLoopsAlligner,
            ObjectLoopsPointsReducer objectLoopsPointsReducer,
            ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover,
            ObjectLoopsSlotsReducer objectLoopsSlotsReducer)
        {
            _objectLoopsAlligner = objectLoopsAlligner;
            _objectLoopsPointsReducer = objectLoopsPointsReducer;
            _objectLoopsTinyGapsRemover = objectLoopsTinyGapsRemover;
            _objectLoopsSlotsReducer = objectLoopsSlotsReducer;
        }

        public void CleanLoops(IEnumerable<MeshObjects> meshes)
        {
            _objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
            _objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);
            _objectLoopsTinyGapsRemover.ReplaceGapsWithLine(meshes);
            _objectLoopsSlotsReducer.CloseSlots(meshes);
        }
    }
}
