using System.Collections.Generic;
using Trady.Analysis;
using Trady.Analysis.Extension;

namespace AutoTrader.Traders.Bots
{
    public class SpikeBot : TradingBotBase, ITradingBot
    {
        private const int COOLDOWN_IN_MINUTES = 60;
        private const int PRICE_PERCENTAGE_CHANGE = 5;

        public string Name => nameof(SpikeBot);
        protected TradingBotManager botManager { get; set; }

        public SpikeBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
            BotName = Name;
        }

        public List<TradeItem> RefreshAll()
        {
            var sellRule = Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() > PRICE_PERCENTAGE_CHANGE);
            var buyRule = Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() < -PRICE_PERCENTAGE_CHANGE).And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));

            return GetTrades(botManager.PastPrices, sellRule, buyRule, TradePeriod.Short, COOLDOWN_IN_MINUTES);
        }
    }
}
