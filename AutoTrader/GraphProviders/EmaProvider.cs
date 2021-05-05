using AutoTrader.Api.Objects;
using System.Collections.Generic;

namespace AutoTrader.GraphProviders
{
    public enum ColumnType
    {
        Open,
        High,
        Low,
        Close,
        Volume,
        AdjClose
    }

    public class EmaProvider
    {
        protected int Period = 10;
        protected bool Wilder = false;
        protected ColumnType ColumnType { get; set; } = ColumnType.Close;

        protected IList<CandleStick> Data { get; set; }

        public IList<EmaValue> Ema { get; } = new List<EmaValue>();

        public EmaProvider(IList<CandleStick> data, int period, bool wilder, ColumnType columnType = ColumnType.Close)
        {
            Period = period;
            Wilder = wilder;
            ColumnType = columnType;
            Data = data;
            Calculate();
        }

        /// <summary>
        /// SMA: 10 period sum / 10 
        /// Multiplier: (2 / (Time periods + 1) ) = (2 / (10 + 1) ) = 0.1818 (18.18%)
        /// EMA: {Close - EMA(previous day)} x multiplier + EMA(previous day). 
        /// for Wilder parameter details: http://www.inside-r.org/packages/cran/TTR/docs/GD
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages"/>
        /// <returns></returns>
        public void Calculate()
        {
            var multiplier = !this.Wilder ? (2.0 / (double)(Period + 1)) : (1.0 / (double)Period);

            for (int i = 0; i < Data.Count; i++)
            {
                if (i >= Period - 1)
                {
                    double value = 0.0;
                    switch (ColumnType)
                    {
                        case ColumnType.Close:
                            value = Data[i].close;
                            break;
                        case ColumnType.High:
                            value = Data[i].high;
                            break;
                        case ColumnType.Low:
                            value = Data[i].low;
                            break;
                        case ColumnType.Open:
                            value = Data[i].open;
                            break;
                        case ColumnType.Volume:
                            value = Data[i].volume;
                            break;
                        default:
                            break;
                    }

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
                            switch (ColumnType)
                            {
                                 case ColumnType.Close:
                                    sum += Data[j].close;
                                    break;
                                case ColumnType.High:
                                    sum += Data[j].high;
                                    break;
                                case ColumnType.Low:
                                    sum += Data[j].low;
                                    break;
                                case ColumnType.Open:
                                    sum += Data[j].open;
                                    break;
                                case ColumnType.Volume:
                                    sum += Data[j].volume;
                                    break;
                                default:
                                    break;
                            }
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
        }
    }
}
