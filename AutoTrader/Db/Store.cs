using RethinkDb.Driver;
using RethinkDb.Driver.Net;

namespace AutoTrader.Db
{
    public class Store
    {
        public static Store Instance { get; private set; }

        public static RethinkDB R = RethinkDB.R;

        public IConnection Con { get; private set; }

        public Prices Prices { get; private set; }

        public OrderBooks OrderBooks { get; private set; }

        private Store()
        {
            Con = R.Connection()
                     .Hostname("jolcsak-nas")
                     .Port(32773)
                     .Timeout(20)
                     .Connect();

            Instance = this;

            Prices = new Prices();
            OrderBooks = new OrderBooks();
        }

        public static Store Connect()
        {
            return Store.Instance ?? new Store();
        }

        ~Store()
        {
            Con?.Dispose();
        }
    }
}
