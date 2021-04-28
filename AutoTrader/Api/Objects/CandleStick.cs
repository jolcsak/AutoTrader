namespace AutoTrader.Api.Objects
{
    public class CandleStick
    {
        public int time { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double low { get; set; }
        public double high { get; set; }
        public double volume { get; set; }
        public double quote_volume { get; set; }
        public int count { get; set; }
    }
}
