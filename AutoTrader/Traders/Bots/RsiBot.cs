using System.Collections.Generic;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class RsiBot : TradingBotBase, ITradingBot
    {
        public const int OVERBOUGHT = 70;
        public const int OVERSOLD = 30;

        protected TradingBotManager botManager;

        public RsiBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
            BotName = nameof(RsiBot);
        }

        public string Name  => nameof(RsiBot);

        public List<TradeItem> RefreshAll()
        {
            var sellRule = Rule.Create(c => c.IsRsiOverbought() && !c.Next?.IsRsiOverbought() == true);
            var buyRule = Rule.Create(c => c.IsRsiOversold() && !c.Next?.IsRsiOversold() == true);//.And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));

            return GetTrades(botManager.PastPrices, sellRule, buyRule, TradePeriod.Long);
        }
    }
}
