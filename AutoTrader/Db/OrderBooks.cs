using AutoTrader.Db.Entities;
using AutoTrader.Traders;
using System.Collections.Generic;

namespace AutoTrader.Db
{
    public class OrderBooks : AutoTraderStore<TradeOrder, OrderBooks>
    {
        public OrderBooks() : base()
        {
        }

        public IList<TradeOrder> GetOrdersForTrader(ITrader trader)
        {
            var r = Table.
                Filter(doc => 
                R.Or(doc["Trader"].Eq(trader.TraderId)
                .And(doc["Currency"].Eq(trader.TargetCurrency))))
                .RunCursor<TradeOrder>(conn);

            var ret = new List<TradeOrder>();
            while (r.MoveNext())
            {
                ret.Add(r.Current);
            }
            return ret;
        }

        public IList<TradeOrder> GetAllOrders(ITrader trader)
        {
            var r = Table.Filter(R.HashMap("Trader", trader.TraderId)).RunCursor<TradeOrder>(conn);
            var ret = new List<TradeOrder>();
            while (r.MoveNext())
            {
                ret.Add(r.Current);
            }
            return ret;
        }
    }
}
