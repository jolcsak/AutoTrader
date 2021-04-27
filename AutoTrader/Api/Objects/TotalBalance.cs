using System.Collections.Generic;

namespace AutoTrader.Api.Objects
{
    public class PendingDetails
    {
        public string deposit { get; set; }
        public string withdrawal { get; set; }
        public string exchange { get; set; }
        public string hashpowerOrders { get; set; }
        public string unpaidMining { get; set; }
    }

    public class Total
    {
        public string currency { get; set; }
        public double totalBalance { get; set; }
        public double available { get; set; }
        public double pending { get; set; }
        public PendingDetails pendingDetails { get; set; }
    }

    public class Currency
    {
        public bool active { get; set; }
        public string currency { get; set; }
        public double totalBalance { get; set; }
        public double available { get; set; }
        public double pending { get; set; }
        public PendingDetails pendingDetails { get; set; }
        public double btcRate { get; set; }
        public double fiatRate { get; set; }
        public bool? enabled { get; set; }
    }

    public class TotalBalance
    {
        public Total total { get; set; }
        public List<Currency> currencies { get; set; }
    }
}
