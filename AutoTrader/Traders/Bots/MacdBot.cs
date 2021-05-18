using System.Collections.Generic;
using Trady.Analysis.Indicator;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : ITradingBot
    {
        public string Name => nameof(MacdBot);

        protected TradingBotManager tradeManager { get; set; }

        public MovingAverageConvergenceDivergence Line => tradeManager.Macd;
        public MovingAverageConvergenceDivergenceHistogram Histogram => tradeManager.MacdHistogram;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        public MacdBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            //return Signal.IsCross(Line, i) < 0;
            return false;
        }

        public bool Sell(int i)
        {
            //return Signal.IsCross(Line, i) > 0;
            return false;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();
            //for (int i = 0; i < Signal.Count; i++)
            //{
            //    if (i > 0 && Signal[i] != null && Signal[i - 1] != null)
            //    {
            //        bool isBuy = false;
            //        bool isSell = Sell(i);
            //        if (!isSell)
            //        {
            //            isBuy = Buy(i);
            //        }
            //        if (isBuy || isSell)
            //        {
            //          tradeItems.Add(new TradeItem(Signal[i].CandleStick.Date, Signal[i].CandleStick.close, isBuy ? TradeType.Buy : TradeType.Sell, Name, TradePeriod.Short));
            //        }
            //    }
            //}
            return tradeItems;
        }
    }
}
