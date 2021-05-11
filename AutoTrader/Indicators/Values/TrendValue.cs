using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators.Values
{
    public class TrendValue : ValueBase
    {
        public TrendValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }
    }
}
