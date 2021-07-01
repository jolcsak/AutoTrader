using AutoTrader.Api;

namespace AutoTrader.Traders
{
    public class ActualPrice
    {
        public string Currency { get; set; }
        public double SellPrice { get; set; }
        public double BuyPrice { get; set; }
        public double SellAmount { get; set; }
        public double BuyAmount { get; set; }

        public ActualPrice() { }

        public ActualPrice(string targetCurrency, OrderBooks orderBooks)
        {
            Currency = targetCurrency;
            BuyPrice = orderBooks.buy.Count > 0 ? orderBooks.buy[0][0] : 0; // Neki tudok eladni. Nekem SELL
            BuyAmount = orderBooks.buy.Count > 0 ? orderBooks.buy[0][1] : 0;
            SellPrice = orderBooks.sell.Count > 0 ? orderBooks.sell[0][0] : 0; // Tőle tudok venni. Nekem BUY
            SellAmount = orderBooks.sell.Count > 0 ? orderBooks.sell[0][1] : 0;
        }
    }
}
