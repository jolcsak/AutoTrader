using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators
{
    public class EmaValue : ValueBase
    { 
        public EmaValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }
    }
}
