using System.Collections.Generic;
using AutoTrader.Api.Objects;

namespace AutoTrader.Traders.Bots
{
    public class SpikeBot : ITradingBot
    {
        public string Name => nameof(SpikeBot);
        protected TradingBotManager tradeManager { get; set; }

        public IList<CandleStick> Prices => tradeManager.PastPrices;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        public SpikeBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            return Prices.IsSellSpike(i) < 0;
        }

        public bool Sell(int i)
        {
            return Prices.IsBuySpike(i) > 0;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();
            for (int i = 0; i < Prices.Count; i++)
            {
                bool isBuy = false;
                bool isSell = Sell(i);
                if (!isSell)
                {
                    isBuy = Buy(i);
                }
                if (isBuy || isSell)
                {
                    tradeItems.Add(new TradeItem(Prices[i].Date, Prices[i].close, isBuy ? TradeType.Buy : TradeType.Sell, Name, TradePeriod.Short));
                }
            }
            return tradeItems;
        }
    }
}
