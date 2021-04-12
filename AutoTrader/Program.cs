using AutoTrader.Log;

namespace AutoTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            new TraderThread().GetTraderThread().Start();
        }
    }
}
