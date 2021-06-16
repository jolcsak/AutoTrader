using System;
using System.ComponentModel;
using AutoTrader.Traders.Bots;
using RethinkDb.Driver.Extras.Dao;

namespace AutoTrader.Db.Entities
{
    public class TradeOrder : Document<Guid>, INotifyPropertyChanged
    {
        public string BuyOrderId { get; set; }

        public string SellOrderId { get; set; }

        public double Amount { get; set; }
        public double TargetAmount { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Fee { get; set; }
        public double SellPrice { get; set; }
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }
        public TradeOrderType Type { get; set; }
        public TradeOrderState State { get; set; }
        public double ActualPrice { get; set; }

        public double SellBtcAmount { get; set; }

        public TradePeriod Period {get; set;}
        public double ActualYield => ActualPrice > 0 ? ((ActualPrice / Price) * 100) - 100 : 0;

        public double Yield => Price > 0 ? ((SellBtcAmount / Amount) * 100) - 100 : 0;

        public bool IsEntered => State == TradeOrderState.ENTERED || State == TradeOrderState.OPEN_ENTERED;

        public string Trader { get; set; }

        public string BotName { get; set; }

        public TradeOrder()
        {
        }

        public TradeOrder(TradeOrderType type, string orderId, double price, double amount, double targetAmount, string currency, double fee, string trader, TradeOrderState state, TradePeriod period, string botName) : base()
        {
            Type = type;
            BuyOrderId = orderId;
            BuyDate = DateTime.Now;
            Price = price;
            Amount = amount;
            TargetAmount = targetAmount;
            Currency = currency;
            Fee = fee;
            Trader = trader;
            State = state;
            ActualPrice = price;
            Period = period;
            BotName = botName;
        }

        public void RefreshFrom(TradeOrder tradeOrder)
        {
            if (tradeOrder.ActualPrice != ActualPrice)
            {
                ActualPrice = tradeOrder.ActualPrice;
                NotifyPropertyChanged(nameof(ActualPrice));
                NotifyPropertyChanged(nameof(ActualYield));
            }
            State = tradeOrder.State;
            NotifyPropertyChanged(nameof(State));
            NotifyPropertyChanged(nameof(IsEntered));
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
            return $"TradeOrder: OrderId={BuyOrderId}, Currency={Currency}, Buy Price={Price}, Buy Amount={Amount}, TargetAmount={TargetAmount}, Price={Price}, SellPrice={SellPrice}, Type={State}, Period={Period}";
        }
    }

    public enum TradeOrderState
    {
        OPEN = 0,
        CLOSED = 1,
        ENTERED = 2,
        OPEN_ENTERED = 3,
        CANCELLED = 4
    }

    public enum TradeOrderType
    {
        MARKET = 0,
        LIMIT = 1
    }
}
