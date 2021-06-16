using System;
using System.Collections.Generic;
using AutoTrader.Db.Entities;
using AutoTrader.Traders.Bots;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        double Order { get; }
        DateTime LastPriceDate { get; }

        public ActualPrice ActualPrice { get; set; }

        public ActualPrice PreviousPrice { get; set; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public TradingBotManager BotManager { get; }

        public void Trade(bool canBuy);

        ActualPrice GetAndStoreCurrentOrders();

        IList<Price> GetAllPastPrices();

        bool Buy(double amount, ActualPrice actualPrice, TradePeriod period, string botName);

        bool Sell(double actualPrice, TradeOrder tradeOrder, bool isMarket);

        bool CancelLimit(TradeOrder tradeOrder);

        public void SellAll(bool onlyProfitable);

        double RefreshBalance();
    }
}
