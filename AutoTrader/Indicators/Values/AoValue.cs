namespace AutoTrader.Indicators
{
    public enum AoColor
    {
        Green, Red
    }

    public class AoValue : ValueBase
    {
        public AoColor Color { get; set; }
        public AoColor InvColor => Color == AoColor.Green ? AoColor.Red : AoColor.Green;
        public bool Buy { get; set; }
        public bool BuyMore { get; set; }
        public bool Sell { get; set; }
        public bool SellMore { get; set; }
        public int SmaIndex { get; set; }
    }
}
