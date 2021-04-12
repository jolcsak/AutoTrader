using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class SmaProvider
    {
        public double GetMa(int period, int ii, IList<double> data)
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

        public IList<double> GetSma(IList<double> data, int period = 8)
        {
            IList<double> sma = new List<double>();
            for (int i = 0; i < data.Count; i++)
            {
                double ma = GetMa(period, i, data);
                if (ma > -1)
                {
                    sma.Add(ma);
                }
            }
            return sma;
        }
    }
}
