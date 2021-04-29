using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Db
{
    public class TradeSettings : AutoTraderStore<TradeSetting, TradeSettings>
    {
        public TradeSetting GetTradeSettings()
        {
            var tradeSettings = Table.Limit(1).RunResult<IList<TradeSetting>>(conn);
            return tradeSettings.FirstOrDefault() ?? new TradeSetting();
        }
    }
}
