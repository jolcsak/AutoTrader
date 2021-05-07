using AutoTrader.Api.Objects;
using System.Collections.Generic;

namespace AutoTrader.Indicators
{
    /// <summary>
    /// Relative Strength Index (RSI)
    /// </summary>
    public class RsiProvider
    {
        public IList<CandleStick> Data { get; private set; }
        public IList<RsiValue> Rsi { get; private set; } = new List<RsiValue>();

        public int  Period { get; private set; }

        public RsiProvider(IList<CandleStick> data, int period)
        {
            Data = data;            
            Period = period;
            Calculate();
        }

        public void Calculate()
        {
            if (Data.Count < 1)
            {
                return;
            }
            double gainSum = 0;
            double lossSum = 0;
            for (int i = 1; i < Period; i++)
            {
                double thisChange = Data[i].close - Data[i - 1].close;
                if (thisChange > 0)
                {
                    gainSum += thisChange;
                }
                else
                {
                    lossSum += -thisChange;
                }
            }

            var averageGain = gainSum / Period;
            var averageLoss = lossSum / Period;
            var rs = averageGain / averageLoss;
            var rsi = 100 - (100 / (1 + rs));
            Rsi.Add(new RsiValue(rsi, Data[Period]));

            for (int i = Period + 1; i < Data.Count; i++)
            {
                double thisChange = Data[i].close- Data[i - 1].close;
                if (thisChange > 0)
                {
                    averageGain = (averageGain * (Period - 1) + thisChange) / Period;
                    averageLoss = (averageLoss * (Period - 1)) / Period;
                }
                else
                {
                    averageGain = (averageGain * (Period - 1)) / Period;
                    averageLoss = (averageLoss * (Period - 1) - thisChange) / Period;
                }
                rs = averageGain / averageLoss;
                rsi = 100 - (100 / (1 + rs));
                Rsi.Add(new RsiValue(rsi, Data[i]));
            }
        }
    }
}
