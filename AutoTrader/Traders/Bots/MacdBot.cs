using System.Collections.Generic;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : TradingBotBase, ITradingBot
    {

        public string Name => nameof(MacdBot);
        protected TradingBotManager botManager { get; set; }

        public MacdBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
            BotName = Name;
        }

        public List<TradeItem> RefreshAll()
        {
            var buyRule = Rule.Create(c => c.Prev != null && c.IsMacdBearishCross()).And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));
            var sellRule = Rule.Create(c => c.Prev != null && c.IsMacdBullishCross());

            return GetTrades(botManager.PastPrices, sellRule, buyRule, TradePeriod.Long);
        }
    }
}
