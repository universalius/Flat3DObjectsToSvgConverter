using Flat3DObjectsToSvgConverter.Models;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops
{
    public class ObjectLoopsCleaner
    {
        private readonly ObjectLoopsAlligner _objectLoopsAlligner;
        private readonly ObjectLoopsPointsReducer _objectLoopsPointsReducer;

        public ObjectLoopsCleaner(ObjectLoopsAlligner objectLoopsAlligner, ObjectLoopsPointsReducer objectLoopsPointsReducer)
        {
            _objectLoopsAlligner = objectLoopsAlligner;
            _objectLoopsPointsReducer = objectLoopsPointsReducer;
        }

        public void CleanLoops(IEnumerable<MeshObjects> meshes)
        {
            _objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
            _objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);
        }
    }
}
