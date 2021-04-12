using System;
using RethinkDb.Driver.Extras.Dao;

namespace AutoTrader.Db.Entities
{
    public class LastPrice : Document<Guid>
    {
        public string Currency { get; set; }
        public double Price { get; set; }        
        public double Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
