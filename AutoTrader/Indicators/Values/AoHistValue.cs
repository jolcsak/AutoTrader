namespace AutoTrader.Indicators.Values
{
    public class AoHistValue : HistValue
    {
        public bool Buy { get; set; }
        public bool BuyMore { get; set; }
        public bool Sell { get; set; }
        public bool SellMore { get; set; }
        public int SmaIndex { get; set; }
    }
}
