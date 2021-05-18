using System;
using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public interface ITradingBot
    {
        string Name { get; }

        List<TradeItem> RefreshAll();
    }
}
