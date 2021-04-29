using System;

namespace AutoTrader.Traders.Agents
{
    public interface IAgent
    {
        bool Buy(int i = -1);
        bool Sell(int i = -1);
        void Refresh(double? actualPrice, DateTime? date);

        void RefreshAll();
    }
}
