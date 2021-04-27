using RethinkDb.Driver.Extras.Dao;
using System;


namespace AutoTrader.Db.Entities
{
    public class TotalBalance : Document<Guid>
    {
        public double Balance { get; set; }
        public DateTime Date { get; set; }
    }
}
