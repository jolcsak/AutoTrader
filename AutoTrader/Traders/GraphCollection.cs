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
        private const int SMA_SMOOTHNESS = 20;

        private ITrader trader;

        protected SmaProvider smaProvider = new SmaProvider(SMA_SMOOTHNESS);

        public AoProvider AoProvider { get; } = new AoProvider();

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public ObservableCollection<double> PastPrices { get; set; }

        public IList<double> MlPrices { get; set; }
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
                IList<Db.Entities.Price> prices = Store.Prices.GetPricesForTrader(trader);

                MlPrices = prices.Select(p => p.Value).ToList();
                PastPrices = new ObservableCollection<double>(prices.Select(p => p.Value));
                Dates = new List<DateTime>(prices.Select(p => p.Time));
                smaProvider.SetData(PastPrices);
                AoProvider.SetData(new ObservableCollection<double>(smaProvider.Sma.Where(sma =>sma > -1)));
                SmaSkip = smaProvider.Sma.Count - Ao.Count;
                PricesSkip = PastPrices.Count - Ao.Count;
            }
            else 
            {
                if (actualPrice.HasValue)
                {
                    PastPrices.Add(actualPrice.Value);
                    //var input = new ModelInput { Col0 = DateTime.Now.Ticks };

                    // Load model and predict output of sample data
                    //if (trader.TargetCurrency == "PPT")
                    //{
                    //    ModelOutput result = ConsumeModel.Predict(input);
                    //    MlPrices.Add(result.Score);
                    //}
                    //else
                    {
                        MlPrices.Add(actualPrice.Value);
                    }
                }
                if (date.HasValue)
                {
                    Dates.Add(date.Value);
                }
            }

            Balances = Store.TotalBalances.GetTotalBalances(trader).Select(b => b.Balance).ToList();
        }
    }
}
