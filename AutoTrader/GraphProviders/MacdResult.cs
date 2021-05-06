using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class MacdResult
    {
        public List<MacdLineValue> Line { get; set; } = new List<MacdLineValue>();
        public List<MacdHistogramValue> Histogram { get; set; } = new List<MacdHistogramValue>();
        public List<EmaValue> Signal { get; set; } = new List<EmaValue>();
    }
}
