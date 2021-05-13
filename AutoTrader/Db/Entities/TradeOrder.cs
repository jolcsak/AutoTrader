using AutoTrader.Traders.Bots;
using RethinkDb.Driver.Extras.Dao;
using System;
using System.ComponentModel;

namespace AutoTrader.Db.Entities
{
    public class TradeOrder : Document<Guid>, INotifyPropertyChanged
    {
        public string OrderId { get; set; }
        public double Amount { get; set; }
        public double TargetAmount { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Fee { get; set; }
        public double SellPrice { get; set; }
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }
        public TradeOrderType Type { get; set; }
        public double ActualPrice { get; set; }

        public double SellBtcAmount { get; set; }

        public TradePeriod Period {get; set;}
        public double ActualYield => ActualPrice > 0 ? ((ActualPrice / Price) * 100) - 100 : 0;

        public double Yield => Price > 0 ? ((SellPrice / Price) * 100) - 100 : 0;

        public string Trader { get; set; }

        public TradeOrder()
        {
        }

        public TradeOrder(string orderId, double price, double amount, double targetAmount, string currency, double fee, string trader, TradeOrderType orderType, TradePeriod period) : base()
        {
            OrderId = orderId;
            BuyDate = DateTime.Now;
            Price = price;
            Amount = amount;
            TargetAmount = targetAmount;
            Currency = currency;
            Fee = fee;
            Trader = trader;
            Type = orderType;
            ActualPrice = price;
            Period = period;
        }

        public TradeOrder(string orderId, double price, double amount, double targetAmount, string currency, double fee, string trader, TradePeriod period) : this(orderId, price, amount, targetAmount, currency, fee, trader, TradeOrderType.OPEN, period)
        {
        }

        public void RefreshFrom(TradeOrder tradeOrder)
        {
            if (tradeOrder.ActualPrice != ActualPrice)
            {
                ActualPrice = tradeOrder.ActualPrice;
                NotifyPropertyChanged(nameof(ActualPrice));
                NotifyPropertyChanged(nameof(ActualYield));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public override string ToString()
        {
            return $"TradeOrder: OrderId={OrderId}, Currency={Currency}, Buy Price={Price}, Buy Amount={Amount}, TargetAmount={TargetAmount}, Price={Price}, SellPrice={SellPrice}, Type={Type}, Period={Period}";
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
