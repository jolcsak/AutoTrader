using AutoTrader.Log;
using System;

namespace AutoTrader.Exporter
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            new TraderThread().GetCollectorThread().Start();
        }
    }
}
