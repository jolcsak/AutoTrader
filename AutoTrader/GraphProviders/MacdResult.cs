using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class MacdResult
    {
        public List<double?> Line { get; set; } = new List<double?>();
        public List<double?> Histogram { get; set; } = new List<double?>();
        public List<EmaValue> Signal { get; set; } = new List<EmaValue>();
    }
}
