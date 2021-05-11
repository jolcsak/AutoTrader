using AutoTrader.Db.Entities;
using System;
using System.Collections.Generic;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        double Order { get; }
        DateTime LastPriceDate { get; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public TradingBotManager BotManager { get; }

        public void Trade(bool canBuy);

        ActualPrice GetAndStoreCurrentOrders();

        IList<Price> GetAllPastPrices();

        bool Buy(double amount, double actualPrice, double actualAmount);

        bool Sell(double actualPrice, TradeOrder tradeOrder);

        public void SellAll(bool onlyProfitable);

        double RefreshBalance();
    }
}
