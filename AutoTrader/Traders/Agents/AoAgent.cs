using System;
using System.Collections.Generic;
using AutoTrader.GraphProviders;
using AutoTrader.Log;

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

        public bool IsSell => Ao.Count > 0 && Ao[Ao.Count - 1].Sell;

        protected bool lastBuy = false;
        protected bool lastSell = false;

        protected int previousBuyMoreSma = 0;
        protected int previousSellMoreSma = 0;
        protected double priceChange = 0;

        protected double lastPrice = 0;

        protected ITradeLogger Logger => TradeLogManager.GetLogger(GetType().Name);

        public AoAgent(GraphCollection graphCollection)
        {
            this.graphCollection = graphCollection;
        }

        public bool Buy(int i)
        {
            if (i >= 2)
            {
                int j = i + graphCollection.PricesSkip;
                Ao[i].Buy = graphCollection.SmaFast[j - 1].Value <= graphCollection.SmaSlow[j - 1].Value && graphCollection.SmaFast[j].Value >= graphCollection.SmaSlow[j].Value;
                double ratio = Ao[i].Value < 0 ? 1.1 : 1.01;
                Ao[i].Buy &= graphCollection.SmaFast[j].Value * ratio < lastPrice;
                if (Ao[i].Buy)
                {
                    lastPrice = graphCollection.SmaFast[j].Value;
                    return true;
                }

                //graphCollection.SmaFast[Ao[i].SmaIndex] - graphCollection.SmaFast[Ao[i -1].SmaIndex]
                //Ao[i].BuyMore = Math.Sign(Ao[i - 1].Value * Ao[i].Value) < 0;
                //Ao[i].BuyMore |= Ao[i].Value < 0 && Ao[i].Color == AoColor.Green && Ao[i - 1].Color == AoColor.Red && Ao[i - 2].Color == AoColor.Red;
                ////Ao[i].BuyMore |= Tendency[Ao[i].SmaIndex] > 0 && Tendency[Ao[i - 1].SmaIndex] < 0;
                //if (Ao[i].BuyMore)
                //{
                //    priceChange = FastSmaProvider.Sma[Ao[i].SmaIndex] / FastSmaProvider.Sma[previousBuyMoreSma];
                //    previousBuyMoreSma = Ao[i].SmaIndex;
                //    lastBuy = !Ao[i].BuyMore;
                //}
                //Ao[i].Buy = !lastBuy && (Ao[i].Value < 0) && FastSmaProvider.Sma[Ao[i].SmaIndex] >= FastSmaProvider.Sma[previousBuyMoreSma] && priceChange < 0.91;
                //Ao[i].Buy |= Tendency[Ao[i].SmaIndex] > 0 && Tendency[Ao[i-1].SmaIndex] < 0;
                //if (Ao[i].Buy)
                //{
                //    lastBuy = true;
                //}
            }
            return false;
        }

        public bool Sell(int i)
        {
            if (i >= 2)
            {

                int j = i + graphCollection.PricesSkip;
                Ao[i].Sell = graphCollection.SmaFast[j - 1].Value >= graphCollection.SmaSlow[j - 1].Value && graphCollection.SmaFast[j].Value <= graphCollection.SmaSlow[j].Value;
                double ratio = Ao[i].Value > 0 ? 1.1 : 1.01;
                Ao[i].Sell &= graphCollection.SmaFast[j].Value > lastPrice * ratio;
                if (Ao[i].Sell)
                {
                    lastPrice = graphCollection.SmaFast[j].Value;
                    return true;
                }

                //Ao[i].SellMore = Math.Sign(Ao[i - 1].Value * Ao[i].Value) < 0;
                //Ao[i].SellMore |= Ao[i].Value > 0 && Ao[i].Color == AoColor.Red && Ao[i - 1].Color == AoColor.Green && Ao[i - 2].Color == AoColor.Green;
                //Ao[i].SellMore |= Tendency[Ao[i].SmaIndex] < 0 && Tendency[Ao[i - 1].SmaIndex] > 0;

                //if (Ao[i].SellMore)
                //{
                //    priceChange = FastSmaProvider.Sma[Ao[i].SmaIndex] / FastSmaProvider.Sma[previousBuyMoreSma];
                //    previousSellMoreSma = Ao[i].SmaIndex;
                //    lastSell = !Ao[i].SellMore;
                //}
                //Ao[i].Sell = !lastSell && (FastSmaProvider.Data[Ao[i].SmaIndex]) <= FastSmaProvider.Data[previousSellMoreSma] && priceChange > 1.1;
                //Ao[i].Sell |= Tendency[Ao[i].SmaIndex] < 0 && Tendency[Ao[i - 1].SmaIndex] > 0;
                //if (Ao[i].Sell)
                //{
                //    lastSell = true;
                //}
            }
            return false;
        }

        public List<TradeItem> RefreshAll()
        {
            previousBuyMoreSma = 0;
            previousSellMoreSma = 0;
            priceChange = 0;
            List<TradeItem> tradeItems = new List<TradeItem>();
            for (int i = 0; i < Ao.Count; i++)
            {
                bool isBuy = false;
                bool isSell = Sell(i);
                if (!isSell)
                {
                    isBuy = Buy(i);
                }

                if (isBuy || isSell)
                {
                    tradeItems.Add(new TradeItem(Ao[i].CandleStick.Date, Ao[i].CandleStick.close, isBuy ? TradeType.Buy : TradeType.Sell));
                }
            }
            return tradeItems;
        }
    }
}
