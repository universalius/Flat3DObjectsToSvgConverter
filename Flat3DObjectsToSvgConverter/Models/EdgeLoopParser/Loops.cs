namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class Loops
    {
        public LoopEdges Main { get; set; }
        public IEnumerable<LoopEdges> Children { get; set; }
    }
}
