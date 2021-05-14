using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators
{
    public class RsiValue : TradeValueBase
    {
        public RsiValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
            ShowTrade = false;
        }
    }
}
