using System;
using AutoTrader.Db.Entities;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class BenchmarkBot : TradingBotBase, ITradingBot
    {
        public static double MaxBenchProfit { get; set; } = double.MinValue;

        public override string Name => nameof(BenchmarkBot);
        public override Predicate<IIndexedOhlcv> BuyRule =>
            Rule.Create(c => c.IsEmaBullish(16) && c.IsEmaBullish(24) && c.IsEmaBearish(96) && c.IsBearish()).
            And(c => c.IsAboveEma(new Random().Next(0, 100)) && c.IsBelowEma(new Random().Next(0, 100))).
            And(c => c.Get<RateOfChange>(new Random().Next(0, 100))[c.Index].Tick > MinRateOfChange);
        public override Predicate<IIndexedOhlcv> SellRule => 
            Rule.Create(c => c.IsBullish() && c.IsEmaBullish(48) && c.IsEmaBullish(96)).
            And(c => c.IsAboveEma(24) && c.IsAboveEma(96)).
            And(c => c.Get<RateOfChange>(24)[c.Index].Tick > MinRateOfChange);

        public BenchmarkBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }

        public override SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade) => SellType.None;
    }
}
