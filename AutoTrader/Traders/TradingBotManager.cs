using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoTrader.Api;
using AutoTrader.Api.Objects;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;
using AutoTrader.Traders.Trady;
using MathNet.Filtering;
using Trady.Analysis;
using Trady.Analysis.Indicator;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders
{
    public class TradingBotManager
    {
        public const int RSI_PERIOD = 14;
        
        private const int SMA_FAST_SMOOTHNESS = 5;
        private const int SMA_SLOW_SMOOTHNESS = 9;

        private const int EMA_FAST = 12;
        private const int EMA_SLOW = 26;
        private const int MACD_SIGNAL = 9;

        private ITrader trader;

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public IList<IOhlcv> PastPrices { get; set; }

        public SimpleMovingAverage SmaSlow { get; private set; }
        public SimpleMovingAverage SmaFast { get; private set; }

        public SimpleMovingAverageOscillator Ao { get; private set; }

        public RelativeStrengthIndex Rsi { get; private set; }

        public MovingAverageConvergenceDivergence Macd { get; private set; }

        public MovingAverageConvergenceDivergenceHistogram MacdHistogram { get; private set; }

        public List<AnalyzableTick<decimal?>> Trend { get; private set; }

        public IList<DateTime> Dates { get; set; }  

        public IList<double> FiatBalances { get; set; }

        public IList<double> BtcBalances { get; set; }

        public List<TradeItem> Trades { get; private set; }

        public ITradingBot AoBot { get; set; }

        public ITradingBot RsiBot { get; set; }

        public ITradingBot MacdBot { get; set; }

        public ITradingBot SpikeBot { get; set; }

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
            SpikeBot = new SpikeBot(this);
        }

        public void Refresh(ActualPrice actualPrice = null, bool add = false)
        {
            if (PastPrices == null)
            {
                DateProvider = new DateProvider(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                PastPrices = new NiceHashImporter().Import(trader.TargetCurrency, DateProvider.MinDate, DateProvider.MaxDate);
                if (PastPrices.Count > 0)
                {
                    DateProvider.MinDate = PastPrices.First().DateTime.DateTime;
                    Dates = new List<DateTime>(PastPrices.Select(cs => cs.DateTime.DateTime));
                }
            }

            CandleStick lastCandleStick = actualPrice != null ? RefreshWithActualPrice(actualPrice, add) : null;

            var tasks = new List<Task>
            {
                Task.Factory.StartNew(() => SmaSlow = new SimpleMovingAverage(PastPrices, SMA_SLOW_SMOOTHNESS)),
                Task.Factory.StartNew(() => SmaFast = new SimpleMovingAverage(PastPrices, SMA_FAST_SMOOTHNESS)),
                Task.Factory.StartNew(() => Ao = new SimpleMovingAverageOscillator(PastPrices, SMA_FAST_SMOOTHNESS, SMA_SLOW_SMOOTHNESS)),
                Task.Factory.StartNew(() => Rsi = new RelativeStrengthIndex(PastPrices, RSI_PERIOD)),
                Task.Factory.StartNew(() => Macd = new MovingAverageConvergenceDivergence(PastPrices, EMA_FAST, EMA_SLOW, MACD_SIGNAL)),
                Task.Factory.StartNew(() => MacdHistogram = new MovingAverageConvergenceDivergenceHistogram(PastPrices, EMA_FAST, EMA_SLOW, MACD_SIGNAL))
            };
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();

            SetTrend();

            var storedBalances = Store.TotalBalances.GetTotalBalances(trader).Where(tb => tb.FiatBalance > 1).ToList();

            FiatBalances = storedBalances.Select(b => b.FiatBalance).ToList();
            BtcBalances = storedBalances.Select(b => b.BtcBalance).ToList();

            Trades = new List<TradeItem>();
            List<TradeItem> aoTrades = new List<TradeItem>();
            List<TradeItem> rsiTrades = new List<TradeItem>();
            List<TradeItem> macdTrades = new List<TradeItem>();
            List<TradeItem> spikeTrades = new List<TradeItem>();

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
            if (TradeSettings.SpikeBotEnabled)
            {
                tasks.Add(Task.Factory.StartNew(() => spikeTrades = SpikeBot.RefreshAll()));
            }

            Task.WaitAll(tasks.ToArray());

            Trades.AddRange(aoTrades);
            Trades.AddRange(rsiTrades);
            Trades.AddRange(macdTrades);
            Trades.AddRange(spikeTrades);
            Trades = Trades.OrderBy(t => t.Date).ToList();

            if (lastCandleStick != null && Trades.Any())
            {
                LastTrade = Trades.FirstOrDefault(t => t.Date.Equals(lastCandleStick.Date));
            }
        }

        private CandleStick RefreshWithActualPrice(ActualPrice actualPrice, bool add)
        {
            if (!add && PastPrices.Count > 0)
            {
                PastPrices.RemoveAt(PastPrices.Count - 1);
            }

            CandleStick[] candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 1);
            CandleStick lastCandleStick = candleSticks.LastOrDefault();
            if (lastCandleStick != null)
            {
                PastPrices.Add(new Candle(lastCandleStick.Date, (decimal)lastCandleStick.open, (decimal)lastCandleStick.high, (decimal)lastCandleStick.low, (decimal)lastCandleStick.close, (decimal)lastCandleStick.volume));

                DateProvider.MaxDate = lastCandleStick.Date;
                Dates.Add(DateProvider.MaxDate);

            }
            return lastCandleStick;
        }

        private void SetTrend()
        {
            Trend = new List<AnalyzableTick<decimal?>>();
            IList<double> pastPrices = PastPrices.Select(pp => (double)pp.Close).ToList();
            double amplitude = Math.Abs(pastPrices.Max());
            if (amplitude > 0)
            {
                var filter = OnlineFilter.CreateLowpass(ImpulseResponse.Finite, 20, amplitude / 2);
                double[] trendValues = filter.ProcessSamples(pastPrices.ToArray());
                for (int i = 0; i < PastPrices.Count; i++)
                {
                    Trend.Add(new AnalyzableTick<decimal?>(PastPrices[i].DateTime, (decimal)trendValues[i]));
                }
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
                    double sum = amount * trade.Price;
                    if (money >= sum)
                    {
                        money -= sum;
                        tradeItems.Add(new TradeOrder(TradeOrderType.MARKET, string.Empty, trade.Price, amount, amount, string.Empty, 0, string.Empty, trade.Period));
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
                if (tradeItem.State == TradeOrderState.OPEN && trade.Price > tradeItem.Price * TradeSettings.MinSellYield)
                {
                    tradeItem.SellPrice = trade.Price;
                    tradeItem.State = TradeOrderState.CLOSED;
                    money += tradeItem.Amount * trade.Price;
                }
            }
            return money;
        }
    }
}
