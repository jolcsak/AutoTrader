using Microsoft.ML.Data;

namespace AutoTrader.Ai
{
    public class SellInput : TradeInputBase
    {
        [LoadColumn(11)]
        public bool IsSell { get; set; }
    }
}
