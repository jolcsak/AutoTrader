using AutoTrader.Api.Objects;

namespace AutoTrader.GraphProviders
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
