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

        public string Bot { get; set; }

        public TradeItem(DateTime date, double price, TradeType type, string bot)
        {
            Date = date;
            Price = price;
            Type = type;
            Bot = bot;
        }

        public override string ToString()
        {
            return $"TradeItem - Bot: {Bot}, Type: {Type}, Date: {Date}, Price: {Price:N6}";
        }
    }
}
