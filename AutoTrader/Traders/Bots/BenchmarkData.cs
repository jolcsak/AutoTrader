using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class SequenceItem
    {
        public int Value { get; private set; }
        public string Caller { get; private set; }

        public SequenceItem(int value, string caller)
        {
            Value = value;
            Caller = caller;
        }
    }

    public class BenchmarkData
    {
        private Random rnd = new Random();

        public IDictionary<string, int> Sequence { get; set; } = new Dictionary<string, int>();
        
        public int Next(int high, string callerName)
        {
            if (!Sequence.ContainsKey(callerName))
            {
                int next = high >= 0 ? rnd.Next(high - 1) + 1 : -1;
                Sequence.Add(callerName, next);
            }
            return Sequence[callerName];
        }

        public Predicate<IIndexedOhlcv> Next(Predicate<IIndexedOhlcv> subRule, string callerName)
        {
            Next(-1, callerName);
            return subRule;
        }
    }
}
