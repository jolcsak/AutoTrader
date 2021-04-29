
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

        public double Price { get; set; }
        public AoColor Color { get; set; }
        public AoColor InvColor => Color == AoColor.Green ? AoColor.Red : AoColor.Green;
        public bool Buy { get; set; }
        public bool BuyMore { get; set; }
        public bool Sell { get; set; }
        public bool SellMore { get; set; }
        public DateTime Date { get; set; }
        public int SmaIndex { get; set; }
    }
}
