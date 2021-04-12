using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public class AoProvider
    {
        private const double ratio = 10;

        public IList<AoValue> GetAo(IList<double> data, int slowPeriod = 34, int fastPeriod = 5)
        {
            var smaProvider = new SmaProvider();

            IList<AoValue> ao = new List<AoValue>();

            double previousMa = -1;

            for (int i = 0; i < data.Count; i++)
            {
                double slowMa = smaProvider.GetMa(slowPeriod, i, data);
                double fastMa = smaProvider.GetMa(fastPeriod, i, data);
                var ma = fastMa - slowMa;
                if (slowMa > -1 && fastMa > -1)
                {
                    var aoValue = new AoValue { Value = fastMa - slowMa, Color = previousMa > ma ? AoColor.Red : AoColor.Green };
                    ao.Add(aoValue);
                    aoValue.Buy = IsBuy(ao);
                    aoValue.Sell = IsSell(ao);
                }
                previousMa = ma;
            }
            return ao;
        }

        public bool IsBuy(IList<AoValue> ao)
        {
            int i = ao.Count - 1;
            if (i >= 2)
            {
                bool buy = ao[i - 1]?.Value < 0 && ao[i].Value > 0;
                buy |= ao[i].Value > 0 && ao[i - 2].Color == AoColor.Green && ao[i - 1].Color == AoColor.Red && ao[i].Color == AoColor.Green && ao[i].Value > ao[i - 1].Value * ratio;
                buy |= ao[i].Value < 0 && ao[i].Value > ao[i - 1].Value * ratio && ao[i - 1].Color == AoColor.Red && ao[i].Color == AoColor.Green;
                return buy;
            }
            return false;
        }

        public bool IsSell(IList<AoValue> ao)
        {
            int i = ao.Count - 1;
            if (i >= 2)
            {
                bool sell = ao[i - 1]?.Value > 0 && ao[i].Value < 0;
                sell |= ao[i].Value < 0 && ao[i - 2].Color == AoColor.Red && ao[i - 1].Color == AoColor.Green && ao[i].Color == AoColor.Red && ao[i].Value < ao[i - 1].Value * ratio;
                sell |= ao[i].Value > 0 && ao[i].Value < ao[i - 1].Value * ratio && ao[i - 1].Color == AoColor.Green && ao[i].Color == AoColor.Red;
                return sell;
            }
            return false;
        }

    }
}
