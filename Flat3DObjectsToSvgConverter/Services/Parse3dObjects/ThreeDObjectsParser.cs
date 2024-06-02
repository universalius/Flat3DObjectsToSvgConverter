using Flat3DObjectsToSvgConverter.Services.CleanLoops;

namespace Flat3DObjectsToSvgConverter.Services.Parse3dObjects
{
    public class ThreeDObjectsParser
    {
        private readonly ObjectsToLoopsConverter _objectsToLoopsConverter;
        private readonly ObjectLoopsToSvgConverter _objectsToSvgConverter;
        private readonly ObjectLoopsCleaner _objectLoopsCleaner;

        public ThreeDObjectsParser(ObjectsToLoopsConverter objectsToLoopsConverter,
            ObjectLoopsToSvgConverter objectsToSvgConverter,
            ObjectLoopsCleaner objectLoopsCleaner)
        {
            _objectsToLoopsConverter = objectsToLoopsConverter;
            _objectsToSvgConverter = objectsToSvgConverter;
            _objectLoopsCleaner = objectLoopsCleaner;
        }

        public async Task<string> Transform3DObjectsTo2DSvgLoops()
        {
            var meshesObjects = await _objectsToLoopsConverter.Convert();

            _objectLoopsCleaner.CleanLoops(meshesObjects);

            return _objectsToSvgConverter.Convert(meshesObjects);
        }
    }
}
