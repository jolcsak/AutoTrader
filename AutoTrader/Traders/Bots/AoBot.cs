using System;
using System.Collections.Generic;
using AutoTrader.Log;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;

namespace AutoTrader.Traders.Bots
{
    public class AoBot : ITradingBot
    {
        public string Name => nameof(RsiBot);

        public static double Ratio { get; set; } = 1;

        protected TradingBotManager botManager;
        //protected IList<AoHistValue> Ao => botManager.Ao;

        protected SimpleMovingAverage SmaSlow => botManager.SmaSlow;

        protected SimpleMovingAverage SmaFast => botManager.SmaFast;

        public bool IsBuy => false;

        public bool IsSell => false;

        protected bool lastBuy = false;
        protected bool lastSell = false;

        protected int previousBuyMoreSma = 0;
        protected int previousSellMoreSma = 0;
        protected double priceChange = 0;

        protected double lastPrice = 0;

        protected ITradeLogger Logger => TradeLogManager.GetLogger(GetType().Name);

        public AoBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
        }

        public bool Buy(int i)
        {
            if (i >= 2)
            {
                //int j = Ao[i].SmaIndex;
                //if (j >= SmaSlow.Count)
                //{
                //    return false;
                //}
                //Ao[i].Buy = SmaFast[j - 1].Tick <= SmaSlow[j - 1].Tick && SmaFast[j].Tick >= SmaSlow[j].Tick;
                //double ratio = Ao[i].Value < 0 ? 1.15 : 1.05;
                //Ao[i].Buy &= SmaFast[j].Tick * ratio < lastPrice;
                //Ao[i].Buy &= SmaSlow[j].Value < SmaSlow[j-1].Value;
                //if (Ao[i].Buy)
                //{
                //    lastPrice = SmaFast[j].CandleStick.close;
                //    return true;
                //}

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

                //int j = Ao[i].SmaIndex;
                //if (j >= SmaFast.Count)
                //{
                //    return false;
                //}
                //Ao[i].Sell = SmaFast[j - 1].Value >= SmaSlow[j - 1].Value && SmaFast[j].Value <= SmaSlow[j].Value;
                ////Ao[i].Sell |= graphCollection.SmaFast[j].CandleStick.close > lastPrice * 1.02;
                //double ratio = Ao[i].Value > 0 ? 1.15 : 1.05;
                //Ao[i].Sell &= SmaFast[j].CandleStick.close > lastPrice * ratio;
                //if (Ao[i].Sell)
                //{
                //    lastPrice = SmaFast[j].CandleStick.close;
                //    return true;
                //}

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
            //if (Ao.Count > 0) {
            //    lastPrice = Ao[0].Date.close;
            //    for (int i = 0; i < Ao.Count; i++)
            //    {
            //        bool isBuy = false;
            //        bool isSell = Sell(i);
            //        if (!isSell)
            //        {
            //            isBuy = Buy(i);
            //        }

            //        if (isBuy || isSell)
            //        {
            //            tradeItems.Add(new TradeItem(Ao[i].Date.Date, Ao[i].Date.close, isBuy ? TradeType.Buy : TradeType.Sell, Name, TradePeriod.Long));
            //        }
            //    }
            //}

            // Build buy rule & sell rule based on various patterns
            var buyRule = Rule.Create(c => c.IsFullStoBullishCross(14, 3, 3))
                .And(c => c.IsMacdOscBullish(12, 26, 9))
                .And(c => c.IsSmaOscBullish(10, 30))
                .And(c => c.IsAccumDistBullish());

            var sellRule = Rule.Create(c => c.IsFullStoBearishCross(14, 3, 3))
                .Or(c => c.IsMacdBearishCross(12, 24, 9))
                .Or(c => c.IsSmaBearishCross(10, 30));


            using (var ctx = new AnalyzeContext(botManager.PastPrices))
            {
                var validObjects = new SimpleRuleExecutor(ctx, buyRule).Execute();
                Console.WriteLine(validObjects.Count);
            }

            return tradeItems;
        }
    }
}
