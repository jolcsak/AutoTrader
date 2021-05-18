﻿using System;
using System.Collections.Generic;
using AutoTrader.Api.Objects;


namespace AutoTrader.Traders.Bots
{
    public static class BotUtils
    {
        public const double SpikeRatio = 1.07;

        //public static int IsCross<T, T2>(this List<T> value1, List<T2> value2, int i)
        //    where T : ValueBase
        //    where T2 : ValueBase
        //{
        //    bool isScross = Math.Sign(value1[i].Value - value2[i].Value) * Math.Sign(value1[i - 1].Value - value2[i - 1].Value) < 0;
        //    if (isScross)
        //    {
        //        return Math.Sign(value1[i].Value - value1[i - 1].Value);
        //    }
        //    return 0;
        //}
        //public static int IsFlex<T>(this List<T> values, int i) where T: ValueBase
        //{
        //    return values[i].Value * values[i - 1].Value < 0 ? Math.Sign(values[i].Value) : 0;
        //}

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
    }
}
