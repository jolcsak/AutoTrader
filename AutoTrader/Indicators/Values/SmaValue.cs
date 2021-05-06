using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators
{
    public class SmaValue : ValueBase
    { 
        public SmaValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }
    }
}
