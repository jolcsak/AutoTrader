using System;
using System.ComponentModel;

namespace AutoTrader.Db.Entities
{
    public class Currency : INotifyPropertyChanged
    {
        private double? previousPrice;
        public string Name { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public double Order { get; set; }
        public DateTime LastUpdate { get; set; }

        public string Change
        {
            get
            {
                return previousPrice.HasValue
                    ? previousPrice == Price ? string.Empty : (((previousPrice / Price) * 100) - 100).Value.ToString("N3") + "%"
                    : string.Empty;
            }
        }

        public void Refresh(double price, double amount, double order, DateTime lastUpdate)
        {
            previousPrice = Price;
            if (price != Price)
            {
                Price = price;
                NotifyPropertyChanged(nameof(Price));
                NotifyPropertyChanged(nameof(Change));
            }
            if (amount != Amount)
            {
                Amount = amount;
                NotifyPropertyChanged(nameof(Amount));
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
