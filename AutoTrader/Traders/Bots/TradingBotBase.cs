using System;
using System.Collections.Generic;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class TradingBotBase : ITradingBot
    {
        public virtual string Name { get; } = string.Empty;

        public virtual Predicate<IIndexedOhlcv> SellRule { get; } = Rule.Create(c => false);

        public virtual Predicate<IIndexedOhlcv> BuyRule { get; } = Rule.Create(c => false);

        protected TradingBotManager botManager;

        protected int coolDownInMinutes;

        protected TradePeriod tradePeriod;

        protected TradingBotBase(TradingBotManager botManager, TradePeriod tradePeriod, int coolDownInMinutes = 120)
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
    }
}
