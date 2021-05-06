using AutoTrader.Api.Objects;

namespace AutoTrader.GraphProviders
{
    public class MacdLineValue : ValueBase
    {
        public MacdLineValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }
    }
}
