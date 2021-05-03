using System;

namespace AutoTrader.Traders.Agents
{
    public interface IAgent
    {
        bool IsBuy { get; }
        bool IsSell { get; }
        void Buy(string currency, int i);
        void Sell(int i);
        void RefreshAll(string currency);
    }
}
