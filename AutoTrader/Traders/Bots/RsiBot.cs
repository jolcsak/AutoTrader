using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class RsiBot : TradingBotBase
    {
        public const int OVERBOUGHT = 80;
        public const int OVERSOLD = 20;

        public override string Name => nameof(RsiBot);
        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.IsRsiOversold() && !c.Next?.IsRsiOversold() == true).
            And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);//.And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));
        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.IsRsiOverbought() && !c.Next?.IsRsiOverbought() == true).And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);

        public RsiBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }        
    }
}
