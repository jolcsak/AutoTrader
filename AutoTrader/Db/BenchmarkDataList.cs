using AutoTrader.Db.Entities;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Db
{
    public class BenchmarkDataList : AutoTraderStore<BenchmarkData, BenchmarkDataList>
    {
        public BenchmarkDataList() : base()
        {
        }
        public BenchmarkData GetBenchmarkData()
        {
            var benchmarkDataList = Table.Limit(1).RunResult<IList<BenchmarkData>>(conn);
            return benchmarkDataList.FirstOrDefault() ?? new BenchmarkData();
        }
    }
}
