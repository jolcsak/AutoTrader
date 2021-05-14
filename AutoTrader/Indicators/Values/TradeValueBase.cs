

namespace AutoTrader.Indicators
{
    public class TradeValueBase : ValueBase
    {
        public bool IsBuy { get; set; }
        public bool IsSell { get; set; }

        public bool ShowTrade { get; set; } = true;
    }
}
