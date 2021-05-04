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

        public LastPrices LastPrices { get; private set; }

        public OrderBooks OrderBooks { get; private set; }

        public TotalBalances TotalBalances { get; private set; }

        public TradeSettings TradeSettings { get; private set; }

        public void SaveSettings()
        {
            if (TradeSetting.Instance.CanSave())
            {
                TradeSetting.Instance = TradeSettings.SaveOrUpdate(TradeSetting.Instance);
            }
        }

        public void LoadSettings()
        {
            TradeSetting.Instance = TradeSettings.GetTradeSettings();
        }

        private Store()
        {
            Con = R.Connection().Hostname("jolcsak-nas").Port(32773).Timeout(20).Connect();

            Instance = this;

            Prices = new Prices();
            OrderBooks = new OrderBooks();
            LastPrices = new LastPrices();
            TotalBalances = new TotalBalances();
            TradeSettings = new TradeSettings();
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
