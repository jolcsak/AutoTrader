using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.GraphProviders;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class GraphCollection
    {
        private const int PERIOD = 15;
        private const int SMA_SMOOTHNESS = 5;

        private ITrader trader;

        private double priceSum = 0;
        private int counter = 0;

        protected SmaProvider smaProvider = new SmaProvider(SMA_SMOOTHNESS);

        public AoProvider AoProvider { get; } = new AoProvider();

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public ObservableCollection<double> PastPrices { get; set; }

        public IList<double> Sma => smaProvider.Sma;
        public IList<AoValue> Ao => AoProvider.Ao;

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
            if (PastPrices == null)
            {
                var candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateTime.Now.AddMonths(-1), DateTime.Now, 60);
                var prices = candleSticks.Select(cs => cs.open).ToArray();

                PastPrices = new ObservableCollection<double>(prices);
                Dates = new List<DateTime>(candleSticks.Select(cs => NiceHashApi.UnixTimestampToDateTime(cs.time)));
                smaProvider.SetData(PastPrices);
                AoProvider.SetData(PastPrices);
                SmaSkip = smaProvider.Sma.Count - Ao.Count;
                PricesSkip = PastPrices.Count - Ao.Count;
            }
            else 
            {
                if (actualPrice.HasValue && counter == PERIOD - 1)
                {
                    counter = 0;
                    priceSum += actualPrice.Value;
                    double avgPrice = priceSum / PERIOD;
                    PastPrices.Add(avgPrice);

                    if (date.HasValue)
                    {
                        Dates.Add(date.Value);
                    }

                    priceSum = 0;
                }
                else
                {
                    if (actualPrice.HasValue) {
                        priceSum += actualPrice.Value;
                        counter++;
                    }
                }
            }

            Balances = Store.TotalBalances.GetTotalBalances(trader).Select(b => b.Balance).ToList();
        }
    }
}
