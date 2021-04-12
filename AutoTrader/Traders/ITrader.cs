using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using System.Collections.Generic;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public IList<double> PastPrices { get; }

        public IList<double> Sma { get; }

        public IList<AoValue> Ao { get; }

        public void Trade();

        ActualPrice GetandStoreCurrentOrders();
    }
}
