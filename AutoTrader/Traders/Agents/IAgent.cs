using System;
using System.Collections.Generic;

namespace AutoTrader.Traders.Agents
{
    public interface IAgent
    {
        bool IsBuy { get; }
        bool IsSell { get; }
        bool Buy(int i);
        bool Sell(int i);
        List<TradeItem> RefreshAll();
    }
}
