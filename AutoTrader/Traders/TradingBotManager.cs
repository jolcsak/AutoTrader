using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoTrader.Api;
using AutoTrader.Api.Objects;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Indicators;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;
using MathNet.Filtering;

namespace AutoTrader.Traders
{
    public class TradingBotManager
    {
        public const int RSI_PERIOD = 14;
        
        private const int SMA_FAST_SMOOTHNESS = 5;
        private const int SMA_SLOW_SMOOTHNESS = 9;

        private const int EMA_FAST = 12;
        private const int EMA_SLOW = 24;
        private const int MACD_SIGNAL = 19;

        private ITrader trader;

        protected SmaProvider smaSlowProvider;
        protected SmaProvider smaFastProvider;

        public RsiProvider RsiProvider { get; private set; }
        public AoProvider AoProvider { get; private set; }

        public MacdProvider MacdProvider { get; private set; }

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

        public IList<double> FiatBalances { get; set; }

        public IList<double> BtcBalances { get; set; }

        public List<TradeItem> Trades { get; private set; }

        public ITradingBot AoBot { get; set; }

        public ITradingBot RsiBot { get; set; }

        public ITradingBot MacdBot { get; set; }

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public double ProjectedIncome => GetProjectedIncome();

        public DateProvider DateProvider { get; private set; }
        public TradeItem LastTrade { get; set; }

        public TradingBotManager(ITrader trader)
        {
            this.trader = trader;

            AoBot = new AoBot(this);
            RsiBot = new RsiBot(this);
            MacdBot = new MacdBot(this);
        }

        public void Refresh(ActualPrice actualPrice = null, bool add = false)
        {
            CandleStick[] candleSticks;
            CandleStick lastCandleStick = null;
            if (PastPrices == null)
            {
                DateProvider = new DateProvider(DateTime.Now.AddMonths(-1), DateTime.Now);
                candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateProvider.MinDate, DateProvider.MaxDate, 60);

                if (candleSticks == null || candleSticks.Length == 0)
                {
                    return;
                }

                DateProvider.MinDate = candleSticks.Min(cs => cs.Date);   
                PastPrices = candleSticks.ToList();
                Dates = new List<DateTime>(candleSticks.Select(cs => cs.Date));
            }

            if (actualPrice != null)
            {
                if (!add)
                {
                    if (PastPrices.Count > 0)
                    {
                        PastPrices.RemoveAt(PastPrices.Count - 1);
                    }
                }

                lastCandleStick = new CandleStick(actualPrice);
                PastPrices.Add(lastCandleStick);
                DateProvider.MaxDate = DateTime.Now;
                Dates.Add(DateProvider.MaxDate);
            }

            var tasks = new List<Task>
            {
                Task.Factory.StartNew(() => smaSlowProvider = new SmaProvider(PastPrices, SMA_SLOW_SMOOTHNESS)),
                Task.Factory.StartNew(() => smaFastProvider = new SmaProvider(PastPrices, SMA_FAST_SMOOTHNESS)),
                Task.Factory.StartNew(() => AoProvider = new AoProvider(PastPrices)),
                Task.Factory.StartNew(() => RsiProvider = new RsiProvider(PastPrices, RSI_PERIOD)),
                Task.Factory.StartNew(() => MacdProvider = new MacdProvider(PastPrices, EMA_FAST, EMA_SLOW, MACD_SIGNAL)),
            };
            Task.WaitAll(tasks.ToArray());

            tasks.Clear();

            double amplitude = AoProvider.Amplitude;
            if (!double.IsNaN(amplitude))
            {
                var filter = OnlineFilter.CreateLowpass(ImpulseResponse.Finite, 50, amplitude);
                tasks.Add(Task.Factory.StartNew(() => Tendency = filter.ProcessSamples(PastPrices.Select(pp => pp.close).ToArray())));
            }
            else
            {
                Tendency = Array.Empty<double>();
            }

            var storedBalances = Store.TotalBalances.GetTotalBalances(trader).ToList();

            FiatBalances = storedBalances.Select(b => b.FiatBalance).ToList();
            BtcBalances = storedBalances.Select(b => b.BtcBalance).ToList();

            Trades = new List<TradeItem>();
            List<TradeItem> aoTrades = new List<TradeItem>();
            List<TradeItem> rsiTrades = new List<TradeItem>();
            List<TradeItem> macdTrades = new List<TradeItem>();

            if (TradeSettings.SmaBotEnabled)
            {
                tasks.Add(Task.Factory.StartNew(() => aoTrades = AoBot.RefreshAll()));
            }
            if (TradeSettings.RsiBotEnabled)
            {
                tasks.Add(Task.Factory.StartNew(() => rsiTrades = RsiBot.RefreshAll()));
            }
            if (TradeSettings.MacdBotEnabled)
            {
                tasks.Add(Task.Factory.StartNew(() => macdTrades = MacdBot.RefreshAll()));
            }

            Task.WaitAll(tasks.ToArray());

            Trades.AddRange(aoTrades);
            Trades.AddRange(rsiTrades);
            Trades.AddRange(macdTrades);
            Trades = Trades.OrderBy(t => t.Date).ToList();

            if (lastCandleStick != null && Trades.Any())
            {
                LastTrade = Trades.FirstOrDefault(t => t.Date.Equals(lastCandleStick.Date));
            }
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
                        tradeItems.Add(new TradeOrder("", trade.Price, amount, amount, "CUR", 0, "TRADER"));
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

        private double Sell( IList<TradeOrder> tradeItems, TradeItem trade)
        {
            double money = 0;
            foreach (var tradeItem in tradeItems)
            {
                if (tradeItem.Type == TradeOrderType.OPEN && trade.Price > tradeItem.Price * TradeSettings.MinSellYield)
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
