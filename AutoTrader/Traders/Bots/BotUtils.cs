using System;
using System.Collections.Generic;
using AutoTrader.Indicators;

namespace AutoTrader.Traders.Bots
{
    public static class BotUtils
    {
        public static bool IsCross<T, T2>(this List<T> value1, List<T2> value2, int i) 
            where T: ValueBase
            where T2: ValueBase
        {
            return Math.Sign(value1[i].Value - value2[i].Value) * Math.Sign(value1[i - 1].Value - value2[i - 1].Value) < 0;
        }
        public static int IsFlex<T>(this List<T> values, int i) where T: ValueBase
        {
            return values[i].Value * values[i - 1].Value < 0 ? Math.Sign(values[i].Value) : 0;
        }
        public static int IsTrend<T>(this List<T> values, int i) where T : ValueBase
        {
            if (i < 1 || values[i - 1] == null)
            {
                return 0;
            }
            int sign = Math.Sign(values[i - 1].Value);
            int j = i;
            do
            {
                j--;
            } while (j >= 0 && values[j] != null && Math.Sign(values[j].Value) == sign);
            return i - j;
        }

        public static int IsSpike<T>(this List<T> values, int i, double ratio = 1.2) where T : ValueBase
        {
            double a = Math.Abs(values[i].CandleStick.close);
            int j = i - 1;
            while (j >= 0 && values[j] != null && i - j < 3)
            {
                double b = Math.Abs(values[j].CandleStick.close);
                double c = a > b ? a / b : b / a;
                if (c >= ratio)
                {
                    return Math.Sign(values[i].CandleStick.close - values[j].CandleStick.close);
                }
                j--;
            }
            return 0;
        }
    }
}
