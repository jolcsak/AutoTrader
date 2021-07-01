using AutoTrader.Log;

namespace AutoTrader.PriceCollector
{    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            new HistoicalPriceCollector().GetCollectorThread().Start();
        }
    }
}
