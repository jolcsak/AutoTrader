using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AutoTrader.Traders
{
    public interface ITrader
    {
        string TraderId { get; }

        string TargetCurrency { get; }

        public IList<TradeOrder> TradeOrders { get; }

        public IList<TradeOrder> AllTradeOrders { get; }

        public ObservableCollection<double> PastPrices { get; }

        public IList<double> Sma { get; }

        public IList<AoValue> Ao { get; }

        public int SmaSkip { get; set; } 

        public int PastPricesSkip { get; set; }

        public void Trade();

        ActualPrice GetAndStoreCurrentOrders();
    }
}
