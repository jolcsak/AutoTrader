﻿using System;
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
    }
}
