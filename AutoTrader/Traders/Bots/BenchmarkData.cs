using System;
using System.Collections.Concurrent;
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

        public ConcurrentDictionary<string, int> Sequence { get; set; } = new ConcurrentDictionary<string, int>();
        
        public int Next(int high, string callerName)
        {
            int next;
            while (!Sequence.TryGetValue(callerName, out next))
            {
                next = high >= 0 ? rnd.Next(high - 1) + 1 : -1;
                Sequence.TryAdd(callerName, next);
            }
            return next;
        }

        public Predicate<IIndexedOhlcv> Next(Predicate<IIndexedOhlcv> subRule, string callerName)
        {
            Next(-1, callerName);
            return subRule;
        }
    }
}
