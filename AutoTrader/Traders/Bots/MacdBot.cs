using AutoTrader.Indicators;
using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : ITradingBot
    {
        public static double Ratio { get; set; } = 1.05;

        protected TradingBotManager tradeManager { get; set; }

        public List<MacdLineValue> Line => tradeManager.MacdProvider.Result.Line;
        public List<MacdHistogramValue> Histogram => tradeManager.MacdProvider.Result.Histogram;
        public List<EmaValue> Signal => tradeManager.MacdProvider.Result.Signal;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        private double previousTradePrice = 0;

        private bool buyMarker = false;
        private bool sellMarker = false;

        public MacdBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            if (Histogram[i].Value > 0 && Histogram[i - 1].Value < 0)
            {
                buyMarker = Histogram[i].CandleStick.high * Ratio < previousTradePrice;
            }
            if (buyMarker && (Histogram[i].Value > Histogram[i - 1].Value * 1.015))
            {
                buyMarker = false;
                previousTradePrice = Histogram[i].CandleStick.high;
                return true;
            }
            return false;
        }

        public bool Sell(int i)
        {
            if (Histogram[i].Value < 0 && Histogram[i - 1].Value > 0 )
            {
                sellMarker = Histogram[i].CandleStick.high > previousTradePrice * Ratio;
            }
            if (sellMarker && (Histogram[i].Value * 1.015 < Histogram[i - 1].Value))
            {
                sellMarker = false;
                previousTradePrice = Histogram[i].CandleStick.high;
                return true;
            }
            return false;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();
            for (int i = 0; i < Histogram.Count; i++)
            {
                if (i > 0 && Histogram[i] != null && Histogram[i - 1] != null)
                {
                    bool isBuy = false;
                    bool isSell = Sell(i);
                    if (!isSell)
                    {
                        isBuy = Buy(i);
                    }
                    if (isBuy || isSell)
                    {
                        tradeItems.Add(new TradeItem(Histogram[i].CandleStick.Date, Histogram[i].CandleStick.close, isBuy ? TradeType.Buy : TradeType.Sell));
                    }
                }
            }
            return tradeItems;
        }
    }
}
