using System;

namespace AutoTrader.Traders.Agents
{
    public interface IAgent
    {
        bool IsBuy { get; }
        bool IsSell { get; }
        void Buy(int i);
        void Sell(int i);
        void RefreshAll();
    }
}
