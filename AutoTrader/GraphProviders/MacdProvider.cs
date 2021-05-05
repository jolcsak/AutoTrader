using AutoTrader.Api.Objects;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.GraphProviders
{
    public class MacdProvider
    {
        protected int Fast = 12;
        protected int Slow = 26;
        protected int Signal = 9;
        protected bool Percent = false;

        protected IList<CandleStick> Data { get; set; }

        public MacdProvider(IList<CandleStick> data, bool percent)
        {
            Percent = percent;
            Data = data.Select(d => d.Clone()).ToList();
        }

        public MacdProvider(IList<CandleStick> data, int fast, int slow, int signal)
        {
            Fast = fast;
            Slow = slow;
            Signal = signal;
            Data = data.Select(d => d.Clone()).ToList();
        }

        public MacdProvider(IList<CandleStick> data, int fast, int slow, int signal, bool percent)
        {
            Fast = fast;
            Slow = slow;
            Signal = signal;
            Percent = percent;
            Data = data.Select(d => d.Clone()).ToList();
        }

        public MacdResult Calculate()
        {
            MacdResult result = new MacdResult();

            IList<EmaValue> fastEmaValues = new EmaProvider(Data, Fast, false).Calculate();
            IList<EmaValue> slowEmaValues = new EmaProvider(Data, Slow, false).Calculate();

            for (int i = 0; i < Data.Count; i++)
            {
                // MACD Line
                if (fastEmaValues[i] != null && slowEmaValues[i] != null)
                {
                    if (!Percent)
                    {
                        result.Line.Add(fastEmaValues[i].Value - slowEmaValues[i].Value);
                    }
                    else
                    {
                        // macd <- 100 * ( mavg.fast / mavg.slow - 1 )
                        result.Line.Add(100 * ((fastEmaValues[i].Value / slowEmaValues[i].Value) - 1));
                    }
                    Data[i].close = result.Line[i].Value;
                }
                else
                {
                    result.Line.Add(null);
                    Data[i].close = 0.0;
                }
            }

            int zeroCount = result.Line.Where(x => x == null).Count();
            IList<EmaValue> signalEmaValues = new EmaProvider(Data.Skip(zeroCount).ToList(), Signal, false).Calculate();

            for (int i = 0; i < zeroCount; i++)
            {
                signalEmaValues.Insert(0, null);
            }

            // Fill Signal and MACD Histogram lists
            for (int i = 0; i < signalEmaValues.Count; i++)
            {
                result.Signal.Add(signalEmaValues[i]);
                result.Histogram.Add(result.Line[i].Value - result.Signal[i].Value);
            }

            return result;
        }
    }
}
