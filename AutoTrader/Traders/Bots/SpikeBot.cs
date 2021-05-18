using System.Collections.Generic;
using AutoTrader.Api.Objects;

namespace AutoTrader.Traders.Bots
{
    public class SpikeBot : ITradingBot
    {
        public string Name => nameof(SpikeBot);
        protected TradingBotManager tradeManager { get; set; }

        public IList<CandleStick> Prices => new List<CandleStick>();

        private int lastBuy = -1;
        private int lastSell = -1;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        public SpikeBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            bool isBuy = Prices.IsSpike(i) < 0;
            if (isBuy)
            {
                if (i - lastBuy > 3)
                {
                    lastBuy = i;
                    return true;
                }
            }
            return false;
        }

        public bool Sell(int i)
        {
            bool isSell = Prices.IsSpike(i) > 0;
            if (isSell)
            {
                if (i - lastSell > 3)
                {
                    lastSell = i;
                    return true;
                }
            }
            return false;
        }

        public List<TradeItem> RefreshAll()
        {
            lastSell = -1;
            lastBuy = -1;
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
