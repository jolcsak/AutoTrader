using System;
using System.Collections.Generic;
using AutoTrader.GraphProviders;

namespace AutoTrader.Traders.Agents
{
    public class AoAgent : IAgent
    {
        public static double Ratio { get; set; } = 1;

        protected GraphCollection graphCollection;
        protected IList<AoValue> Ao => graphCollection.Ao;

        protected SmaProvider SlowSmaProvider => graphCollection.AoProvider.SlowSmaProvider;
        protected SmaProvider FastSmaProvider => graphCollection.AoProvider.FastSmaProvider;

        protected IList<double> Tendency => graphCollection.Tendency;

        public bool IsBuy => Ao.Count > 0 && Ao[Ao.Count - 1].Buy;

        public bool IsSell => Ao.Count > 0 && Ao[Ao.Count - 1].Buy;

        protected bool lastBuy = false;
        protected bool lastSell = false;

        protected int previousBuyMoreSma = 0;
        protected int previousSellMoreSma = 0;

        public AoAgent(GraphCollection graphCollection)
        {
            this.graphCollection = graphCollection;
        }

        public void Buy(int i)
        {
            if (i >= 2)
            {
                Ao[i].BuyMore = Math.Sign(Ao[i - 1].Value * Ao[i].Value) < 0;
                Ao[i].BuyMore |= Ao[i].Value < 0 && Ao[i].Color == AoColor.Green && Ao[i - 1].Color == AoColor.Red;
                if (Ao[i].BuyMore)
                {
                    previousBuyMoreSma = Ao[i].SmaIndex;
                    lastBuy = !Ao[i].BuyMore;
                }
                Ao[i].Buy = !lastBuy && (Ao[i].Value < 0) && (FastSmaProvider.Sma[Ao[i].SmaIndex]) > FastSmaProvider.Sma[previousBuyMoreSma] && T(i) > 0;
                if (Ao[i].Buy)
                {
                    lastBuy = true;
                }
            }
        }

        public void Sell(int i)
        {
            if (i >= 2)
            {
                Ao[i].SellMore = Math.Sign(Ao[i - 1].Value * Ao[i].Value) < 0;
                Ao[i].SellMore |= Ao[i].Value > 0 && Ao[i].Color == AoColor.Red && Ao[i - 1].Color == AoColor.Green;
                if (Ao[i].SellMore)
                {
                    previousSellMoreSma = Ao[i].SmaIndex;
                    lastSell = !Ao[i].SellMore;
                }
                Ao[i].Sell = !lastSell && (FastSmaProvider.Data[Ao[i].SmaIndex]) <= FastSmaProvider.Data[previousSellMoreSma];
                if (Ao[i].Sell)
                {
                    lastSell = true;
                }
            }
        }

        private int T(int i)
        {
            if (Tendency[Ao[i].SmaIndex] > Tendency[Ao[i - 1].SmaIndex])
            {
                return 1;
            }
            return -1;
        }

        public void RefreshAll()
        {
            graphCollection.Refresh();
            for (int i = 0; i < Ao.Count; i++)
            {
                Sell(i);
                Buy(i);
            }
        }
    }
}
