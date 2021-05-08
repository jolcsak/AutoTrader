﻿using AutoTrader.Indicators;
using System;
using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : ITradingBot
    {
        public static double Ratio { get; set; } = 1.03;
        public static double SmallRatio { get; set; } = 1.01;

        protected TradingBotManager tradeManager { get; set; }

        public List<MacdLineValue> Line => tradeManager.SlowMacdProvider.Result.Line;
        public List<MacdHistogramValue> Histogram => tradeManager.FastMacdProvider.Result.Histogram;
        public List<EmaValue> Signal => tradeManager.SlowMacdProvider.Result.Signal;

        public bool IsBuy { get; }
        public bool IsSell { get; }

        private double previousTradePrice = 0;
        private double previousFlexPrice = 0;

        private bool buyMarker = false;
        private bool sellMarker = false;

        public MacdBot(TradingBotManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public bool Buy(int i)
        {
            return Histogram.IsFlex(i) > 0 && Signal.IsTrend(i) > 1;
            //if (buy)
            //{
            //    double buyPrice = Histogram[i].CandleStick.close;
            //    buy = buyPrice  * Ratio < previousTradePrice;
            //    previousFlexPrice = buyPrice;
            //    if (buy)
            //    {
            //        previousTradePrice = previousFlexPrice;
            //    }
            //}
            //return buy;
        }

        public bool Sell(int i)
        {
            return Histogram.IsFlex(i) < 0 && Signal.IsTrend(i) > 1;
            //if (sell)
            //{
            //    double sellPrice = Histogram[i].CandleStick.close;
            //    sell = sellPrice > previousTradePrice * Ratio;
            //    previousFlexPrice = sellPrice;
            //    if (sell)
            //    {
            //        previousTradePrice = previousFlexPrice;
            //    }
            //}
            //return sell;
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
