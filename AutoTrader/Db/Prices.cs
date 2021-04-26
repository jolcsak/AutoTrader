using AutoTrader.Db.Entities;
using AutoTrader.Traders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Db
{

    public class Prices : AutoTraderStore<Price, Prices>
    {
        public Prices() : base()
        {
        }

        public Price GetLastPriceForTrader(ITrader trader)
        {
            var lastPrice = Table.
                Filter(doc => R.Or(doc["Currency"].Eq(trader.TargetCurrency))).OrderBy(R.Desc("Time")).Limit(1)
                .RunResult<IList<Price>>(conn);
            return lastPrice.FirstOrDefault();
        }

        public IList<Price> GetPricesForTrader(ITrader trader)
        {
            var prices = Table.
                Filter(doc => R.Or(doc["Currency"].Eq(trader.TargetCurrency))).OrderBy(R.Desc("Time")).Limit(750)
                .RunResult<IList<Price>>(conn);
            return prices.Reverse().ToList();
        }

        public void ClearOldPrices()
        {
 //          VirtualPrice virtualPrice = R.Db("PriceStore").Table("Prices").GetAll().RunResult<VirtualPrice>(con);
        }
    }

}
