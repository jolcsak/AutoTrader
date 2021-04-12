using System.Collections.Generic;

namespace AutoTrader.Utils
{
    public static class ListUtils
    {
        public static T Previous<T>(this IList<T> list)
        {
            if (list.Count > 1)
            {
                return list[list.Count - 1];
            }
            return default(T);
        }

        public static T Previous<T>(this IList<T> list, int i)
        {
            if (list.Count > i)
            {
                return list[list.Count - i];
            }
            return default(T);
        }
    }
}
