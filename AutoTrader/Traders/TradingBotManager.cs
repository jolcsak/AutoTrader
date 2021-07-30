using System;
using System.Collections.Generic;
using System.Linq;
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
        private const bool BOT_OPTIMIZATION = false;
        private const string FIAT = "HUF";

        public static int LastMonths { get; set; } = -1;

        public static bool IsBenchmarking { get; set; } = false;

        public static int BenchmarkIteration { get; set; } = 0;

        private ITrader trader;

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());
        protected static Store Store => Store.Instance;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public ITrader Trader => trader;

        public IList<IOhlcv> Prices { get; set; }

        public IList<DateTime> Dates { get; set; }  

        public static IList<double> FiatBalances { get; set; }

        public static IList<double> BtcBalances { get; set; }

        public List<TradeItem> Trades { get; private set; }

        public ITradingBot AoBot { get; set; }

        public ITradingBot RsiBot { get; set; }

        public ITradingBot MacdBot { get; set; }

        public ITradingBot SpikeBot { get; set; }

        public ITradingBot AiBot { get; set; }

        public ITradingBot BenchmarkBot { get; set; }

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        protected Predicate<IIndexedOhlcv> buyRule;
        protected Predicate<IIndexedOhlcv> sellRule;

        protected Predicate<IIndexedOhlcv> tempBuyRule;
        protected Predicate<IIndexedOhlcv> tempSellRule;

        public double ProjectedIncome => GetProjectedIncome(buyRule, sellRule);

        public DateProvider DateProvider { get; private set; }
        public TradeItem LastTrade { get; set; }

        private List<ITradingBot> bots;

        public TradingBotManager(ITrader trader)
        {
            this.trader = trader;

            AoBot = new AoBot(this);
            RsiBot = new RsiBot(this);
            MacdBot = new MacdBot(this);
            SpikeBot = new SpikeBot(this);
            AiBot = new AiBot(this);
            BenchmarkBot = new BenchmarkBot(this);
        }

        public CandleStick Refresh(ActualPrice actualPrice, bool add = false)
        {
            pricesChanged = false;
            bool isPricesEmpty = Prices == null;
            if (isPricesEmpty)
            {
                pricesChanged = true;
                DateProvider = new DateProvider(DateTime.UtcNow.AddMonths(LastMonths), DateTime.UtcNow);
                Prices = new NiceHashImporter().Import(trader.TargetCurrency, DateProvider.MinDate, DateProvider.MaxDate);

                if (Prices.Count > 0)
                {
                    DateProvider.MinDate = Prices.First().DateTime.DateTime;
                    Dates = Prices.Select(cs => cs.DateTime.DateTime).ToList();
                }
            }

            LastTrade = null;
            CandleStick lastCandleStick = null;
            if (!IsBenchmarking)
            {
                lastCandleStick = RefreshPrices(add, actualPrice);

                if (lastCandleStick != null || isPricesEmpty)
                {
                    Trades = new List<TradeItem>();
                    buyRule = Rule.Create(c => false);
                    sellRule = Rule.Create(c => false);
                    tempBuyRule = Rule.Create(c => false);
                    tempSellRule = Rule.Create(c => false);

                    if (bots == null || new Random().Next(30) == 10)
                    {
                        bots = GetEnabledBots();
                        if (BOT_OPTIMIZATION)
                        {
                            var permutations = bots.Permutations();
                            var selectedCombinations = permutations.AsParallel().WithDegreeOfParallelism(6).Select(p => new { Income = GetIncome(p), Bots = p.ToList() }).OrderByDescending(p => p.Income).ToList();

                            var selectedCombination = selectedCombinations.FirstOrDefault();
                            if (selectedCombination != null)
                            {
                                bots = selectedCombination.Bots;
                            }
                        }
                    }

                    MergeBotRules(bots);

                    if (lastCandleStick != null)
                    {
                        LastTrade = Trades.FirstOrDefault(t => t.Date.Equals(lastCandleStick.Date));
                    }
                }
            }
            else
            {
                pricesChanged = true;
                buyRule = BenchmarkBot.BuyRule;
                sellRule = BenchmarkBot.SellRule;
                Trades = BenchmarkBot.RefreshAll();
            }

            return lastCandleStick;
        }

        public void Refresh()
        {
            if (Prices?.Count > 0)
            {
                bots = GetEnabledBots();
                Trades = new List<TradeItem>();
                MergeBotRules(bots);
            }
        }

        public static void RefreshBalanceHistory()
        {
            var storedBalances = Store.TotalBalances.GetTotalBalances().Where(tb => tb.FiatBalance > 1).ToList();

            FiatBalances = storedBalances.Select(b => b.FiatBalance).ToList();
            BtcBalances = storedBalances.Select(b => b.BtcBalance).ToList();
        }

        private List<ITradingBot> GetEnabledBots()
        {
            List<ITradingBot> bots = new List<ITradingBot>();

            if (TradeSettings.RsiBotEnabled)
            {
                bots.Add(RsiBot);
            }
            if (TradeSettings.SpikeBotEnabled)
            {
                bots.Add(SpikeBot);
            }
            if (TradeSettings.MacdBotEnabled)
            {
                bots.Add(MacdBot);
            }
            if (TradeSettings.AiBotEnabled)
            {
                bots.Add(AiBot);
            }
            if (TradeSettings.SmaBotEnabled)
            {
                bots.Add(AoBot);
            }
            if (TradeSettings.BBotEnabled)
            {
                bots.Add(BenchmarkBot);
            }

            return bots;
        }

        private bool IsBotRuleMerged(ITradingBot bot)
        {
            tempBuyRule = Rule.Or(bot.BuyRule, buyRule);
            tempSellRule = Rule.Or(bot.SellRule, sellRule);

            if (GetProjectedIncome(tempBuyRule, tempSellRule) > GetProjectedIncome(buyRule, sellRule))
            {
                return MergeBotRule(bot);
            }
            return false;
        }

        private double GetIncome(ICollection<ITradingBot> bots)
        {
            Predicate<IIndexedOhlcv> buyRule = Rule.Create(c => false);
            Predicate<IIndexedOhlcv> sellRule = Rule.Create(c => false);
            tempBuyRule = Rule.Create(c => false);
            tempSellRule = Rule.Create(c => false);

            double income = 0;
            foreach (var bot in bots)
            {
                tempBuyRule = Rule.Or(bot.BuyRule, buyRule);
                tempSellRule = Rule.Or(bot.SellRule, sellRule);
                if (GetProjectedIncome(tempBuyRule, tempSellRule, isHalf: true) > income)
                {
                    buyRule = Rule.Or(bot.BuyRule, buyRule);
                    sellRule = Rule.Or(bot.SellRule, sellRule);
                    income = GetProjectedIncome(buyRule, sellRule, isHalf: true);
                }
            }
            return income;
        }

        private  void MergeBotRules(ICollection<ITradingBot> tradingBots)
        {
            foreach(var bot in tradingBots)
            {
                if (IsBotRuleMerged(bot))
                {
                    Trades.AddRange(bot.RefreshAll());
                }
            }
        }

        private bool MergeBotRule(ITradingBot bot)
        {
            buyRule = Rule.Or(bot.BuyRule, buyRule);
            sellRule = Rule.Or(bot.SellRule, sellRule);
            return true;
        }

        public static Tuple<double, double> GetTotalFiatBalance()
        {
            TotalBalance totalBalance = NiceHashApi.GetTotalBalance(FIAT);
            var btcCurrency = totalBalance.currencies.FirstOrDefault(c => c.currency == BtcTrader.BTC);
            if (totalBalance?.total != null && btcCurrency != null)
            {
                return new Tuple<double, double>(totalBalance.total.totalBalance, btcCurrency.fiatRate);
            }
            return new Tuple<double, double>(0, 0);
        }

        private CandleStick RefreshPrices(bool add, ActualPrice actualPrice)
        {
            CandleStick[] candleSticks = NiceHashApi.GetCandleSticks(trader.TargetCurrency + BtcTrader.BTC, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 1);
            CandleStick lastCandleStick = candleSticks?.LastOrDefault();
            if (lastCandleStick != null)
            {
                pricesChanged = true;
                if (!add && Prices.Count > 0)
                {
                    Prices.RemoveAt(Prices.Count - 1);
                }

                Prices.Add(new Candle(lastCandleStick.Date, (decimal)lastCandleStick.open, (decimal)lastCandleStick.high, (decimal)lastCandleStick.low, (decimal)(actualPrice?.SellPrice??lastCandleStick.close), (decimal)lastCandleStick.volume));
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

        private double lastIncome = 0;
        private bool pricesChanged = false;

        private double GetProjectedIncome(Predicate<IIndexedOhlcv> buyRule, Predicate<IIndexedOhlcv> sellRule, bool isHalf = false)
        {
            if (Prices?.Count > 0 && buyRule != null && sellRule != null)
            {
                if (pricesChanged)
                {
                    var runner = new Builder()
                        .Add(Prices.Skip(isHalf ? Prices.Count / 2 : 0))
                        .Buy(buyRule)
                        .Sell(sellRule)
                        .BuyWithAllAvailableCash()
                        .FlatExchangeFeeRate(0.004m)
                        .Premium(1)
                        .Build();

                    var result = runner.Run(100, DateProvider.MinDate, DateProvider.MaxDate);
                    lastIncome = (double)result.TotalCorrectedBalance - 100;
                }
                return lastIncome;
            }
            return 0;
        }
    }
}
