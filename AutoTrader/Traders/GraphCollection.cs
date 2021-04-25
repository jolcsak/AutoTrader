using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTrader.Db;
using AutoTrader.GraphProviders;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class GraphCollection
    {
        private ITrader trader;

        protected SmaProvider smaProvider = new SmaProvider();

        public AoProvider AoProvider { get; } = new AoProvider();

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        public ObservableCollection<double> PastPrices { get; set; }
        public IList<double> Sma => smaProvider.Sma;
        public IList<AoValue> Ao => AoProvider.Ao;

        public IList<DateTime> Dates { get; set; }
        public int PricesSkip { get; set; }
        public int SmaSkip { get; set; } = 0;
        public double MaxPeriodPrice => PastPrices.Any() ? PastPrices.Max() : 0;
        public double MinPeriodPrice => PastPrices.Any() ? PastPrices.Min() : 0;

        public GraphCollection(ITrader trader)
        {
            this.trader = trader;
        }

        public void Refresh(double? actualPrice = null)
        {
            if (PastPrices == null)
            {
                IList<Db.Entities.Price> prices = Store.Prices.GetPricesForTrader(trader);
                PastPrices = new ObservableCollection<double>(prices.Select(p => p.Value));
                Dates = new List<DateTime>(prices.Select(p => p.Time));
                smaProvider.SetData(PastPrices);
                AoProvider.SetData(PastPrices);
                SmaSkip = smaProvider.Sma.Count - Ao.Count;
                PricesSkip = PastPrices.Count - Ao.Count;
            }
            else if (actualPrice.HasValue)
            {
                PastPrices.Add(actualPrice.Value);
            }
        }
    }
}
