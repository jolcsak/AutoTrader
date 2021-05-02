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

        protected bool lastBuy = false;
        protected bool lastSell = false;

        protected int previousBuyMoreSma = 0;
        protected int previousSellMoreSma = 0;

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

            if (i >= 2)
            {
                Ao[i].BuyMore = Math.Sign(Ao[i - 1].Value * Ao[i].Value) < 0;
                Ao[i].BuyMore |= Ao[i].Value < 0 && Ao[i].Color == AoColor.Green && Ao[i - 1].Color == AoColor.Red;
                if (Ao[i].BuyMore)
                {
                    previousBuyMoreSma = Ao[i].SmaIndex;
                    lastBuy = !Ao[i].BuyMore;
                }
                Ao[i].Buy = !lastBuy && (Ao[i].Value < 0) && (FastSmaProvider.Sma[Ao[i].SmaIndex]) > FastSmaProvider.Sma[previousBuyMoreSma];
                if (Ao[i].Buy)
                {
                    lastBuy = true;
                }
                return lastBuy;
            }
            Ao[i].Buy = false;
            Ao[i].BuyMore = false;
            return false;
        }

        public bool Sell(int i = -1)
        {
            if (i < 0)
            {
                i = graphCollection.AoProvider.AoIndex;
            }

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
                return lastSell;
            }
            Ao[i].Sell = false;
            Ao[i].SellMore = false;
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
                Sell(lastIndex);
                Buy(lastIndex);
            }
        }

        public void RefreshAll()
        {
            for (int i = 0; i < Ao.Count; i++)
            {
                Sell(i);
                Buy(i);
            }
        }
    }
}
