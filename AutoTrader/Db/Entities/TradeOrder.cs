﻿using RethinkDb.Driver.Extras.Dao;
using System;

namespace AutoTrader.Db.Entities
{
    public class TradeOrder : Document<Guid>
    {
        public double Amount { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Fee { get; set; }
        public double SellPrice { get; set; }
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }
        public TradeOrderType Type { get; set; }
        public double ActualPrice { get; set; }
        public double ActualYield => ActualPrice > 0 ? ((ActualPrice / Price) * 100) - 100 : 0;


        public double Yield => Price > 0 ? ((SellPrice / Price) * 100) - 100 : 0;

        public string Trader { get; set; }

        public TradeOrder()
        {
        }

        public TradeOrder(double price, double amount, string currency, double fee, string trader, TradeOrderType orderType) : base()
        {
            BuyDate = DateTime.Now;
            Price = price;
            Amount = amount;
            Currency = currency;
            Fee = fee;
            Trader = trader;
            Type = orderType;
        }

        public TradeOrder(double price, double amount, string currency, double fee, string trader) : this(price, amount, currency, fee, trader, TradeOrderType.OPEN)
        {
        }
    }

    public enum TradeOrderType
    {
        OPEN = 0,
        CLOSED = 1,
        BUY = 2,
        SELL = 3
    }
}