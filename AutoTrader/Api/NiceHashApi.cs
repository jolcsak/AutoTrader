using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AutoTrader.Config;
using Newtonsoft.Json;

namespace AutoTrader.Api
{
    public class NiceHashApi
    {
        private static string TEST_URL_ROOT = "https://api-test.nicehash.com";
        private static string PROD_URL_ROOT = "https://api2.nicehash.com";

        private const int RETRY_PERIOD = 10;
        private string URL_ROOT;
        private NiceHashConnectApi api;

        public static NiceHashApi Instance { get; private set; }
        public string ServerTime { get; private set; }

        private NiceHashApi(IConfig config)
        {
            URL_ROOT = config.IsProd ? PROD_URL_ROOT : TEST_URL_ROOT;
            api = new NiceHashConnectApi(URL_ROOT, config.OrgId, config.ApiKey, config.ApiSecret);
            Instance = this;
        }

        public static NiceHashApi Create(IConfig config)
        {
            return Instance ?? new NiceHashApi(config);
        }

        public void QueryServerTime()
        {
            ServerTime = Get<ServerTime>("/api/v2/time").serverTime;
        }
        
        public IDictionary<string, double> GetBalances()
        {
            var currenciesObj = Get<Currencies>("/main/api/v2/accounting/accounts2", true, ServerTime);
            return currenciesObj?.currencies != null ? currenciesObj.currencies.Where(c => c.available != 0).ToDictionary(c => c.currency, c => c.available) : new Dictionary<string, double>();
        }

        public Symbols GetExchangeSettings()
        {
            return Get<Symbols>("/exchange/api/v2/info/status");
        }

        public OrderBooks GetOrderBook(string currencyBuy, string currencySell)
        {
            return Get<OrderBooks>($"/exchange/api/v2/orderbook?market={currencyBuy}{currencySell}&limit=1", true, ServerTime);
        }

        public OrderTrade Order(string market, bool isBuy, double amount, double minSecQuantity = 0)
        {
            string side = isBuy ? "buy" : "sell";
            string amountStr = amount.ToString("N8", CultureInfo.InvariantCulture);
            string url = $"/exchange/api/v2/order?market={market}&side={side}&type=market&quantity={amountStr}";
            if (isBuy)
            {
                url += $"&secQuantity={amountStr}";
            }

            return Post<OrderTrade>(url);
        }

        public GetOrderTrade GetOrder(string market, string orderId)
        {
            var trades = Get<GetOrderTrade[]>($"/exchange/api/v2/info/orderTrades?market={market}&orderId={orderId}", true, ServerTime);
            return trades.Length > 0 ? trades[0] : null;
        }

        private T Get<T>(string url, bool auth = false, string time = null)
        {
            string response = api.get(url, auth, time);
            // {"error_id":"3a71f9a1-be49-46ac-8a8b-862da6b8ad91","errors":[{"code":2001,"message":"Session Time skew detected"}]}
            while (response.Contains("\"code\":2001"))
            {
                QueryServerTime();
                Thread.Sleep(RETRY_PERIOD);
                response = api.get(url, true, ServerTime);
            }

            return JsonConvert.DeserializeObject<T>(response);
        }

        private T Post<T>(string url)
        {
            string response = api.post(url, null, ServerTime, true);
            // {"error_id":"3a71f9a1-be49-46ac-8a8b-862da6b8ad91","errors":[{"code":2001,"message":"Session Time skew detected"}]}
            while (response.Contains("\"code\":2001"))
            {
                QueryServerTime();
                Thread.Sleep(RETRY_PERIOD);
                response = api.post(url, null, ServerTime, true);
            }
            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}
