using System;
using System.Collections.Generic;
using Trady.Core.Infrastructure;
using Trady.Core.Period;

namespace AutoTrader.Traders.Trady
{
    public interface INiceHashImporter
    {
        IList<IOhlcv> Import(string symbol, DateTime startTime, DateTime endTime, PeriodOption period = PeriodOption.Hourly);
    }
}