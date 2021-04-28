﻿using AutoTrader.Db.Entities;
using AutoTrader.Traders.Agents;
using System;
using System.Collections.Generic;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        double Frequency { get; }

        double Amplitude { get; }

        double Order { get; }
        DateTime LastPriceDate { get; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public GraphCollection GraphCollection { get; }

        public IAgent AoAgent { get; set; }

        public void Trade(bool canBuy);

        ActualPrice GetAndStoreCurrentOrders();

        IList<Price> GetAllPastPrices();

        void Sell(double actualPrice, TradeOrder tradeOrder);

        public void SellAll(bool onlyProfitable);
    }
}
