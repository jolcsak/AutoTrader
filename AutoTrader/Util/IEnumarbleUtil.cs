using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Util
{
    public static class IEnumarbleUtil
    {
        public static void ForAll<T>(this IEnumerable<T> enumerable, Action<T> action, bool isParallel = false) where T: class
        {
            if (isParallel)
            {
                enumerable.AsParallel().ForAll(action);
            }
            else
            {
                foreach (var item in enumerable)
                {
                    action(item);
                }
            }
        }
    }
}
