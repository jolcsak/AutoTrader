using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoTrader.Api;
using AutoTrader.Api.Objects;
using AutoTrader.Db;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;
using AutoTrader.Traders.Trady;
using Trady.Analysis;
using Trady.Core;
using Trady.Core.Infrastructure;
using Trady.Analysis.Backtest;

namespace AutoTrader.Traders
{
    public class TradingBotManager
    {
        private ITrader trader;

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public IList<IOhlcv> Prices { get; set; }

        public IList<DateTime> Dates { get; set; }  

        public IList<double> FiatBalances { get; set; }

        public IList<double> BtcBalances { get; set; }

        public List<TradeItem> Trades { get; private set; }

        public ITradingBot AoBot { get; set; }

        public ITradingBot RsiBot { get; set; }

        public ITradingBot MacdBot { get; set; }

        public ITradingBot SpikeBot { get; set; }

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        protected Predicate<IIndexedOhlcv> buyRule;
        protected Predicate<IIndexedOhlcv> sellRule;

        protected Predicate<IIndexedOhlcv> tempBuyRule;
        protected Predicate<IIndexedOhlcv> tempSellRule;

        public double ProjectedIncome => GetProjectedIncome(buyRule, sellRule);

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

        public CandleStick Refresh(bool add = false)
        {
            if (Prices == null)
            {
                DateProvider = new DateProvider(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
                Prices = new NiceHashImporter().Import(trader.TargetCurrency, DateProvider.MinDate, DateProvider.MaxDate);
                if (Prices.Count > 0)
                {
                    DateProvider.MinDate = Prices.First().DateTime.DateTime;
                    Dates = new List<DateTime>(Prices.Select(cs => cs.DateTime.DateTime));
                }
            }

            CandleStick lastCandleStick = RefreshPrices(add);

            var storedBalances = Store.TotalBalances.GetTotalBalances(trader).Where(tb => tb.FiatBalance > 1).ToList();

            FiatBalances = storedBalances.Select(b => b.FiatBalance).ToList();
            BtcBalances = storedBalances.Select(b => b.BtcBalance).ToList();

            Trades = new List<TradeItem>();
            List<TradeItem> aoTrades = new List<TradeItem>();
            List<TradeItem> rsiTrades = new List<TradeItem>();
            List<TradeItem> macdTrades = new List<TradeItem>();
            List<TradeItem> spikeTrades = new List<TradeItem>();

            buyRule = Rule.Create(c => false);
            sellRule = Rule.Create(c => false);

            tempBuyRule = Rule.Create(c => false);
            tempSellRule = Rule.Create(c => false);

            var tasks = new List<Task>();
            if (TradeSettings.SmaBotEnabled && IsBotRuleMerged(AoBot))
            {
                tasks.Add(Task.Factory.StartNew(() => aoTrades = AoBot.RefreshAll()));
            }
            if (TradeSettings.RsiBotEnabled && IsBotRuleMerged(RsiBot))
            {
                tasks.Add(Task.Factory.StartNew(() => rsiTrades = RsiBot.RefreshAll()));
            }
            if (TradeSettings.MacdBotEnabled && IsBotRuleMerged(MacdBot))
            {
                tasks.Add(Task.Factory.StartNew(() => macdTrades = MacdBot.RefreshAll()));
            }
            if (TradeSettings.SpikeBotEnabled && IsBotRuleMerged(SpikeBot))
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

            return lastCandleStick;
        }

        private bool IsBotRuleMerged(ITradingBot bot)
        {
            tempBuyRule = Rule.Or(bot.BuyRule, buyRule);
            tempSellRule = Rule.Or(bot.SellRule, sellRule);

            if (GetProjectedIncome(tempBuyRule, tempSellRule) > GetProjectedIncome(buyRule, sellRule))
            {
                buyRule = Rule.Or(bot.BuyRule, buyRule);
                sellRule = Rule.Or(bot.SellRule, sellRule);
                return true;
            }
            return false;
        }

        private CandleStick RefreshPrices(bool add)
        {
            CandleStick[] candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + "BTC", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 1);
            CandleStick lastCandleStick = candleSticks.LastOrDefault();
            if (lastCandleStick != null)
            {
                if (!add && Prices.Count > 0)
                {
                    Prices.RemoveAt(Prices.Count - 1);
                }

                Prices.Add(new Candle(lastCandleStick.Date, (decimal)lastCandleStick.open, (decimal)lastCandleStick.high, (decimal)lastCandleStick.low, (decimal)lastCandleStick.close, (decimal)lastCandleStick.volume));
                DateProvider.MaxDate = lastCandleStick.Date;
                Dates.Add(DateProvider.MaxDate);
                if (add) {
                    if (Prices.Count > 1)
                    {
                        Prices.RemoveAt(0);
                    }
                    if (Dates.Count > 1)
                    {
                        Dates.RemoveAt(0);
                    }
                }
            }
            return lastCandleStick;
        }

        private double GetProjectedIncome(Predicate<IIndexedOhlcv> buyRule, Predicate<IIndexedOhlcv> sellRule)
        {
            if (Prices?.Count > 0)
            {
                var runner = new Builder()
                    .Add(Prices)
                    .Buy(buyRule)
                    .Sell(sellRule)
                    .BuyWithAllAvailableCash()
                    .FlatExchangeFeeRate(0.001m)
                    .Premium(1)
                    .Build();

                var result = runner.Run(100, DateProvider.MinDate, DateProvider.MaxDate);
                return (double)result.TotalCorrectedBalance - 100;
            }
            return 0;
        }
    }
}
