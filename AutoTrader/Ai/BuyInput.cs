using Microsoft.ML.Data;

namespace AutoTrader.Ai
{
    public class BuyInput : TradeInputBase
    {
        [LoadColumn(15)]
        public bool IsBuy { get; set; }
    }
}
