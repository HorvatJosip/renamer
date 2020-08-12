using System;
using System.Collections.Generic;
using System.Linq;

namespace Renamer
{
    public static class Extensions
    {
        public static bool HasElements<T>(this IEnumerable<T> collection)
            => collection != null && collection.Count() > 0;

        public static void Enumerate<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
    }
}
