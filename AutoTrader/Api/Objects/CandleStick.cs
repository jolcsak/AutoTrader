using System;

namespace AutoTrader.Api.Objects
{
    public class CandleStick
    {
        public int time { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double low { get; set; }
        public double high { get; set; }
        public double volume { get; set; }
        public double quote_volume { get; set; }
        public int count { get; set; }

        public DateTime Date => NiceHashApi.UnixTimestampToDateTime(time);

        public CandleStick Clone()
        {
            return new CandleStick
            {
                time = time,
                open = open,
                close = close,
                low = low,
                high = high,
                volume = volume,
                quote_volume = quote_volume,
                count = count
            };
        }
    }
}
