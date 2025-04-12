using Flat3DObjectsToSvgConverter.Features.CleanLoops;
using Flat3DObjectsToSvgConverter.Features.Kerf;

namespace Flat3DObjectsToSvgConverter.Features.Parse3dObjects
{
    public class ThreeDObjectsParser(ObjectsToLoopsConverter objectsToLoopsConverter,
        ObjectLoopsToSvgConverter objectsToSvgConverter,
        ObjectLoopsCleaner objectLoopsCleaner
        , KerfApplier kerfApplier)
    {
        public async Task<string> Transform3DObjectsTo2DSvgLoops()
        {
            var meshesObjects = await objectsToLoopsConverter.Convert();

            objectLoopsCleaner.CleanLoops(meshesObjects);

            //kerfApplier.ApplyKerf(meshesObjects);

            return objectsToSvgConverter.ConvertAndSave(meshesObjects);
        }
    }
}
