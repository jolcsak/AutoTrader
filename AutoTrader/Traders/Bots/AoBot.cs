using System.Collections.Generic;
using AutoTrader.Log;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class AoBot : ITradingBot
    {
        public string Name => nameof(RsiBot);

        protected TradingBotManager botManager;

        protected ITradeLogger Logger => TradeLogManager.GetLogger(GetType().Name);

        public AoBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();

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
                var buys = new SimpleRuleExecutor(ctx, buyRule).Execute();
                foreach (var buy in buys) {
                    tradeItems.Add(new TradeItem(buy.DateTime.DateTime, 0, TradeType.Buy, nameof(AoBot), TradePeriod.Long));
                }

                var sells = new SimpleRuleExecutor(ctx, sellRule).Execute();
                foreach (var sell in sells)
                {
                    tradeItems.Add(new TradeItem(sell.DateTime.DateTime, 0, TradeType.Sell, nameof(AoBot), TradePeriod.Long));
                }
            }

            return tradeItems;
        }
    }
}
