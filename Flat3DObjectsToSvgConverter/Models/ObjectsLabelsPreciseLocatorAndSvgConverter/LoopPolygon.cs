namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class LoopPolygon
    {
        public LoopPath LoopPath { get; set; }
        public LabelSvgGroup LabelLetters { get; set; }
        public PathPolygon Polygon { get; set; }
        public IEnumerable<LoopPolygon> Neighbors { get; set; }
    }
}
