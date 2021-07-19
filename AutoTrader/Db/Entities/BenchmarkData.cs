using System;
using RethinkDb.Driver.Extras.Dao;

namespace AutoTrader.Db.Entities
{
    public class BenchmarkData : Document<Guid>
    {
        public double Profit { get; set; }
        public string Data { get; set; }
    }
}
