using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots;

public class ObjectLoopsSlotsCloser(
    ObjectLoopsSlotsCutter objectLoopsSlotsReducer,
    ObjectLoopsGearsCutter objectLoopsGearsCutter,
    ObjectLoopsSlotSizeReducer objectLoopsSlotSizeReducer,
    IOptions<FeaturesSettings> featuresOptions)
{
    public string CloseSlots(string svg)
    {
        var newSvg = svg;
        //_objectLoopsSlotSizeReducer.ChangeSlotsSize(meshes);
        if (featuresOptions.Value.Slots.CloseSlots)
        {
            newSvg = objectLoopsSlotsReducer.CloseSlots(svg);
        }
        return objectLoopsGearsCutter.CutTeeth(newSvg);
    }
}
