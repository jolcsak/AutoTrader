using System;

namespace AutoTrader.Db.Entities
{
    public class Currency
    {
        private double? previousPrice;

        public string Name { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public double MinPeriodPrice { get; set; }
        public double MaxPeriodPrice { get; set; }
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public double Order { get; set; }
        public DateTime LastUpdate { get; set; }

        public string Change
        {
            get
            {
                return previousPrice.HasValue
                    ? previousPrice == Price ? string.Empty : (((previousPrice / Price) * 100) - 100).Value.ToString("N3") + "%"
                    : string.Empty;
            }
        }

        public void Refresh(double price, double amount, double minPeriodPrice, double maxPeriodPrice, double frequency, double amplitude, double order, DateTime lastUpdate)
        {
            previousPrice = Price;
            Price = price;
            Amount = amount;
            MinPeriodPrice = minPeriodPrice;
            MaxPeriodPrice = maxPeriodPrice;
            Frequency = frequency;
            Amplitude = amplitude;
            Order = order;
            LastUpdate = LastUpdate;                                
        }
    }
}
