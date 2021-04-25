using System;
using System.Collections.Generic;
using AutoTrader.GraphProviders;

namespace AutoTrader.Traders.Agents
{
    public class AoAgent : IAgent
    {
        public static double Ratio { get; set; } = 0;

        private static int cc = 6;

        protected GraphCollection graphCollection;
        protected IList<AoValue> Ao => graphCollection.Ao;

        protected SmaProvider SlowSmaProvider => graphCollection.AoProvider.SlowSmaProvider;
        protected SmaProvider FastSmaProvider => graphCollection.AoProvider.FastSmaProvider;

        public AoAgent(GraphCollection graphCollection)
        {
            this.graphCollection = graphCollection;
        }

        public bool IsBuy(int i = -1)
        {
            if (i < 0)
            {
                i = graphCollection.AoProvider.AoIndex;
            }

            if (i >= 2)
            {
                bool buy = Ao[i - 1].Value < 0 && Ao[i].Value > 0;
                if (!buy)
                {
                    buy |= Ao[i].Value < 0 && Ao[i].Color == AoColor.Green && ColorCountBefore(i, AoColor.Red) > cc && ValueOf(AoColor.Red, i) > Ratio;
                }
                return buy;
            }
            return false;
        }

        public bool IsSell(int i = -1)
        {
            if (i < 0)
            {
                i = graphCollection.AoProvider.AoIndex;
            }
            if (i >= 2)
            {
                bool sell = Ao[i - 1]?.Value > 0 && Ao[i].Value < 0;
                if (!sell)
                {
                    sell |= Ao[i].Value > 0 && Ao[i].Color == AoColor.Red && ColorCountBefore(i, AoColor.Green) > cc && ValueOf(AoColor.Green, i) > Ratio;
                }
                return sell;
            }
            return false;
        }

        private int ColorCountBefore(int i, AoColor color)
        {
            int c = 0;
            do
            {
                i--;
                c++;
            } while (i >= 0 && Ao[i].Color == color);
            return c; 
        }

        private double ValueOf(AoColor color, int i)
        {
            double a = Ao[i].Value;
            int c = ColorCountBefore(i, color);
            double valueA = Math.Abs(a);
            double valueB = Math.Abs(Ao[i].Value);
            var r = valueA > valueB ? valueA / valueB : valueB / valueA;
            return r * (r / c) * 10;
        }

        private bool IsInflexionPoint(int i)
        {
            if (i > 0)
            {
                var smaCurrent = SlowSmaProvider.Sma[Ao[i].SmaIndex];
                var smaPrevious = SlowSmaProvider.Sma[Ao[i - 1].SmaIndex];
                if (Ao[i].Value >= 0)
                {
                    return smaPrevious <= smaCurrent;
                }
                return smaCurrent >= smaPrevious;
            }
            return false;
        }

        public void Refresh(double? actualPrice)
        {
            bool empty = graphCollection.PastPrices == null;
            graphCollection.Refresh();

            if (empty)
            {
                RefreshAll();
            }
            else if (actualPrice.HasValue)
            {
                RefreshLast();
            }
        }

        private void RefreshLast()
        {
            int lastIndex = graphCollection.AoProvider.AoIndex;
            Ao[lastIndex].Buy = IsBuy(lastIndex);
            Ao[lastIndex].Sell = IsSell(lastIndex);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < Ao.Count; i++)
            {
                Ao[i].Buy = IsBuy(i);
                Ao[i].Sell = IsSell(i);
            }
        }
    }
}
