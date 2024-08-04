using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops
{
    public class ObjectLoopsCleaner
    {
        private readonly ObjectLoopsAlligner _objectLoopsAlligner;
        private readonly ObjectLoopsPointsReducer _objectLoopsPointsReducer;
        private readonly ObjectLoopsTinyGapsRemover _objectLoopsTinyGapsRemover;
        private readonly ObjectLoopsSlotsCutter _objectLoopsSlotsReducer;
        private readonly ObjectLoopsGearsCutter _objectLoopsGearsCutter;
        private readonly ObjectLoopsSlotSizeReducer _objectLoopsSlotSizeReducer;
        private readonly SlotsSettings _slotsSettings;

        public ObjectLoopsCleaner(ObjectLoopsAlligner objectLoopsAlligner,
            ObjectLoopsPointsReducer objectLoopsPointsReducer,
            ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover,
            ObjectLoopsSlotsCutter objectLoopsSlotsReducer,
            ObjectLoopsGearsCutter objectLoopsGearsCutter,
            ObjectLoopsSlotSizeReducer objectLoopsSlotSizeReducer,
            IOptions<SlotsSettings> options)
        {
            _objectLoopsAlligner = objectLoopsAlligner;
            _objectLoopsPointsReducer = objectLoopsPointsReducer;
            _objectLoopsTinyGapsRemover = objectLoopsTinyGapsRemover;
            _objectLoopsSlotsReducer = objectLoopsSlotsReducer;
            _objectLoopsGearsCutter = objectLoopsGearsCutter;
            _objectLoopsSlotSizeReducer = objectLoopsSlotSizeReducer;
            _slotsSettings = options.Value;
        }

        public void CleanLoops(IEnumerable<MeshObjects> meshes)
        {
            _objectLoopsPointsReducer.RemoveRedundantPoints(meshes);
            //_objectLoopsSlotSizeReducer.ChangeSlotsSize(meshes);
            _objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshes);
            _objectLoopsTinyGapsRemover.ReplaceGapsWithLine(meshes);
            if (_slotsSettings.CloseSlots)
            {
                _objectLoopsSlotsReducer.CloseSlots(meshes);
            }
            _objectLoopsGearsCutter.CutTeeth(meshes);
        }
    }
}
