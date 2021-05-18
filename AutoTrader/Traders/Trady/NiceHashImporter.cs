using AutoTrader.Api;
using AutoTrader.Api.Objects;
using System;
using System.Collections.Generic;
using Trady.Core;
using Trady.Core.Infrastructure;
using Trady.Core.Period;

namespace AutoTrader.Traders.Trady
{
    public class NiceHashImporter
    {
        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public IList<IOhlcv> Import(string symbol, DateTime startTime, DateTime endTime, PeriodOption period = PeriodOption.Hourly)
        {
            var dateProvider = new DateProvider(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
            CandleStick[] candleSticks = NiceHashApi.GetCandleSticks(symbol + "BTC", startTime, endTime, 60);
            var candles = new List<IOhlcv>();

            if (candleSticks?.Length > 0)
            {
                foreach (CandleStick candleStick in candleSticks) {
                    Candle candle = new Candle(candleStick.Date, (decimal)candleStick.open, (decimal)candleStick.high, (decimal)candleStick.low, (decimal)candleStick.close, (decimal)candleStick.volume);
                    candles.Add(candle);
                }
            }

            return candles;
        }
    }
}
