using System.Collections.Generic;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class NiceHashTraderBase : ITrader
    {
        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        protected static Store Store => Store.Instance;

        public string TargetCurrency { get; protected set; }

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());

        public string TraderId => this.GetType().Name;

        public virtual IList<TradeOrder> TradeOrders => Store.OrderBooks.GetOrdersForTrader(this);

        public virtual IList<TradeOrder> AllTradeOrders => Store.OrderBooks.GetAllOrders(this);

        public GraphCollection GraphCollection { get; }
        public double Frequency => GraphCollection.AoProvider.Frequency;
        public double Amplitude => GraphCollection.AoProvider.Amplitude;

        public double Order => GraphCollection.AoProvider.Frequency * GraphCollection.AoProvider.Amplitude;

        public NiceHashTraderBase()
        {
            GraphCollection = new GraphCollection(this);
        }

        public virtual void Trade()
        {
        }

        public virtual ActualPrice GetAndStoreCurrentOrders()
        {
            return null;
        }

        public void StoreTradeOrder(double price, double amount, double fee, string currency)
        {
            Store.OrderBooks.Save(new TradeOrder(price, amount, currency, fee, TraderId));
        }
    }
}
