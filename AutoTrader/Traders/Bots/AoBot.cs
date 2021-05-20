using System.Collections.Generic;
using AutoTrader.Log;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class AoBot : TradingBotBase, ITradingBot
    {
        public string Name => nameof(RsiBot);

        protected TradingBotManager botManager;

        protected ITradeLogger Logger => TradeLogManager.GetLogger(GetType().Name);

        public AoBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
            BotName = nameof(AoBot);
        }

        public List<TradeItem> RefreshAll()
        {
            var buyRule = Rule.Create(c => c.IsFullStoBullishCross(14, 3, 3))
                .And(c => c.IsMacdOscBullish(12, 26, 9))
                .And(c => c.IsSmaOscBullish(10, 30))
                .And(c => c.IsAccumDistBullish())
                .And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));

            var sellRule = Rule.Create(c => c.IsFullStoBearishCross(14, 3, 3))
                .Or(c => c.IsMacdBearishCross(12, 24, 9))
                .Or(c => c.IsSmaBearishCross(10, 30));

            return GetTrades(botManager.PastPrices, sellRule, buyRule, TradePeriod.Short);
        }
    }
}
