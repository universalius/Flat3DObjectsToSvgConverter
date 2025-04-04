using Flat3DObjectsToSvgConverter.Services.CleanLoops;
using Flat3DObjectsToSvgConverter.Services.Kerf;

namespace Flat3DObjectsToSvgConverter.Services.Parse3dObjects
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
