namespace AutoTrader.Api.Objects
{
    public class HistoricPrice
    {
        public string id { get; set; }
        public string dir { get; set; }
        public double price { get; set; }
        public double qty { get; set; }
        public double sndQty { get; set; }
        public object time { get; set; }
    }
}
