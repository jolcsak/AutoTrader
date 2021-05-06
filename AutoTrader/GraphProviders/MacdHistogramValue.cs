
using AutoTrader.Api.Objects;

namespace AutoTrader.GraphProviders
{
    public class MacdHistogramValue : ValueBase
    {
        public MacdHistogramValue(double value, CandleStick candleStick)
        {
            Value = value;
            CandleStick = candleStick;
        }
    }
}
