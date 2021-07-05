using System;
using System.Collections.Generic;
using AutoTrader.Api.Objects;


namespace AutoTrader.Traders.Bots
{
    public static class BotUtils
    {
        public const double SpikeRatio = 1.07;

        public static int IsSpike(this IList<CandleStick> values, int i, double ratio = SpikeRatio)
        {
            double a = Math.Abs(values[i].close);
            int j = i - 1;
            while (j >= 0 && values[j] != null && i - j < 4)
            {
                double b = Math.Abs(values[j].close);
                double c = a > b ? a / b : b / a;
                if (c >= ratio)
                {
                    return Math.Sign(values[i].close - values[j].close);
                }
                j--;
            }
            return 0;
        }

        public static bool IsSpike(this double v1, double v2, double ratio = SpikeRatio)
        {
            double c = v1 > v2 ? v1 / v2 : v2 / v1;
            return c >= ratio;
        }

        public static ICollection<ICollection<T>> Permutations<T>(this ICollection<T> list)
        {
            var result = new List<ICollection<T>>();
            if (list.Count == 1)
            { // If only one possible permutation
                result.Add(list); // Add it and return it
                return result;
            }
            foreach (var element in list)
            { // For each element in that list
                var remainingList = new List<T>(list);
                remainingList.Remove(element); // Get a list containing everything except of chosen element
                foreach (var permutation in Permutations<T>(remainingList))
                { // Get all possible sub-permutations
                    permutation.Add(element); // Add that element
                    result.Add(permutation);
                }
            }
            return result;
        }
    }
}
