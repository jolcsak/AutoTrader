using System;
using System.Collections.Generic;
using Trady.Core;
using Trady.Core.Infrastructure;
using Trady.Core.Period;

namespace AutoTrader.Traders.Trady
{
    public class FakeNiceHashImporter : INiceHashImporter
    {
        Random rnd = new Random();

        public IList<IOhlcv> Import(string symbol, DateTime startTime, DateTime endTime, PeriodOption period = PeriodOption.Hourly)
        {
            var dateDiff = endTime - startTime;
            long totalHour = (long) dateDiff.TotalSeconds / 3600;

            List<IOhlcv> prices = new List<IOhlcv>();
            DateTime currentDate = startTime;
            PriceBar bar = new PriceBar { Close = rnd.NextDouble() + 0.000001 };

            double fluct = 0.02 + rnd.NextDouble() / 8;
            double volFluct = 0.1 + rnd.NextDouble();

            for (int i = 0; i < totalHour; i++)
            {
                GenerateRandomBar(bar, fluct, volFluct);
                Candle candle = new Candle(currentDate , (decimal)bar.Open, (decimal)bar.High, (decimal)bar.Low, (decimal)bar.Close, (decimal)bar.Volume);
                prices.Add(candle);

                currentDate = currentDate.AddHours(1);
            }
            return prices;
        }

        public double GetRandomNumber(double minimum, double maximum)
        {
            return rnd.NextDouble() * (maximum - minimum) + minimum;
        }

        public void GenerateRandomBar(PriceBar newBar, double fluct = 0.025, double volFluct = 0.40)
        {
            newBar.Open = newBar.Close;
            newBar.Close = GetRandomNumber(newBar.Close - newBar.Close * fluct, newBar.Close + newBar.Close * fluct);
            newBar.High = GetRandomNumber(Math.Max(newBar.Close, newBar.Open), Math.Max(newBar.Close, newBar.Open) + Math.Abs(newBar.Close - newBar.Open) * fluct);
            newBar.Low = GetRandomNumber(Math.Min(newBar.Close, newBar.Open), Math.Min(newBar.Close, newBar.Open) - Math.Abs(newBar.Close - newBar.Open) * fluct);
            newBar.Volume = (long)GetRandomNumber(newBar.Volume * volFluct, newBar.Volume);
        }

        public class PriceBar
        {
            public DateTime Date { get; set; }
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
            public long Volume { get; set; }
        }
    }
}
