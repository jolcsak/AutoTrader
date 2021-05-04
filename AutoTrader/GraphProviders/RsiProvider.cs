using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    /// <summary>
    /// Relative Strength Index (RSI)
    /// </summary>
    public class RsiProvider
    {
        public IList<double> Data { get; private set; } = new List<double>();
        public IList<RsiValue> Rsi { get; private set; } = new List<RsiValue>();

        public int  Period { get; private set; }

        public RsiProvider(IList<double> data, int period)
        {
            Data = data;            
            Period = period;
            Calculate();
        }

        public void Calculate()
        {
            
            double gainSum = 0;
            double lossSum = 0;
            for (int i = 1; i < Period; i++)
            {
                double thisChange = Data[i] - Data[i - 1];
                if (thisChange > 0)
                {
                    gainSum += thisChange;
                }
                else
                {
                    lossSum += (-1) * thisChange;
                }
            }

            var averageGain = gainSum / Period;
            var averageLoss = lossSum / Period;
            var rs = averageGain / averageLoss;
            var rsi = 100 - (100 / (1 + rs));
            Rsi.Add(new RsiValue(rsi));

            for (int i = Period + 1; i < Data.Count; i++)
            {
                double thisChange = Data[i]- Data[i - 1];
                if (thisChange > 0)
                {
                    averageGain = (averageGain * (Period - 1) + thisChange) / Period;
                    averageLoss = (averageLoss * (Period - 1)) / Period;
                }
                else
                {
                    averageGain = (averageGain * (Period - 1)) / Period;
                    averageLoss = (averageLoss * (Period - 1) + (-1) * thisChange) / Period;
                }
                rs = averageGain / averageLoss;
                rsi = 100 - (100 / (1 + rs));
                Rsi.Add(new RsiValue(rsi));
            }
        }
    }
}
