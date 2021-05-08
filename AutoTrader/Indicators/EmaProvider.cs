using AutoTrader.Api.Objects;
using System.Collections.Generic;

namespace AutoTrader.Indicators
{
    public class EmaProvider
    {
        protected int Period = 10;
        protected bool Wilder = false;

        protected IList<CandleStick> Data { get; set; }

        public IList<EmaValue> Ema { get; } = new List<EmaValue>();

        public EmaProvider(IList<CandleStick> data, int period, bool wilder)
        {
            Period = period;
            Wilder = wilder;
            Data = data;
        }

        /// <summary>
        /// SMA: 10 period sum / 10 
        /// Multiplier: (2 / (Time periods + 1) ) = (2 / (10 + 1) ) = 0.1818 (18.18%)
        /// EMA: {Close - EMA(previous day)} x multiplier + EMA(previous day). 
        /// for Wilder parameter details: http://www.inside-r.org/packages/cran/TTR/docs/GD
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages"/>
        /// <returns></returns>
        public IList<EmaValue> Calculate()
        {
            var multiplier = !Wilder ? (2.0 / (Period + 1)) : (1.0 / Period);

            for (int i = 0; i < Data.Count; i++)
            {
                if (i >= Period - 1)
                {
                    double value = Data[i].temp_close;

                    if (Ema[i - 1] != null)
                    {
                        var emaPrev = Ema[i - 1].Value;
                        var ema = (value - emaPrev) * multiplier + emaPrev;
                        Ema.Add(new EmaValue(ema, Data[i]));
                    }
                    else
                    {
                        double sum = 0;
                        for (int j = i; j >= i - (Period - 1); j--)
                        {
                            sum += Data[j].temp_close;
                        }
                        var ema = sum / Period;
                        Ema.Add(new EmaValue(ema, Data[i]));
                    }
                }
                else
                {
                    Ema.Add(null);
                }
            }
            return Ema;
        }
    }
}
