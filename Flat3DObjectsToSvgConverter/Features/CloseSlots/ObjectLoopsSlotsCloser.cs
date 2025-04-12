using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots
{
    public class ObjectLoopsSlotsCloser
    {
        private readonly ObjectLoopsSlotsCutter _objectLoopsSlotsReducer;
        private readonly ObjectLoopsGearsCutter _objectLoopsGearsCutter;
        private readonly ObjectLoopsSlotSizeReducer _objectLoopsSlotSizeReducer;
        private readonly SlotsSettings _slotsSettings;

        public ObjectLoopsSlotsCloser(ObjectLoopsSlotsCutter objectLoopsSlotsReducer,
            ObjectLoopsGearsCutter objectLoopsGearsCutter,
            ObjectLoopsSlotSizeReducer objectLoopsSlotSizeReducer,
            IOptions<SlotsSettings> options)
        {
            _objectLoopsSlotsReducer = objectLoopsSlotsReducer;
            _objectLoopsGearsCutter = objectLoopsGearsCutter;
            _objectLoopsSlotSizeReducer = objectLoopsSlotSizeReducer;
            _slotsSettings = options.Value;
        }

        public void CloseSlots(IEnumerable<MeshObjects> meshes)
        {
            //_objectLoopsSlotSizeReducer.ChangeSlotsSize(meshes);
            if (_slotsSettings.CloseSlots)
            {
                _objectLoopsSlotsReducer.CloseSlots(meshes);
            }
            _objectLoopsGearsCutter.CutTeeth(meshes);
        }
    }
}
