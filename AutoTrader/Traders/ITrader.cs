using AutoTrader.Db.Entities;
using System.Collections.Generic;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public GraphCollection GraphCollection { get; }

        public void Trade();

        ActualPrice GetAndStoreCurrentOrders();
    }
}
