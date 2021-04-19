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
            slowSmaProvider = new SmaProvider(slowPeriod);
            fastSmaProvider = new SmaProvider(fastPeriod);
            slowSmaProvider.SetData(data);
            fastSmaProvider.SetData(data);

            slowSmaProvider.Sma.CollectionChanged += SmaChanged;
            fastSmaProvider.Sma.CollectionChanged += SmaChanged;

            Calculate();
        }

        public void RefreshAll()
        {
            int i = 0;
            foreach (var ao in Ao)
            {
                ao.Buy = IsBuy(i, ao.SmaIndex);
                ao.Sell = IsSell(i, ao.SmaIndex);
                i++;
            }
        }

        public void Calculate()
        {
            Ao.Clear();
            int aoIndex = 0;
            for (int i = 0; i < slowSmaProvider.Sma.Count; i++)
            {
                double slowMa = slowSmaProvider.Sma[i];
                double fastMa = fastSmaProvider.Sma[i];
                var ma = fastMa - slowMa;
                if (fastMa > -1 && slowMa > -1)
                {
                    var aoValue = CreateAo(ma, i);
                    Ao.Add(aoValue);
                    aoValue.Buy = IsBuy(aoIndex, aoValue.SmaIndex);
                    aoValue.Sell = IsSell(aoIndex, aoValue.SmaIndex);
                    aoIndex++;
                }
                previousMa = ma;
            }
        }

        public bool IsBuy(int i, int si)
        {
            if (i >= 2)
            {
                bool buy = Ao[i - 1]?.Value < 0 && Ao[i].Value > 0;
                if (!buy)
                {
                    buy |= Ao[i].Value > 0 && Ao[i - 2].Color == AoColor.Green && Ao[i - 1].Color == AoColor.Red && Ao[i].Color == AoColor.Green && fastSmaProvider.Sma[si] > fastSmaProvider.Sma[si - 1] * Ratio;
                }
                if (!buy)
                {
                    buy |= Ao[i].Value < 0 && fastSmaProvider.Sma[si] > fastSmaProvider.Sma[si - 1] * Ratio && Ao[i - 1].Color == AoColor.Red && Ao[i].Color == AoColor.Green;
                }
                return buy;
            }
            return false;
        }

        public bool IsSell(int i, int si)
        {
            if (i >= 2)
            {
                bool sell = Ao[i - 1]?.Value > 0 && Ao[i].Value < 0;
                if (!sell)
                {
                    sell |= Ao[i].Value < 0 && Ao[i - 2].Color == AoColor.Red && Ao[i - 1].Color == AoColor.Green && Ao[i].Color == AoColor.Red && fastSmaProvider.Sma[si] < fastSmaProvider.Sma[si - 1] * Ratio;
                }
                if (!sell)
                {
                    sell |= Ao[i].Value > 0 && fastSmaProvider.Sma[si] < fastSmaProvider.Sma[si - 1] * Ratio && Ao[i - 1].Color == AoColor.Green && Ao[i].Color == AoColor.Red;
                }
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
                    var aoValue = CreateAo(ma, fastSmaProvider.Sma.Count - 1);
                    Ao.Add(aoValue);
                    aoValue.Buy = IsBuy(AoIndex, aoValue.SmaIndex);
                    aoValue.Sell = IsSell(AoIndex, aoValue.SmaIndex);
                }
                HasChanged = true;
            }

        }

        private void SmaChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Refresh();
        }

        private AoValue CreateAo(double ma, int smaIndex)
        {
            return new AoValue { Value = ma, Color = previousMa > ma ? AoColor.Red : AoColor.Green, SmaIndex = smaIndex };
        }
    }
}
