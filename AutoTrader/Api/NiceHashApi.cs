using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using AutoTrader.Api.Objects;
using AutoTrader.Config;
using Newtonsoft.Json;

namespace AutoTrader.Api
{
    public class NiceHashApi
    {
        private static string TEST_URL_ROOT = "https://api-test.nicehash.com";
        private static string PROD_URL_ROOT = "https://api2.nicehash.com";

        private static readonly long unixStartTicks = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;

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

        public double GetBalance(string currency)
        {
            var balance = Get<Balance>($"/main/api/v2/accounting/account2/{currency}", true, ServerTime);
            return balance == null ? 0 : balance.available;
        }

        public Symbols GetExchangeSettings()
        {
            return Get<Symbols>("/exchange/api/v2/info/status");
        }

        public TotalBalance GetTotalBalance(string fiat = "EUR")
        {
            return Get<TotalBalance>($"/main/api/v2/accounting/accounts2?fiat={fiat}", true, ServerTime);
        }

        public OrderBooks GetOrderBook(string currencyBuy, string currencySell)
        {
            return Get<OrderBooks>($"/exchange/api/v2/orderbook?market={currencyBuy}{currencySell}&limit=1", true, ServerTime, false);
        }

        public HistoricPrice[] GetLastPrices(string market, int limit)
        {
            return Get<HistoricPrice[]>($"/exchange/api/v2/info/trades?market={market}&limit={limit}");
        }

        public CandleStick[] GetCandleSticks(string market, DateTime from, DateTime to, int resolution = 1)
        {
            long fromSec = new DateTimeOffset(from).ToUnixTimeSeconds();
            long toSec = new DateTimeOffset(to).ToUnixTimeSeconds();
            return Get<CandleStick[]>($"/exchange/api/v2/info/candlesticks?market={market}&from={fromSec}&to={toSec}&resolution={resolution}");
        }

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            long unixTimeStampInTicks = unixTime * TimeSpan.TicksPerSecond;
            return new DateTime(unixStartTicks + unixTimeStampInTicks, DateTimeKind.Utc).ToLocalTime();
        }

        public OrderTrade Order(string market, bool isBuy, double amount, double price, bool isMarket)
        {
            string side = isBuy ? "buy" : "sell";
            string type = isMarket ? "market" : "limit";

            if (isBuy && !isMarket)
            {
                amount = amount * (1 / price);
            }

            string amountStr = amount.ToString("F8", CultureInfo.InvariantCulture);
            string priceStr = price.ToString("F8", CultureInfo.InvariantCulture);
            string url = $"/exchange/api/v2/order?market={market}&side={side}&price={priceStr}&type={type}&quantity={amountStr}";
            if (isBuy)
            {
                url += $"&secQuantity={amountStr}";
            }

            return Post<OrderTrade>(url);
        }

        public GetOrderTrade GetOrderSummary(string market, string orderId)
        {
            var trades = Get<GetOrderTrade[]>($"/exchange/api/v2/info/orderTrades?market={market}&orderId={orderId}", true, ServerTime);
            if (trades.Length > 1)
            {
                double sumNumberWeighting = 0;
                double sumSecQty = 0;
                double sumQty = 0;
                double sumFee = 0;
                foreach (GetOrderTrade trade in trades)
                {
                    sumNumberWeighting += trade.price * trade.qty;
                    sumQty += trade.qty;
                    sumSecQty += trade.sndQty;
                    sumFee += trade.fee;
                }
                return new GetOrderTrade
                {
                    orderId = orderId,
                    price = sumNumberWeighting / sumQty,
                    qty = sumQty,
                    sndQty = sumSecQty,
                    fee = sumFee,
                    dir = trades[0].dir,
                    isMaker = trades[0].isMaker,
                    time = trades.Last().time,
                    id = trades.Last().id
                };
            }
            return trades.Length == 1 ? trades[0] : null;
        }

        public OrderTrade GetOrder(string market, string orderId)
        {
            return Get<OrderTrade>($"/exchange/api/v2/info/myOrder?market={market + "BTC"}&orderId={orderId}", true, ServerTime);
        }

        public OrderTrade CancelOrder(string market, string orderId)
        {
            return Delete<OrderTrade>($"/exchange/api/v2/order?market={market}&orderId={orderId}", ServerTime);
        }

        private T Get<T>(string url, bool auth = false, string time = null, bool logErrors = true)
        {
            string response = api.get(url, auth, time, logErrors);
            // {"error_id":"3a71f9a1-be49-46ac-8a8b-862da6b8ad91","errors":[{"code":2001,"message":"Session Time skew detected"}]}
            while (response.Contains("\"code\":2001"))
            {
                QueryServerTime();
                Thread.Sleep(RETRY_PERIOD);
                response = api.get(url, true, ServerTime, logErrors);
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

        private T Delete<T>(string url, string time = null, bool logErrors = true)
        {
            string response = api.delete(url, time, logErrors);
            // {"error_id":"3a71f9a1-be49-46ac-8a8b-862da6b8ad91","errors":[{"code":2001,"message":"Session Time skew detected"}]}
            while (response.Contains("\"code\":2001"))
            {
                QueryServerTime();
                Thread.Sleep(RETRY_PERIOD);
                response = api.delete(url, ServerTime, logErrors);
            }

            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}
