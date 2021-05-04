using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.GraphProviders;
using AutoTrader.Log;
using MathNet.Filtering;

namespace AutoTrader.Traders
{
    public class GraphCollection
    {
        public const int RSI_PERIOD = 14;
        private const int SMA_FAST_SMOOTHNESS = 5;
        private const int SMA_SLOW_SMOOTHNESS = 9;

        private ITrader trader;

        protected SmaProvider smaSlowProvider;
        protected SmaProvider smaFastProvider;

        public RsiProvider RsiProvider { get; private set; }

        public AoProvider AoProvider { get; private set; }

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public IList<double> PastPrices { get; set; }

        public IList<double> SmaSlow => smaSlowProvider.Sma;
        public IList<double> SmaFast => smaFastProvider.Sma;

        public IList<AoValue> Ao => AoProvider.Ao;

        public IList<RsiValue> Rsi => RsiProvider.Rsi;

        public IList<double> Tendency { get; private set; }

        public IList<DateTime> Dates { get; set; }  

        public IList<double> Balances { get; set; }
        public int PricesSkip { get; set; }
        public int SmaSkip { get; set; } = 0;
        public double MaxPeriodPrice => PastPrices.Any() ? PastPrices.Max() : 0;
        public double MinPeriodPrice => PastPrices.Any() ? PastPrices.Min() : 0;

        public GraphCollection(ITrader trader)
        {
            this.trader = trader;
        }

        public void Refresh(double? actualPrice = null, DateTime? date = null)
        {
            var candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateTime.Now.AddMonths(-1), DateTime.Now, 60);
            var prices = candleSticks.Select(cs => cs.close).ToArray();

            PastPrices = prices;
            Dates = new List<DateTime>(candleSticks.Select(cs => NiceHashApi.UnixTimestampToDateTime(cs.time)));

            smaSlowProvider = new SmaProvider(PastPrices, SMA_SLOW_SMOOTHNESS);
            smaFastProvider = new SmaProvider(PastPrices, SMA_FAST_SMOOTHNESS);

            AoProvider =  new AoProvider(PastPrices);
            PricesSkip = PastPrices.Count - Ao.Count;
            SmaSkip = PricesSkip;

            RsiProvider = new RsiProvider(PastPrices, RSI_PERIOD);

            double amplitude = AoProvider.Amplitude;
            if (!double.IsNaN(amplitude))
            {
                var filter = OnlineFilter.CreateLowpass(ImpulseResponse.Finite, 50, amplitude);
                Tendency = filter.ProcessSamples(prices);
            }
            else
            {
                Tendency = Array.Empty<double>();
            }

            Balances = Store.TotalBalances.GetTotalBalances(trader).Select(b => b.Balance).ToList();
        }
    }
}
