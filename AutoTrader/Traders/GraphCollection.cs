using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Api.Objects;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using AutoTrader.Log;
using AutoTrader.Traders.Agents;
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

        public IList<CandleStick> PastPrices { get; set; }

        public IList<SmaValue> SmaSlow => smaSlowProvider.Sma;
        public IList<SmaValue> SmaFast => smaFastProvider.Sma;

        public IList<AoValue> Ao => AoProvider.Ao;

        public IList<RsiValue> Rsi => RsiProvider.Rsi;

        public IList<double> Tendency { get; private set; }

        public IList<DateTime> Dates { get; set; }  

        public IList<double> Balances { get; set; }

        public List<TradeItem> Trades { get; private set; }

        public int PricesSkip { get; set; }
        public int SmaSkip { get; set; } = 0;
        public double MaxPeriodPrice => PastPrices.Any() ? PastPrices.Select(pp => pp.close).Max() : 0;
        public double MinPeriodPrice => PastPrices.Any() ? PastPrices.Select(pp => pp.close).Min() : 0;

        public IAgent AoAgent { get; set; }

        public IAgent RsiAgent { get; set; }

        public bool IsBuy => Trades.LastOrDefault(t => t.Date.AddHours(1) >= DateTime.Now)?.Type  == TradeType.Buy;

        public bool IsSell => Trades.LastOrDefault(t => t.Date.AddHours(1) >= DateTime.Now)?.Type == TradeType.Sell;

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public double ProjectedIncome => GetProjectedIncome();


        public GraphCollection(ITrader trader)
        {
            this.trader = trader;

            AoAgent = new AoAgent(this);
            RsiAgent = new RsiAgent(this);
        }

        public void Refresh()
        {
            var candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateTime.Now.AddMonths(-1), DateTime.Now, 60);

            PastPrices = candleSticks;
            Dates = new List<DateTime>(candleSticks.Select(cs => cs.Date));

            smaSlowProvider = new SmaProvider(candleSticks, SMA_SLOW_SMOOTHNESS);
            smaFastProvider = new SmaProvider(candleSticks, SMA_FAST_SMOOTHNESS);

            AoProvider = new AoProvider(candleSticks);
            PricesSkip = PastPrices.Count - Ao.Count;
            SmaSkip = PricesSkip;

            RsiProvider = new RsiProvider(PastPrices, RSI_PERIOD);

            double amplitude = AoProvider.Amplitude;
            if (!double.IsNaN(amplitude))
            {
                var filter = OnlineFilter.CreateLowpass(ImpulseResponse.Finite, 50, amplitude);
                Tendency = filter.ProcessSamples(PastPrices.Select(pp => pp.close).ToArray());
            }
            else
            {
                Tendency = Array.Empty<double>();
            }

            Balances = Store.TotalBalances.GetTotalBalances(trader).Select(b => b.Balance).ToList();

            Trades = new List<TradeItem>();
            if (TradeSettings.SmaBotEnabled)
            {
                Trades.AddRange(AoAgent.RefreshAll());
            }
            if (TradeSettings.RsiBotEnabled)
            {
                Trades.AddRange(RsiAgent.RefreshAll());
            }

            Trades = Trades.OrderBy(t => t.Date).ToList();
        }

        private double GetProjectedIncome()
        {
            if (Trades == null  || !Trades.Any()) {
                return 1;
            }

            double money = 100 * Trades.First().Price;
            double startMoney = money;
            double amount = 25;
            IList<TradeOrder> tradeItems = new List<TradeOrder>();
            foreach (var trade in Trades)
            {
                if (trade.Type == TradeType.Buy)
                {
                    if (money >= amount * trade.Price)
                    {
                        double transactionMoney = amount * trade.Price;
                        money -= transactionMoney;
                        tradeItems.Add(new TradeOrder(Guid.NewGuid().ToString(), trade.Price, amount, amount, "CUR", 0, "TRADER"));
                    }
                }
                else if (trade.Type == TradeType.Sell)
                {
                    money += Sell(tradeItems, trade);
                }
            }

            money += Sell(tradeItems, Trades.Last());

            return money / startMoney;
        }

        private static double Sell( IList<TradeOrder> tradeItems, TradeItem trade)
        {
            double money = 0;
            foreach (var tradeItem in tradeItems)
            {
                if (tradeItem.Type == TradeOrderType.OPEN && trade.Price > tradeItem.Price * 1.03)
                {
                    tradeItem.SellPrice = trade.Price;
                    tradeItem.Type = TradeOrderType.CLOSED;
                    money += tradeItem.Amount * trade.Price;
                }
            }

            return money;
        }
    }
}
