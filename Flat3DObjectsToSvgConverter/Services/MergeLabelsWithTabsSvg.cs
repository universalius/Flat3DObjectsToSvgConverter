using SvgLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class MergeLabelsWithTabsSvg
    {
        private readonly IOFileService _file;
        private const string labelsClass = "labels";

        public MergeLabelsWithTabsSvg(IOFileService file)
        {
            _file = file;
        }

        public void Merge(SvgDocument labelsSvg, SvgDocument tabsSvg)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start merging of labels and tabs svgs!");
            Console.WriteLine();

            var newSvgDocument = labelsSvg.Clone(true);
            var labelGroups = newSvgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>()
                .Select(e => new SvgGroup(e))
                .Where(g => g.HasClass(labelsClass))
                .ToArray();

            var tabGroups = tabsSvg.Element.GetElementsByTagName("g").Cast<XmlElement>()
                .Select(e => new SvgGroup(e))
                .Where(g => g.HasClass("tabs"))
                .ToArray();

            labelGroups.ToList().ForEach(g =>
            {
                var labelsGroup = g.Element;
                var parentGroup = new SvgGroup(labelsGroup.ParentNode as XmlElement);
                parentGroup.Element.RemoveChild(labelsGroup.PreviousSibling);

                var pathId = g.GetData("mainId");
                var tabsGroup = tabGroups.First(g => g.GetData("mainId") == pathId);

                var newTabsGroup = parentGroup.AddGroup();
                newTabsGroup.Transform = tabsGroup.Transform;
                newTabsGroup.Element.InnerXml = tabsGroup.Element.InnerXml;
            });

            _file.SaveSvg("labels_and_tabs", newSvgDocument.Element.OuterXml);

            watch.Stop();
            Console.WriteLine($"Finished merging of labels and tabs svgs! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();
        }
    }
}
