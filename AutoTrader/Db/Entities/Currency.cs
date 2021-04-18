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

        public string Change
        {
            get
            {
                if (previousPrice.HasValue)
                {
                    return previousPrice == Price ? string.Empty : (((previousPrice / Price) * 100) - 100).Value.ToString("N3") + "%";
                }
                return string.Empty;
            }
        }

        public bool Refresh(double price, double amount, double minPeriodPrice, double maxPeriodPrice)
        {
            bool hasChanged = price != Price || amount != Amount || previousPrice != Price || minPeriodPrice != MinPeriodPrice || maxPeriodPrice != MaxPeriodPrice;
            if (hasChanged)
            {
                previousPrice = Price;
                Price = price;
                Amount = amount;
                MinPeriodPrice = minPeriodPrice;
                MaxPeriodPrice = maxPeriodPrice;
            }
            return hasChanged;
        }
    }
}
