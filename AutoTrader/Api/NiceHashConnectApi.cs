using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Net;
using AutoTrader.Log;
using RestSharp;
using Newtonsoft.Json;

namespace AutoTrader.Api
{
    class NiceHashConnectApi
    {
        private const int DEFAULT_TOO_MANY_REQUESTS_RETRY_TIME = 30 * 1000;
        private const int DEFAULT_BAD_GATEWAY_RETRY_TIME = 5 * 1000;
        private const int RETRY_COUNT = 10;

        private string urlRoot;
        private string orgId;
        private string apiKey;
        private string apiSecret;

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());

        public NiceHashConnectApi(string urlRoot, string orgId, string apiKey, string apiSecret)
        {
            this.urlRoot = urlRoot;
            this.orgId = orgId;
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
        }

        private static string HashBySegments(string key, string apiKey, string time, string nonce, string orgId, string method, string encodedPath, string query, string bodyStr)
        {
            List<string> segments = 
                new List<string> { apiKey, time, nonce, null, orgId, null, method, encodedPath, query };

            if (bodyStr?.Length > 0)
            {
                segments.Add(bodyStr);
            }
            return CalcHMACSHA256Hash(JoinSegments(segments), key);
        }
        private static string getPath(string url)
        {
            return url.Split('?')[0];
        }
        private static string getQuery(string url)
        {
            var arrSplit = url.Split('?');
            return arrSplit.Length == 1 ? null : arrSplit[1];
        }

        private static string JoinSegments(List<string> segments)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var segment in segments)
            {
                if (!first)
                {
                    sb.Append("\x00");
                }
                else
                {
                    first = false;
                }

                if (segment != null)
                {
                    sb.Append(segment);
                }
            }
            return sb.ToString();
        }

        private static string CalcHMACSHA256Hash(string plaintext, string salt)
        {
            byte[] baText2BeHashed = Encoding.Default.GetBytes(plaintext),
            baSalt = Encoding.Default.GetBytes(salt);
            byte[] baHashedText = new HMACSHA256(baSalt).ComputeHash(baText2BeHashed);
            return string.Join("", baHashedText.ToList().Select(b => b.ToString("x2")).ToArray());
        }

        public string get(string url, bool logErrors = true)
        {
            return this.get(url, false, null, logErrors);
        }

        public string get(string url, bool auth, string time, bool logErrors)
        {
            var request = new RestRequest(url);
            if (auth)
            {
                AddHeaders(time, request, "GET", url);
            }

            return GetContent(request, Method.GET, logErrors);
        }

        public string post(string url, string payload, string time, bool requestId)
        {
            var request = new RestRequest(url);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-type", "application/json");
            
            if (payload != null)
            {
                request.AddJsonBody(payload);
            }

            AddHeaders(time, request, "POST", url, payload);

            if (requestId)
            {
                request.AddHeader("X-Request-Id", Guid.NewGuid().ToString());
            }

            return GetContent(request, Method.POST);
        }

        public string delete(string url, string time, bool requestId)
        {
            var request = new RestRequest(url);            
            AddHeaders(time, request, "DELETE", url);

            if (requestId)
            {
                request.AddHeader("X-Request-Id", Guid.NewGuid().ToString());
            }

            return GetContent(request, Method.DELETE);
        }

        private string GetContent(RestRequest request, Method method, bool logErrors = true)
        {
            IRestResponse response;
            bool hasValidResponse;
            int retry = RETRY_COUNT;
            do
            {
                hasValidResponse = true;
                response = new RestClient(urlRoot).Execute(request, method);

                if (response == null)
                {
                    Logger.Err("No response from the server!");
                    return string.Empty;
                }
                
                if (!response.IsSuccessful)
                {
                    if (logErrors && response.StatusCode != HttpStatusCode.PreconditionFailed)
                    {
                        Logger.Err($"Request failed: Status={response.StatusCode}, URL={response.ResponseUri}, RespStatus={response.ResponseStatus}, Error={response.ErrorMessage}, Desc={response.StatusDescription}, Content={response.Content}");
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        hasValidResponse = HandleTooManyRequests(response);
                    }
                    if (response.StatusCode == HttpStatusCode.BadGateway)
                    {
                        hasValidResponse = HandleBadGateway(response);
                    }
                }
                retry--;
            } while (!hasValidResponse && retry >= 0);

            return response.Content;
        }

        private bool HandleTooManyRequests(IRestResponse response)
        {
            string timeWindow =
                response.Headers.FirstOrDefault(p => p.Name.Equals("retry-after", StringComparison.OrdinalIgnoreCase))?.Value as string;

            if (timeWindow != null)
            {
                if (int.TryParse(timeWindow, out var time))
                {
                    Logger.Info($"Waiting {time} seconds before retry...");
                    time *= 1000;
                    Thread.Sleep(time);
                }
                else
                {
                    Logger.Err($"retry_after ({timeWindow}) in header not a valid number!");
                    Logger.Info($"Waiting the default {DEFAULT_TOO_MANY_REQUESTS_RETRY_TIME} seconds before retry...");
                    Thread.Sleep(DEFAULT_TOO_MANY_REQUESTS_RETRY_TIME);
                }
            }
            else
            {
                Logger.Err($"retry_after not found in header!");
                Logger.Info($"Waiting the default {DEFAULT_TOO_MANY_REQUESTS_RETRY_TIME} seconds before retry...");
                Thread.Sleep(DEFAULT_TOO_MANY_REQUESTS_RETRY_TIME);
            }

            return false;
        }

        private bool HandleBadGateway(IRestResponse response)
        {
            Thread.Sleep(DEFAULT_BAD_GATEWAY_RETRY_TIME);
            return false;
        }

        private void AddHeaders(string time, RestRequest request, string method, string url, string payload = null)
        {
            string nonce = Guid.NewGuid().ToString();
            string digest = HashBySegments(this.apiSecret, this.apiKey, time, nonce, this.orgId, method, getPath(url), getQuery(url), payload);
            request.AddHeader("X-Time", time);
            request.AddHeader("X-Nonce", nonce);
            request.AddHeader("X-Auth", $"{this.apiKey}:{digest}");
            request.AddHeader("X-Organization-Id", this.orgId);
        }
    }
}
