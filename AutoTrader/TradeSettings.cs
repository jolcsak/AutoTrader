namespace AutoTrader
{
    public class TradeSettings
    {
        public static bool CanBuy { get; set; } = true;

        public static double GameRatio { get; set; } = 1.01;

        public static double BuyRatio { get; set; } = 1.10;

        public static double SellRatio { get; set; } = 0.90;

        public static double MinSellYield { get; set; } = 1.10;
    }
}
