using AutoTrader.Traders;
using System;
using System.ComponentModel;

namespace AutoTrader.Db.Entities
{
    public class Currency : INotifyPropertyChanged
    {
        private double? previousBuyPrice;
        private double? previousSellPrice;
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double BuyAmount { get; set; }
        public double SellPrice { get; set; }
        public double SellAmount { get; set; }
        public double Order { get; set; }
        public DateTime LastUpdate { get; set; }

        public string BuyChange
        {
            get
            {
                return previousBuyPrice.HasValue
                    ? previousBuyPrice == BuyPrice ? string.Empty : (((previousBuyPrice / BuyPrice) * 100) - 100).Value.ToString("N3") + "%"
                    : string.Empty;
            }
        }
        public string SellChange
        {
            get
            {
                return previousSellPrice.HasValue
                    ? previousSellPrice == SellPrice ? string.Empty : (((previousSellPrice / SellPrice) * 100) - 100).Value.ToString("N3") + "%"
                    : string.Empty;
            }
        }

        public void Refresh(ActualPrice actualPrice, double order, DateTime lastUpdate)
        {
            previousBuyPrice = BuyPrice;
            previousSellPrice = SellPrice;

            if (actualPrice.BuyPrice != BuyPrice)
            {
                BuyPrice = actualPrice.BuyPrice;
                NotifyPropertyChanged(nameof(BuyPrice));
                NotifyPropertyChanged(nameof(BuyChange));
            }

            if (actualPrice.BuyAmount != BuyAmount)
            {
                BuyAmount = actualPrice.BuyAmount;
                NotifyPropertyChanged(nameof(BuyAmount));
            }

            if (actualPrice.SellPrice != SellPrice)
            {
                SellPrice = actualPrice.SellPrice;
                NotifyPropertyChanged(nameof(SellPrice));
                NotifyPropertyChanged(nameof(SellChange));
            }
            if (actualPrice.SellAmount != SellAmount)
            {
                SellAmount = actualPrice.SellAmount;
                NotifyPropertyChanged(nameof(SellAmount));
            }


            if (order != Order)
            {
                Order = order;
                NotifyPropertyChanged(nameof(Order));
            }

            if (lastUpdate != LastUpdate)
            {
                LastUpdate = lastUpdate;
                NotifyPropertyChanged(nameof(LastUpdate));
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
    }
}
