﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Db.Entities;
using AutoTrader.Traders;

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

        public IList<Price> GetPricesForTrader(ITrader trader, int limit = RECORD_LIMIT)
        {
            var prices = Table.
                Filter(doc => R.And(doc["Currency"].Eq(trader.TargetCurrency))).OrderBy(R.Desc("Time")).Limit(limit)
                .RunResult<IList<Price>>(conn);
            return prices.Reverse().ToList();
        }

        public void ClearOldPrices()
        {
 //          VirtualPrice virtualPrice = R.Db("PriceStore").Table("Prices").GetAll().RunResult<VirtualPrice>(con);
        }
    }

}
