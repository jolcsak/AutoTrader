
namespace AutoTrader.GraphProviders
{
    public class RsiValue
    {
        public RsiValue(double value)
        {
            Value = value;
        }

        public RsiValue(double value, bool isBuy, bool isSell)
        {
            Value = value;
            IsBuy = isBuy;
            IsSell = isSell;
        }

        public double Value { get; set; }
        public bool IsBuy { get; set; }
        public bool IsSell { get; set; }
    }
}
