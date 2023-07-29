using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plain3DObjectsToSvgConverter.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunks<T>(this IEnumerable<T> source, int chunkSize)
        {
            T[] buffer;
            var array = source.ToArray();
            var chunks = new List<IEnumerable<T>>();
            for (int i = 0; i < array.Length; i += chunkSize)
            {
                buffer = new T[chunkSize];
                Array.Copy(array, i, buffer, 0, chunkSize);
                chunks.Add(buffer);
            }
            return chunks;
        }

        public static T PopAt<T>(this List<T> list, int index)
        {
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }

        public static T Pop<T>(this List<T> list)
        {
            var index = list.Count() - 1;
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }
    }
}
