using System.Collections.Generic;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class RsiBot : ITradingBot
    {

        public const int OVERBOUGHT = 70;
        public const int OVERSOLD = 30;

        protected TradingBotManager botManager;


        public RsiBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
        }

        public string Name  => nameof(RsiBot);

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();

            // Build buy rule & sell rule based on various patterns
            var sellRule = Rule.Create(c => c.IsRsiOverbought());
            var buyRule = Rule.Create(c => c.IsRsiOversold());
            
            using (var ctx = new AnalyzeContext(botManager.PastPrices))
            {
                var buys = new SimpleRuleExecutor(ctx, buyRule).Execute();
                foreach (var buy in buys)
                {
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
