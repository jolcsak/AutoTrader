using Microsoft.ML.Data;

namespace AutoTrader.Ai
{
    public class SellInput : TradeInputBase
    {
        [LoadColumn(12)]
        public bool IsSell { get; set; }
    }
}
