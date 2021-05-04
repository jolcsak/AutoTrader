using AutoTrader.Api.Objects;
using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class SmaProvider
    {
        private int period;
        public IList<CandleStick> Data { get; set; }

        public IList<SmaValue> Sma { get; } = new List<SmaValue>();

        public SmaProvider(int period = 8)
        {
            this.period = period;
        }

        public SmaProvider(IList<CandleStick> data, int period = 8) : this(period)
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
                    summ += Data[i].close;
                }
                return summ / period;
            }
            else return -1;
        }

        public void Calculate()
        {
            for (int i = 0; i < Data.Count; i++)
            {
                Sma.Add(new SmaValue(GetMa(i), Data[i]));
            }
        }
    }
}
