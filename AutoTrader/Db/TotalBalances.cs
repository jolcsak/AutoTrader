using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Db.Entities;

namespace AutoTrader.Db
{

    public class TotalBalances : AutoTraderStore<TotalBalance, TotalBalances>
    {
        public TotalBalances() : base()
        {
        }

        public IList<TotalBalance> GetTotalBalances()
        {
            var balances = Table.OrderBy(R.Desc("Date")).Limit(RECORD_LIMIT).RunResult<IList<TotalBalance>>(conn);
            return balances.Reverse().ToList();
        }
    }

}
