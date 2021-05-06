using System;
using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public interface ITradingBot
    {
        bool IsBuy { get; }
        bool IsSell { get; }
        bool Buy(int i);
        bool Sell(int i);
        List<TradeItem> RefreshAll();
    }
}
