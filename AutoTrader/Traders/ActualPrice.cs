﻿using AutoTrader.Api;

namespace AutoTrader.Traders
{
    public class ActualPrice
    {
        public string Currency { get; set; }
        public double SellPrice { get; set; }
        public double BuyPrice { get; set; }
        public double SellAmount { get; set; }
        public double BuyAmount { get; set; }

        public ActualPrice(string targetCurrency, OrderBooks orderBooks)
        {
            Currency = targetCurrency;
            BuyPrice = orderBooks.buy[0][0];
            BuyAmount = orderBooks.buy[0][1];
            SellPrice = orderBooks.sell[0][0];
            SellAmount = orderBooks.sell[0][1];
        }
    }
}
