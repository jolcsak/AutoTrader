using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Config;
using AutoTrader.Db;
using AutoTrader.Log;
using AutoTrader.Traders;

namespace AutoTrader
{
    public class TraderCollection
    {
        protected const bool IsProd = true;

        protected const string VERSION = "0.4";
        protected const string FIAT = "HUF";

        protected static CultureInfo cultureInfo = CultureInfo.InvariantCulture;
        protected static Store Store => Store.Instance;

        protected ITradeLogger Logger = TradeLogManager.GetLogger("AutoTrader");

        public static List<ITrader> Traders { get; } = new List<ITrader>();

        protected static void CreateTraders(NiceHashApi niceHashApi)
        {
            Traders.Clear();

            Symbols symbols = niceHashApi.GetExchangeSettings();

            foreach (Symbol symbol in symbols.symbols.Where(s => s.baseAsset != BtcTrader.BTC))
            {
                if (!Traders.Any(t => t.TargetCurrency == symbol.baseAsset))
                {
                    Traders.Add(new BtcTrader(symbol.baseAsset));
                }
            }
        }

        public ITrader GetTrader(string targetCurrency)
        {
            return Traders.FirstOrDefault(t => t.TargetCurrency == targetCurrency);
        }

        protected static NiceHashApi GetNiceHashApi()
        {
            Store.Connect();
            return NiceHashApi.Create(IsProd ? ProdConfig.Instance : TestConfig.Instance);
        }

        protected static Tuple<double, double> GetTotalFiatBalance(NiceHashApi niceHashApi)
        {
            Api.Objects.TotalBalance totalBalance = niceHashApi.GetTotalBalance(FIAT);
            var btcCurrency = totalBalance.currencies.FirstOrDefault(c => c.currency == BtcTrader.BTC);
            if (totalBalance?.total != null && btcCurrency != null)
            {
                return new Tuple<double, double>(totalBalance.total.totalBalance, btcCurrency.fiatRate);
            }
            return new Tuple<double, double>(0, 0);
        }
    }
}
