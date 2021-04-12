using System;
using System.Collections.Generic;

namespace AutoTrader.Api
{
    public class ServerTime
    {
        public string serverTime { get; set; }
    }
    public class Currencies
    {
        public List<Currency> currencies { get; set; }
    }
    public class Currency
    {
        public String currency { get; set; }
        public double available { get; set; }
    }
    public class Symbols
    {
        public List<Symbol> symbols { get; set; }
    }
    public class Symbol
    {
        public String baseAsset { get; set; }
    }
    public class Pool
    {
        public string id { get; set; }
    }
    public class Order
    {
        public string id { get; set; }
    }
    public class OrderBooks
    {
        public List<double[]> sell { get; set; }
        public List<double[]> buy { get; set; }
    }
}
