using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AutoTrader.GraphProviders
{
    public class SmaProvider
    {
        private int period;
        public ObservableCollection<double> Data { get; set; }

        public ObservableCollection<double> Sma { get; } = new ObservableCollection<double>();

        public double Current { get; private set; } = -1;

        public SmaProvider(int period = 8)
        {
            this.period = period;
        }

        public SmaProvider(ObservableCollection<double> data, NotifyCollectionChangedEventHandler eventHandler, int period = 8) : this(period)
        {
            SetData(data);
            Sma.CollectionChanged += eventHandler;
        }

        public void SetData(ObservableCollection<double> data)
        {
            this.Data = data;
            Calculate();
            this.Data.CollectionChanged += DataChanged;
        }

        public double GetMa(int ii)
        {
            if (period != 0 && ii > period)
            {
                double summ = 0;
                for (int i = ii; i > ii - period; i--)
                {
                    summ += Data[i];
                }
                return summ / period;
            }
            else return -1;
        }

        public void Calculate()
        {
            Sma.Clear();
            Current = -1;
            for (int i = 0; i < Data.Count; i++)
            {
                Add(GetMa(i));
            }
        }

        public void Refresh()
        {
            Add(GetMa(Data.Count - 1));
        }

        private void Add(double ma)
        {
            Current = ma;
            Sma.Add(ma);
        }

        private void DataChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Refresh();
        }
    }
}
