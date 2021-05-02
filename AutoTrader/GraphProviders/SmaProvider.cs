using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class SmaProvider
    {
        private int period;
        public IList<double> Data { get; set; }

        public IList<double> Sma { get; } = new List<double>();

        public SmaProvider(int period = 8)
        {
            this.period = period;
        }

        public SmaProvider(IList<double> data, int period = 8) : this(period)
        {
            this.Data = data;
            Calculate();
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
            for (int i = 0; i < Data.Count; i++)
            {
                Sma.Add(GetMa(i));
            }
        }
    }
}
