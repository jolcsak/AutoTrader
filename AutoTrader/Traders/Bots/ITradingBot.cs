using System;
using System.Collections.Generic;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public interface ITradingBot : ISeller
    {
        string Name { get; }

        Predicate<IIndexedOhlcv> BuyRule { get; }
        Predicate<IIndexedOhlcv> SellRule { get; }

        List<TradeItem> RefreshAll();
    }
}
