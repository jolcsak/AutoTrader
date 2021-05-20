using System;
using System.Collections.Generic;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class TradingBotBase
    {

        private const int COOLDOWN_IN_MINUTES = 120;

        protected string BotName { get; set; } = string.Empty;

        protected List<TradeItem> GetTrades(IList<IOhlcv> prices, Predicate<global::Trady.Core.Infrastructure.IIndexedOhlcv> sellRule, Predicate<global::Trady.Core.Infrastructure.IIndexedOhlcv> buyRule, TradePeriod tradePeriod, int coolDown = COOLDOWN_IN_MINUTES)
        {
            int i = 0;
            List<TradeItem> tradeItems = new List<TradeItem>();
            using (var ctx = new AnalyzeContext(prices))
            {
                var buys = new SimpleRuleExecutor(ctx, buyRule).Execute();
                DateTime lastTrade = DateTime.MinValue;
                foreach (var buy in buys)
                {
                    if (buy.DateTime.DateTime > lastTrade.AddMinutes(coolDown))
                    {
                        tradeItems.Add(new TradeItem(buy.DateTime.DateTime, 0, TradeType.Buy, BotName, tradePeriod));
                        lastTrade = buy.DateTime.DateTime;
                    }
                    i++;
                }

                var sells = new SimpleRuleExecutor(ctx, sellRule).Execute();
                lastTrade = DateTime.MinValue;
                foreach (var sell in sells)
                {
                    if (sell.DateTime.DateTime > lastTrade.AddMinutes(coolDown))
                    {
                        tradeItems.Add(new TradeItem(sell.DateTime.DateTime, 0, TradeType.Sell, BotName, tradePeriod));
                        lastTrade = sell.DateTime.DateTime;
                    }
                    i++;
                }
            }
            return tradeItems;
        }
    }
}
