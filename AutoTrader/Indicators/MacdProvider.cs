using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api.Objects;

namespace AutoTrader.Indicators
{
    public class MacdProvider
    {
        protected int Fast = 12;
        protected int Slow = 26;
        protected int Signal = 15;

        protected bool Percent = false;

        protected IList<CandleStick> Data { get; set; }

        public MacdResult Result { get; set; }

        public MacdProvider(IList<CandleStick> data, int fast, int slow, int signal)
        {
            Fast = fast;
            Slow = slow;
            Signal = signal;
            Data = data.Select(d =>d.Clone()).ToList();
            Calculate();
        }

        public MacdResult Calculate()
        {
            Result = new MacdResult();

            IList<EmaValue> fastEmaValues = new EmaProvider(Data, Fast, false).Calculate();
            IList<EmaValue> slowEmaValues = new EmaProvider(Data, Slow, false).Calculate();

            for (int i = 0; i < Data.Count; i++)
            {
                // MACD Line
                if (fastEmaValues[i] != null && slowEmaValues[i] != null)
                {
                    if (!Percent)
                    {
                        Result.Line.Add(new MacdLineValue(fastEmaValues[i].Value - slowEmaValues[i].Value, Data[i]));
                    }
                    else
                    {
                        // macd <- 100 * ( mavg.fast / mavg.slow - 1 )
                        Result.Line.Add(new MacdLineValue(100 * ((fastEmaValues[i].Value / slowEmaValues[i].Value) - 1), Data[i]));
                    }
                    Data[i].temp_close = Result.Line[i].Value;
                }
                else
                {
                    Result.Line.Add(null);
                    Data[i].temp_close = 0.0;
                }
            }

            int zeroCount = Result.Line.Where(x => x == null).Count();
            IList<EmaValue> signalEmaValues = new EmaProvider(Data.Skip(zeroCount).ToList(), Signal, false).Calculate();

            for (int i = 0; i < zeroCount; i++)
            {
                signalEmaValues.Insert(0, null);
            }

            // Fill Signal and MACD Histogram lists
            for (int i = 0; i < signalEmaValues.Count; i++)
            {
                Result.Signal.Add(signalEmaValues[i]);
                var line = Result.Line[i];
                if (line != null && Result.Signal[i] != null)
                {
                    Result.Histogram.Add(new MacdHistogramValue(line.Value - Result.Signal[i].Value, line.CandleStick));
                }
                else
                {
                    Result.Histogram.Add(null);
                }
            }

            return Result;
        }
    }
}
