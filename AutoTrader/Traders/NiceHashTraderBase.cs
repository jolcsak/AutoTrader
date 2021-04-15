using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class NiceHashTraderBase : ITrader
    {
        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        protected static Store Store => Store.Instance;

        protected ObservableCollection<double> pastPrices = null;

        protected IList<double> pp = new List<double>();

        protected IList<double> sma = new List<double>();

        protected SmaProvider smaProvider = new SmaProvider();

        protected AoProvider aoProvider = new AoProvider();

        public string TargetCurrency { get; protected set; }

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());

        public string TraderId => this.GetType().Name;

        public virtual IList<TradeOrder> TradeOrders => Store.OrderBooks.GetOrdersForTrader(this);

        public virtual IList<TradeOrder> AllTradeOrders => Store.OrderBooks.GetAllOrders(this);

        public IList<double> PastPrices => pp;
        public IList<double> Sma => sma;
        public IList<AoValue> Ao => aoProvider.Ao;

        public virtual void Trade()
        {
        }

        public virtual ActualPrice GetandStoreCurrentOrders()
        {
            return null;
        }

        public void StoreTradeOrder(double price, double amount, double fee, string currency)
        {
            Store.OrderBooks.Save(new TradeOrder(price, amount, currency, fee, TraderId));
        }
    }
}
