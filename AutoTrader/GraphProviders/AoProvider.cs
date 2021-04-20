using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTrader.GraphProviders
{
    public class AoProvider
    {
        public static double Ratio { get; set; } = 1;
        public static int TrendNumber { get; set; } = 4;

        public static int MinTradePeriod { get; set; } = 10;


        private int slowPeriod;
        private int fastPeriod;
        private ObservableCollection<double> data;

        private int lastTradeIndex = 0;

        double previousMa = -1;

        private SmaProvider slowSmaProvider;
        private SmaProvider fastSmaProvider;

        public IList<AoValue> Ao { get; } = new List<AoValue>();

        private int AoIndex => Ao.Count - 1;
        private int DataIndex => data.Count - 1;

        public bool HasChanged { get; private set; } = false;

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
                return frequency > 0 ? (double)frequency / Ao.Count : 0;
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
                        if (i > 0)
                        {
                            if (Ao[i].Color != Ao[i - 1].Color)
                            {
                                if (Ao[i].Value >= 0)
                                {
                                    amplitudes.Add(Ao[i].Value / max);
                                }
                                else
                                {
                                    amplitudes.Add(Math.Abs(Ao[i].Value) / min);
                                }
                            }
                        }
                        i++;
                    }
                    return amplitudes.Average();
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
            slowSmaProvider = new SmaProvider(slowPeriod);
            fastSmaProvider = new SmaProvider(fastPeriod);
            slowSmaProvider.SetData(data);
            fastSmaProvider.SetData(data);

            slowSmaProvider.Sma.CollectionChanged += SmaChanged;
            fastSmaProvider.Sma.CollectionChanged += SmaChanged;

            Ao.Clear();

            Calculate();
        }

        public void RefreshAll()
        {
            lastTradeIndex = 0;
            int i = 0;
            foreach (var ao in Ao)
            {
                Trade(i, ao);
                i++;
            }
        }

        public void Calculate()
        {
            lastTradeIndex = 0;
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
                    Trade(aoIndex, aoValue);
                    aoIndex++;
                }
                previousMa = ma;
            }
        }

        private void Trade(int i, AoValue aoValue)
        {
            if (i - lastTradeIndex >= MinTradePeriod)
            {
                aoValue.Buy = IsBuy(i, aoValue.SmaIndex);
                aoValue.Sell = IsSell(i, aoValue.SmaIndex);
                if (aoValue.Buy || aoValue.Sell)
                {
                    lastTradeIndex = i;
                }
            }
            else
            {
                aoValue.Buy = false;
                aoValue.Sell = false;
            }
        }

        public bool IsBuy(int i, int si)
        {
            if (i >= 2)
            {
                bool buy = Ao[i - 1].Value < 0 && Ao[i].Value > 0;
                if (!buy)
                {
                    buy |= Ao[i].Value < 0 && Ao[i - 1].Color == AoColor.Red && Ao[i].Color == AoColor.Green && ValueOf(AoColor.Green, i) > Ratio;
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
                    sell |= Ao[i].Value > 0 && Ao[i - 1].Color == AoColor.Green && Ao[i].Color == AoColor.Red && ValueOf(AoColor.Green, i) > Ratio;
                }
                return sell;
            }
            return false;
        }

        private double ValueOf(AoColor color, int i)
        {
            int oi = i;
            i--;
            while (i > 0 && (Math.Sign(Ao[i].Value * Ao[i -1].Value) >= 0 || Ao[i].Color == color))
            {
                i--;
            }

            double oiSma = slowSmaProvider.Sma[Ao[i].SmaIndex];
            double iSma = slowSmaProvider.Sma[Ao[oi].SmaIndex];

            return oiSma > iSma ? oiSma / iSma : iSma / oiSma;
        }

        private bool IsInflexionPoint(int i)
        {
            if (i > 0)
            {
                var smaCurrent = slowSmaProvider.Sma[Ao[i].SmaIndex];
                var smaPrevious = slowSmaProvider.Sma[Ao[i - 1].SmaIndex];
                if (Ao[i].Value >= 0)
                {
                    return smaPrevious <= smaCurrent;
                }
                return smaCurrent >= smaPrevious;
            }
            return false;
        }

        private double NumberOf(AoColor color, int i)
        {
            i--;
            int c = 0;

            while (i > 0 && Ao[i].Color == color)
            {
                i--;
                c++;
            }
            return c;
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
                    Trade(Ao.Count - 1, aoValue);
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
