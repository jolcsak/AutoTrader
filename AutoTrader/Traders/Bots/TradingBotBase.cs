using AutoTrader.Db.Entities;
using System;
using System.Collections.Generic;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class TradingBotBase : ITradingBot
    {
        protected static int ShortStopLossPercentage = -5;

        protected static int LongStopLossPercentage = -5;

        protected static int ShortTradeMaxAgeInHours = 8;

        protected static int LongTradeMinAgeInHours = 0;

        protected static int LongTradeMaxAgeInHours = 24;

        protected static int LongTradeSellAgeInHours = 0;

        protected static decimal MinRateOfChange = 0.05M;

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public virtual string Name { get; } = string.Empty;

        public virtual Predicate<IIndexedOhlcv> SellRule { get; } = Rule.Create(c => false);

        public virtual Predicate<IIndexedOhlcv> BuyRule { get; } = Rule.Create(c => false);

        protected TradingBotManager botManager;

        protected int coolDownInMinutes;

        protected TradePeriod tradePeriod;

        protected bool IsRsiOverSold { get; set; } = false;

        internal TradingBotBase(TradingBotManager botManager, TradePeriod tradePeriod, int coolDownInMinutes = 120)
        {
            this.botManager = botManager;
            this.coolDownInMinutes = coolDownInMinutes;
            this.tradePeriod = tradePeriod;
        }

        public List<TradeItem> RefreshAll()
        {
            int i = 0;
            List<TradeItem> tradeItems = new List<TradeItem>();
            using (var ctx = new AnalyzeContext(botManager.Prices))
            {
                var buys = new SimpleRuleExecutor(ctx, BuyRule).Execute();
                DateTime lastTrade = DateTime.MinValue;
                foreach (var buy in buys)
                {
                    if (buy.DateTime.DateTime > lastTrade.AddMinutes(coolDownInMinutes))
                    {
                        tradeItems.Add(new TradeItem(buy.DateTime.DateTime, 0, TradeType.Buy, Name, tradePeriod));
                        lastTrade = buy.DateTime.DateTime;
                    }
                    i++;
                }

                var sells = new SimpleRuleExecutor(ctx, SellRule).Execute();
                lastTrade = DateTime.MinValue;
                foreach (var sell in sells)
                {
                    if (sell.DateTime.DateTime > lastTrade.AddMinutes(coolDownInMinutes))
                    {
                        tradeItems.Add(new TradeItem(sell.DateTime.DateTime, 0, TradeType.Sell, Name, tradePeriod));
                        lastTrade = sell.DateTime.DateTime;
                    }
                    i++;
                }

                if (botManager.Prices.Count > 0)
                {
                    var lastIndexedCandle = new IndexedCandle(botManager.Prices, botManager.Prices.Count - 1);
                    IsRsiOverSold = lastIndexedCandle.IsRsiOversold();
                }
                else
                {
                    IsRsiOverSold = false;
                }
            }
            return tradeItems;
        }

        public virtual SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade)
        {
            bool isSell = lastTrade?.Type == TradeType.Sell;

            if (actualPrice.BuyPrice >= (tradeOrder.Price * TradeSettings.MinSellYield) && tradeOrder.FiatProfit > 0)
            {
                if (tradeOrder.Period == TradePeriod.Short || (tradeOrder.Period == TradePeriod.Long && isSell))
                {
                    return SellType.Profit;
                }
            }
            else
            {
                if (!IsRsiOverSold && isSell)
                {
                    bool isShortSell = tradeOrder.Period == TradePeriod.Short && tradeOrder.Age > ShortTradeMaxAgeInHours;
                    bool isLongSell = tradeOrder.Period == TradePeriod.Long && tradeOrder.Age > LongTradeMaxAgeInHours;
                    if (isShortSell || isLongSell)
                    {
                        return SellType.Loss;
                    }
                }
            }

            return SellType.None;
        }
    }
}
