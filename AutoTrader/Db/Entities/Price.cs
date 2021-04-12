using System;
using RethinkDb.Driver.Extras.Dao;

namespace AutoTrader.Db.Entities
{
    public class Price : Document<Guid>
    {
        public DateTime Time { get; set; }
        public string Currency { get; set; }
        public double Value { get; set; }

        public Price()
        {
        }

        public Price(DateTime time, string currency, double value)
        {
            Time = time;
            Currency = currency;
            Value = value;
        }
    }
}
