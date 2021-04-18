using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTrader.GraphProviders
{
    public class AoProvider
    {
        public static double Ratio { get; set; } = 10;

        private int slowPeriod;
        private int fastPeriod;
        private ObservableCollection<double> data;

        double previousMa = -1;

        private SmaProvider slowSmaProvider;
        private SmaProvider fastSmaProvider;

        public IList<AoValue> Ao { get; } = new List<AoValue>();

        private int AoIndex => Ao.Count - 1;
        private int DataIndex => data.Count - 1;

        public bool HasChanged { get; private set; } = false;

        public AoValue Current => Ao.Any() ? Ao.Last() : null;

        public AoProvider(int slowPeriod = 34, int fastPeriod = 5)
        {
            this.slowPeriod = slowPeriod;
            this.fastPeriod = fastPeriod;
        }

        public void SetData(ObservableCollection<double> data)
        {
            this.data = data;
            RefreshAll();
        }

        public void RefreshAll()
        {
            slowSmaProvider = new SmaProvider(slowPeriod);
            fastSmaProvider = new SmaProvider(fastPeriod);
            slowSmaProvider.SetData(data);
            fastSmaProvider.SetData(data);

            slowSmaProvider.Sma.CollectionChanged += SmaChanged;
            fastSmaProvider.Sma.CollectionChanged += SmaChanged;

            Calculate();
        }

        public void Calculate()
        {
            Ao.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                double slowMa = slowSmaProvider.Sma[i];
                double fastMa = fastSmaProvider.Sma[i];
                var ma = fastMa - slowMa;
                if (fastMa > -1 && slowMa > -1)
                {
                    Ao.Add(CreateAo(ma));
                }
                previousMa = ma;
            }
        }

        public bool IsBuy()
        {
            int i = AoIndex;
            if (i >= 2)
            {
                bool buy = Ao[i - 1]?.Value < 0 && Ao[i].Value > 0;
                buy |= Ao[i].Value > 0 && Ao[i - 2].Color == AoColor.Green && Ao[i - 1].Color == AoColor.Red && Ao[i].Color == AoColor.Green && Ao[i].Value > Ao[i - 1].Value * Ratio;
                buy |= Ao[i].Value < 0 && Ao[i].Value > Ao[i - 1].Value * Ratio && Ao[i - 1].Color == AoColor.Red && Ao[i].Color == AoColor.Green;
                return buy;
            }
            return false;
        }

        public bool IsSell()
        {
            int i = AoIndex;
            if (i >= 2)
            {
                bool sell = Ao[i - 1]?.Value > 0 && Ao[i].Value < 0;
                sell |= Ao[i].Value < 0 && Ao[i - 2].Color == AoColor.Red && Ao[i - 1].Color == AoColor.Green && Ao[i].Color == AoColor.Red && Ao[i].Value < Ao[i - 1].Value * Ratio;
                sell |= Ao[i].Value > 0 && Ao[i].Value < Ao[i - 1].Value * Ratio && Ao[i - 1].Color == AoColor.Green && Ao[i].Color == AoColor.Red;
                return sell;
            }
            return false;
        }

        public void Refresh()
        {
            int i = DataIndex;
            if (i >= 2 && slowSmaProvider.Sma.Count == fastSmaProvider.Sma.Count)
            {
                double slowMa = slowSmaProvider.Current;
                double fastMa = fastSmaProvider.Current;
                var ma = fastMa - slowMa;

                if (fastMa > -1 && slowMa > -1)
                {
                    Ao.Add(CreateAo(ma));
                }
                HasChanged = true;
            }

        }

        private void SmaChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Refresh();
        }

        private AoValue CreateAo(double ma)
        {
            return new AoValue { Value = ma, Color = previousMa > ma ? AoColor.Red : AoColor.Green, Buy = IsBuy(), Sell = IsSell() };
        }
    }
}
