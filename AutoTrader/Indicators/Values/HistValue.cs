namespace AutoTrader.Indicators
{
    public enum AoColor
    {
        Green, Red
    }

    public class HistValue : ValueBase
    {
        public AoColor Color { get; set; }
    }
}
