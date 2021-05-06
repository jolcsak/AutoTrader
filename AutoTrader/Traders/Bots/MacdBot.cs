using AutoTrader.Indicators;
using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : ITradingBot
    {
        protected TradingBotManager tradeManager { get; set; }

        public List<MacdLineValue> Line => tradeManager.MacdProvider.Result.Line;
        public List<MacdHistogramValue> Histogram => tradeManager.MacdProvider.Result.Histogram;
        public List<EmaValue> Signal => tradeManager.MacdProvider.Result.Signal;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        private bool isBuyMarker = false;
        private bool isSellMarker = false;

        public MacdBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            //return Line[i - 1].Value < Signal[i - 1].Value && Line[i].Value > Signal[i].Value;
            if (isBuyMarker && Histogram[i].Value < 0 && Histogram[i - 1].Value < Histogram[i].Value)
            {
                isBuyMarker = false;
                return true;
            }
            isBuyMarker = Histogram[i - 1].Value >= Histogram[i].Value;
            return false;
        }

        public bool Sell(int i)
        {
            //return Line[i - 1].Value > Signal[i - 1].Value && Line[i].Value < Signal[i].Value;
            if (isSellMarker && Histogram[i].Value > 0 && Histogram[i - 1].Value > Histogram[i].Value)
            {
                isSellMarker = false;
                return true;
            }
            isSellMarker = Histogram[i - 1].Value <= Histogram[i].Value;
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
