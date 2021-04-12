using AutoTrader.Db.Entities;
using AutoTrader.Traders;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Db
{
    public class LastPrices : AutoTraderStore<LastPrice, LastPrices>
    {
        public LastPrices() : base()
        {
        }

        public LastPrice GetLastPriceForTrader(ITrader trader)
        {
            var lastPrice = Table.
                Filter(doc => R.Or(doc["Currency"].Eq(trader.TargetCurrency))).Limit(1)
                .RunResult<IList<LastPrice>>(conn);
            return lastPrice.FirstOrDefault();
        }
    }
}
