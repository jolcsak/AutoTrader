using RethinkDb.Driver.Extras.Dao;
using System;


namespace AutoTrader.Db.Entities
{
    public class TotalBalance : Document<Guid>
    {
        public double BtcBalance { get; set; }
        public double FiatBalance { get; set; }
        public DateTime Date { get; set; }
    }
}
