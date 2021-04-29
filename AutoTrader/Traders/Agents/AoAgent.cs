using System;
using System.Collections.Generic;
using AutoTrader.GraphProviders;

namespace AutoTrader.Traders.Agents
{
    public class AoAgent : IAgent
    {
        private const int COLOR_COUNT = 2;

        public static double Ratio { get; set; } = 0;

        protected GraphCollection graphCollection;
        protected IList<AoValue> Ao => graphCollection.Ao;

        protected SmaProvider SlowSmaProvider => graphCollection.AoProvider.SlowSmaProvider;
        protected SmaProvider FastSmaProvider => graphCollection.AoProvider.FastSmaProvider;

        public AoAgent(GraphCollection graphCollection)
        {
            this.graphCollection = graphCollection;
        }

        public bool Buy(int i = -1)
        {
            if (i < 0)
            {
                i = graphCollection.AoProvider.AoIndex;
            }

            int oi = i;

            if (i >= 2)
            {
                bool buy = Ao[i - 1].Value < 0 && Ao[i].Value > 0;
                if (!buy)
                {
                    buy |= Ao[i].Value < 0 && Ao[i].Color == AoColor.Green && 
                           ColorCountBefore(ref i, AoColor.Green) == COLOR_COUNT &&                            
                           ColorCountBefore(ref i, AoColor.Red) > COLOR_COUNT;
                }
                Ao[oi].BuyMore = buy;
                Ao[oi].Buy = buy && PreviousBuyPrice(oi) <= Ao[oi].Price;
                return buy;
            }
            Ao[oi].Buy = false;
            Ao[oi].BuyMore = false;
            return false;
        }

        public bool Sell(int i = -1)
        {
            if (i < 0)
            {
                i = graphCollection.AoProvider.AoIndex;
            }

            int oi = i;

            if (i >= 2)
            {
                bool sell = Ao[i - 1]?.Value > 0 && Ao[i].Value < 0;
                if (!sell)
                {
                    sell |= 
                        Ao[i].Value > 0 && Ao[i].Color == AoColor.Red && 
                        ColorCountBefore(ref i, AoColor.Green) > COLOR_COUNT;
                }
                Ao[oi].SellMore = sell;
                Ao[oi].Sell = sell && PreviousSellPrice(oi) <= Ao[oi].Price;
                return sell;
            }
            return false;
        }

        private int ColorCountBefore(ref int i, AoColor color)
        {
            int c = 0;
            do
            {
                i--;
                c++;
            } while (i >= 0 && Ao[i].Color == color);
            return c; 
        }

        private double PreviousBuyPrice(int i)
        {
            int oi = i;
            double sumPrice = 0;
            int c = 0;
            do
            {
                if (Ao[i].Sell)
                {
                    sumPrice += Ao[i].Price - Ao[oi].Price;
                }
                else
                {
                    sumPrice += Ao[i].Price;
                }
                c++;
                i--;
            } while (i >= 0 && !Ao[i].BuyMore);
            return i < 0 || Ao[i].Sell ? Ao[oi].Price : sumPrice / c;
        }

        private double PreviousSellPrice(int i)
        {
            int oi = i;
            double sumPrice = 0;
            int c = 0;
            do
            {
                sumPrice += Ao[i].Price;
                c++;
                i--;
            } while (i >= 0 && !Ao[i].SellMore && !Ao[i].Buy);
            return i < 0 || Ao[i].Buy ? Ao[oi].Price : sumPrice / c;
        }

        private double ValueOf(AoColor color, int i)
        {
            double a = Ao[i].Value;
            int c = ColorCountBefore(ref i, color);
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

        public void Refresh(double? actualPrice, DateTime? date)
        {
            bool empty = graphCollection.PastPrices == null;
            graphCollection.Refresh(actualPrice, date);

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
            if (lastIndex >= 0)
            {
                Buy(lastIndex);
                Sell(lastIndex);
            }
        }

        public void RefreshAll()
        {
            for (int i = 0; i < Ao.Count; i++)
            {
                Buy(i);
                Sell(i);
            }
        }
    }
}
