using System;

namespace AutoTrader.Traders.Agents
{
    public interface IAgent
    {
        bool IsBuy(int i = -1);
        bool IsSell(int i = -1);
        void Refresh(double? actualPrice, DateTime? date);

        void RefreshAll();
    }
}
