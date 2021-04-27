using System;
using System.Collections.Generic;

namespace AutoTrader.Api
{
    public class ServerTime
    {
        public string serverTime { get; set; }
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

    public class OrderTrade
    {
        public string market { get; set; }
        public string orderId { get; set; }
        public string owner { get; set; }
        public double price { get; set; }
        public double origQty { get; set; }
        public double origSndQty { get; set; }
        public double executedQty { get; set; }
        public double executedSndQty { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public long submitTime { get; set; }
        public long lastResponseTime { get; set; }
        public string state { get; set; }
    }

    public class GetOrderTrades
    {
        public List<GetOrderTrade> orderTrades { get; set; }
    }

    public class GetOrderTrade
    {
        public string id { get; set; }
        public string dir { get; set; }
        public double price { get; set; }
        public double qty { get; set; }
        public double sndQty { get; set; }
        public long time { get; set; }
        public double fee { get; set; }
        public int isMaker { get; set; }
        public string orderId { get; set; }

    }
}
