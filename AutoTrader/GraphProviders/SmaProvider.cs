using System.Collections.ObjectModel;

namespace AutoTrader.GraphProviders
{
    public class SmaProvider
    {
        private int period;
        private ObservableCollection<double> data;

        public ObservableCollection<double> Sma { get; } = new ObservableCollection<double>();

        public double Current { get; private set; } = -1;

        public SmaProvider(int period = 8)
        {
            this.period = period;
        }

        public void SetData(ObservableCollection<double> data)
        {
            this.data = data;
            Calculate();
            this.data.CollectionChanged += DataChanged;
        }

        public double GetMa(int ii)
        {
            if (period != 0 && ii > period)
            {
                double summ = 0;
                for (int i = ii; i > ii - period; i--)
                {
                    summ += data[i];
                }
                return summ / period;
            }
            else return -1;
        }

        public void Calculate()
        {
            Sma.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                Add(GetMa(i));
            }
        }

        public void Refresh()
        {
            Add(GetMa(data.Count - 1));
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
