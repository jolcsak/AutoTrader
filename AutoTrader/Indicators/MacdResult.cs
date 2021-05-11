using System.Collections.Generic;

namespace AutoTrader.Indicators
{
    public class MacdResult
    {
        public List<MacdLineValue> Line { get; set; } = new List<MacdLineValue>();
        public List<HistValue> Histogram { get; set; } = new List<HistValue>();
        public List<EmaValue> Signal { get; set; } = new List<EmaValue>();
    }
}
