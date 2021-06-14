using Microsoft.ML.Data;

namespace AutoTrader.Ai
{
    public class SellInput : TradeInputBase
    {
        [LoadColumn(14)]
        public bool IsSell { get; set; }
    }
}
