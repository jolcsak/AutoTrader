using AutoTrader.Traders;
using System;

namespace AutoTrader.Api.Objects
{
    public class CandleStick
    {
        private double _close;
        public long time { get; set; }
        public double open { get; set; }
        public double close {
            get => _close;
            set
            {
                temp_close = value;
                _close = value;
            }
        }
        public double low { get; set; }
        public double high { get; set; }
        public double volume { get; set; }
        public double quote_volume { get; set; }
        public int count { get; set; }

        public double temp_close { get; set; }

        public DateTime Date => NiceHashApi.UnixTimestampToDateTime(time);

        public CandleStick()
        {
        }

        public CandleStick(ActualPrice price)
        {
            close = price.Price;
            low = close;
            high = close;
            open = close;
            volume = price.Amount;
            quote_volume = volume;
            time = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

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
                count = count,
                temp_close = temp_close
            };
        }
    }
}
