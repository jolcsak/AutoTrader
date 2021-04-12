
namespace AutoTrader.CollectPrices
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            new TraderThread().GetThread().Start();
        }
    }
}
