using System;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using AutoTrader.Log;
using AutoTrader.Utils;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        public const string BTC = "BTC";
        protected const double MIN_BTC_SELL_AMOUNT = 0.000001;

        protected double previousPrice = double.MaxValue;
        protected double changeRatio;
        protected double buyRatio;
        protected double sellRatio;

        protected static double minBtcTradeAmount = 0.0001;
        protected static double btcBalance = 0.001;

        public BtcTrader(string targetCurrency)
        {
            TargetCurrency = targetCurrency;
        }

        protected override ITradeLogger Logger => TradeLogManager.GetLogger(BTC + "->" + TargetCurrency);

        protected double MaxPeriodPrice
        {
            get
            {
                lock (PastPrices)
                {
                    if (PastPrices.Any())
                    {
                        return PastPrices.ToList().Max();
                    }
                    return 0;
                }
            }
        }

        protected double MinPeriodPrice
        {
            get
            {
                lock (PastPrices) {
                    if (PastPrices.Any())
                    {
                        return PastPrices.ToList().Min();
                    }
                    return 0;
                }
            }
        }

        public override ActualPrice GetandStoreCurrentOrders()
        {
            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                Logger.Err("No orderbook returned!");
                return null;
            }

            var actualOrder = new ActualPrice { Currency = TargetCurrency, Price = orderBooks.sell[0][0], Amount = orderBooks.sell[0][1] };
            //Store.Prices.ClearOldPrices();
            Store.Prices.Save(new Price(DateTime.Now, TargetCurrency, actualOrder.Price));

            Logger.Info($"{TargetCurrency} -> price: {actualOrder.Price}, amount: {actualOrder.Amount}");

            return actualOrder;
        }

        public override void Trade()
        {
            double btcBalance = GetBTCBalance();

            if (btcBalance == 0)
            {
                return;
            }

            Logger.LogBalance(BTC, btcBalance);

            var lastPrice = Store.Prices.GetLastPriceForTrader(this);

            if (lastPrice == null)
            {
                return;
            }

            double actualPrice = lastPrice.Value;
            double actualAmount = 10;

            bool hasChanged = previousPrice == double.MaxValue;
            if (hasChanged)
            {
                previousPrice = actualPrice;
            }

            lock (PastPrices)
            {
                if (!PastPrices.Any())
                {
                    PastPrices = Store.Prices.GetPricesForTrader(this, DateTime.MinValue).Select(p => p.Value).ToList();
                    Sma = new SmaProvider().GetSma(PastPrices);
                    Ao = new AoProvider().GetAo(PastPrices);
                    PastPrices = PastPrices.Skip(PastPrices.Count - Ao.Count).ToList();
                    Sma = Sma.Skip(Sma.Count - Ao.Count).ToList();
                }
            }

            changeRatio = actualPrice / previousPrice;
            buyRatio = actualPrice / MinPeriodPrice;
            sellRatio = actualPrice / MaxPeriodPrice;

            if (TradeSettings.CanBuy && btcBalance >= minBtcTradeAmount)
            {
                hasChanged |= Buy(minBtcTradeAmount, actualPrice, actualAmount);
            }

            hasChanged |= Sell(actualPrice);
            previousPrice = actualPrice;

            if (hasChanged)
            {
                Logger.LogTradeOrders(AllTradeOrders, TargetCurrency, actualPrice);
                Logger.LogCurrency(TargetCurrency, actualPrice, actualAmount, MinPeriodPrice, MaxPeriodPrice, buyRatio, sellRatio);
                Logger.Info($"Change: {changeRatio}, Cur: {actualPrice} x {actualAmount}");
            }
        }

        private bool Buy(double btc, double actualPrice, double actualAmount)
        {
            bool buy = Ao.Previous()?.Value < 0 && Ao.Last().Value > 0;
            buy |= Ao.Last().Value > 0 && Ao.Previous(2).Color == AoColor.Green && Ao.Previous().Color == AoColor.Red && Ao.Last().Color == AoColor.Green;
            buy |= Ao.Last().Value < 0 && Ao.Last().Value > Ao.Previous().Value && Ao.Previous().Color == AoColor.Red && Ao.Last().Color == AoColor.Green;

            if (buy)
            {
                Logger.Info($"Time to buy at price {actualPrice}, amount: {btc}");
                StoreTradeOrder(actualPrice, btc, actualPrice * 0.005, TargetCurrency);
                btcBalance -= btc + (actualPrice * 0.005);
                //PastPrices.Clear();
            }

            return true;
        }

        private bool Sell(double actualPrice)
        {
            if (actualPrice >= previousPrice)
            {
                return false;
            }

            if (actualPrice < MaxPeriodPrice * TradeSettings.SellRatio)
            {
                Logger.Info($"Time to sell at price {actualPrice}");
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.Type == TradeOrderType.OPEN))
                {
                    if (actualPrice >= (tradeOrder.Price * TradeSettings.MinSellYield) + tradeOrder.Fee)
                    {
                        Logger.Info($"Time to sell at price {actualPrice}, amount: {tradeOrder.Amount}, buy price: {tradeOrder.Price}, yield: {actualPrice / tradeOrder.Price * 100}%");
                        tradeOrder.Type = TradeOrderType.CLOSED;
                        tradeOrder.SellPrice = actualPrice;
                        tradeOrder.SellDate = DateTime.Now;
                        btcBalance += actualPrice;
                        Store.OrderBooks.SaveOrUpdate(tradeOrder);
                    }
                }
            }

            return true;
        }

        protected double GetBTCBalance()
        {
            //IDictionary<string, double> myBalances = NiceHashApi.GetBalances();
            //return myBalances.ContainsKey(BTC) ? myBalances[BTC] : 0;
            return btcBalance;
        }
    }
}
