using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTrader.GraphProviders
{
    public class AoProvider
    {
        private static int lastAmps = 10;
        private int slowPeriod;
        private int fastPeriod;

        private ObservableCollection<double> data;

        double previousMa = -1;

        public SmaProvider SlowSmaProvider { get; set; }
        public SmaProvider FastSmaProvider { get; set; }

        public IList<AoValue> Ao { get; } = new List<AoValue>();

        public int AoIndex => Ao.Count - 1;
        private int DataIndex => data.Count - 1;

        public AoValue Current => Ao.Any() ? Ao.Last() : null;

        public double Frequency
        {
            get
            {
                int frequency = 0;
                int i = 0;
                foreach (var ao in Ao)
                {
                    if (i > 0 && Math.Sign(Ao[i].Value * Ao[i - 1].Value) < 0)
                    {
                        frequency++;
                    }
                    i++;
                }
                return frequency > 0 ?  (double)frequency / Ao.Count : 0;
            }
        }

        public double Amplitude
        {
            get
            {
                if (Ao.Count > 0)
                {
                    int i = 0;
                    double max = Math.Abs(Ao.Select(ao => ao.Value).Max());
                    double min = Math.Abs(Ao.Select(ao => ao.Value).Min());
                    List<double> amplitudes = new List<double>();
                    foreach (var ao in Ao)
                    {
                        if (i > 0 && Ao[i].Color != Ao[i - 1].Color)
                        {
                            amplitudes.Add(Ao[i].Value >= 0 ? Ao[i].Value / max : Math.Abs(Ao[i].Value) / min);
                        }
                        i++;
                    }
                    return amplitudes.Skip(amplitudes.Count > lastAmps ? amplitudes.Count - lastAmps : 0).Average();
                }
                return 0;
            }
        }

        public AoProvider(int slowPeriod = 34, int fastPeriod = 5)
        {
            this.slowPeriod = slowPeriod;
            this.fastPeriod = fastPeriod;
        }

        public void SetData(ObservableCollection<double> data)
        {
            this.data = data;
            SlowSmaProvider = new SmaProvider(data, SmaChanged, slowPeriod);
            FastSmaProvider = new SmaProvider(data, SmaChanged, fastPeriod);
            Calculate();
        }

        public void Calculate()
        {
            Ao.Clear();
            for (int i = 0; i < SlowSmaProvider.Sma.Count; i++)
            {
                AddAo(SlowSmaProvider.Sma[i], FastSmaProvider.Sma[i], i);
            }
        }

        private void AddAo(double slowMa, double fastMa, int smaIndex)
        {
            var ma = fastMa - slowMa;
            if (fastMa > -1 && slowMa > -1)
            {
                Ao.Add(new AoValue { Value = ma, Color = previousMa > ma ? AoColor.Red : AoColor.Green, SmaIndex = smaIndex });
            }
            previousMa = ma;
        }

        private void SmaChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (DataIndex >= 2 && SlowSmaProvider.Sma.Count == FastSmaProvider.Sma.Count)
            {
                AddAo(SlowSmaProvider.Current, FastSmaProvider.Current, FastSmaProvider.Sma.Count - 1);
            }
        }
    }
}
