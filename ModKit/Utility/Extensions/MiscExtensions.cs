using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModKit.Utility.Extensions {
    public static class MiscExtensions {
        // Takes the last N objects of the source collection
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N) {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
