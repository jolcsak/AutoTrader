namespace AutoTrader.Api
{
    public class Balance
    {
        public bool active { get; set; }
        public string currency { get; set; }
        public double totalBalance { get; set; }
        public double available { get; set; }
        public string pending { get; set; }
        public double btcRate { get; set; }
    }
}
