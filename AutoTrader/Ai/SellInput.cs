using Microsoft.ML.Data;
using System.Linq;

namespace AutoTrader.Ai
{
    public class SellInput : TradeInputBase
    {
        [LoadColumn(15)]
        public bool IsSell { get; set; }
    }
}
