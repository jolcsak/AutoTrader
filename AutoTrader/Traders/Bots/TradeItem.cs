using System;


namespace AutoTrader.Traders.Bots
{
    public enum TradeType
    {
        Buy,
        Sell
    }

    public class TradeItem
    {
        public DateTime Date { get; set; }
        public double Price { get; set; }
        public TradeType Type { get; set; }

        public TradeItem(DateTime date, double price, TradeType type)
        {
            Date = date;
            Price = price;
            Type = type;
        }
    }
}
