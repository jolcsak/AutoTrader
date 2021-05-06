
using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators
{
    public class RsiValue : TradeValueBase
    {
        public RsiValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }

        public RsiValue(double value, bool isBuy, bool isSell)
        {
            Value = value;
            IsBuy = isBuy;
            IsSell = isSell;
        }
    }
}
