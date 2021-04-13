
using System;

namespace AutoTrader.GraphProviders
{
    public enum AoColor
    {
        Green, Red
    }

    public class AoValue
    {
        public double Value { get; set; }
        public AoColor Color { get; set; }
        public bool Buy { get; set; }
        public bool Sell { get; set; }
        public DateTime Date { get; set; }
    }
}
