using AutoTrader.Db.Entities;
using System;
using System.Collections.Generic;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class TradingBotBase : ITradingBot
    {
        protected static int ShortStopLossPercentage = -25;

        protected static int LongStopLossPercentage = -35;

        protected static int ShortTradeMaxAgeInHours = 24;

        protected static int LongTradeMaxAgeInHours = 24 * 14;

        protected static decimal MinRateOfChange = 2.5M;

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public virtual string Name { get; } = string.Empty;

        public virtual Predicate<IIndexedOhlcv> SellRule { get; } = Rule.Create(c => false);

        public virtual Predicate<IIndexedOhlcv> BuyRule { get; } = Rule.Create(c => false);

        protected TradingBotManager botManager;

        protected int coolDownInMinutes;

        protected TradePeriod tradePeriod;

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
            }
            return tradeItems;
        }

        public virtual SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade)
        {
            if (actualPrice.BuyPrice >= (tradeOrder.Price * TradeSettings.MinSellYield))
            {
                if (tradeOrder.Period == TradePeriod.Short || (tradeOrder.Period == TradePeriod.Long && lastTrade?.Type == TradeType.Sell))
                {
                    return SellType.Profit;
                }
            }
            else
            {
                bool isShortSell = tradeOrder.Period == TradePeriod.Short && (tradeOrder.ActualYield < ShortStopLossPercentage || tradeOrder.BuyDate.AddHours(ShortTradeMaxAgeInHours) < DateTime.Now);
                bool isLongSell = tradeOrder.Period == TradePeriod.Long && (tradeOrder.ActualYield < LongStopLossPercentage || tradeOrder.BuyDate.AddHours(LongTradeMaxAgeInHours) < DateTime.Now);
                if (isShortSell || isLongSell)
                {
                    return SellType.Loss;
                }
            }

            return SellType.None;
        }
    }
}
