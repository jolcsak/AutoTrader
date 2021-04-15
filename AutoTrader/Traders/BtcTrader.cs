using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        public const string BTC = "BTC";
        protected const double MIN_BTC_SELL_AMOUNT = 0.000001;


        protected double actualPrice;
        protected double actualAmount;
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
            Store.LastPrices.Save(new LastPrice { Currency = TargetCurrency, Price = actualOrder.Price, Amount = actualOrder.Amount, Date = DateTime.Now });

            Logger.Info($"{TargetCurrency} price: {actualOrder.Price}, amount: {actualOrder.Amount}");

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

            var lastPrice = Store.LastPrices.GetLastPriceForTrader(this);

            if (lastPrice == null)
            {
                return;
            }

            actualPrice = lastPrice.Price;
            actualAmount = lastPrice.Amount;

            bool hasChanged = previousPrice == double.MaxValue;
            if (hasChanged)
            {
                previousPrice = actualPrice;
            }

            lock (PastPrices)
            {
                if (pastPrices == null)
                {
                    pastPrices = new ObservableCollection<double>(Store.Prices.GetPricesForTrader(this, DateTime.MinValue).Select(p => p.Value));
                    smaProvider.SetData(pastPrices);
                    aoProvider.SetData(pastPrices);
                    sma = smaProvider.Sma.Skip(smaProvider.Sma.Count - Ao.Count).ToList();
                    pp = pastPrices.Skip(pastPrices.Count - Ao.Count).ToList();
                    pastPrices.CollectionChanged += PastPricesChanged;
                    Logger.LogCurrency(TargetCurrency, actualPrice, actualAmount, MinPeriodPrice, MaxPeriodPrice, buyRatio, sellRatio);
                }
                else
                {                    
                    pastPrices.Add(actualPrice);
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

            foreach (TradeOrder tradeOrder in TradeOrders.Where(to => to.ActualPrice != actualPrice))
            {
                if (actualPrice != tradeOrder.ActualPrice)
                {
                    tradeOrder.ActualPrice = actualPrice;
                    Store.OrderBooks.SaveOrUpdate(tradeOrder);
                }
            }

            if (hasChanged)
            {
                Logger.LogTradeOrders(AllTradeOrders, TargetCurrency, actualPrice);
                Logger.Info($"Change: {changeRatio}, Cur: {actualPrice} x {actualAmount}");
            }

            if (aoProvider.HasChanged)
            {
                Logger.LogCurrency(TargetCurrency, actualPrice, actualAmount, MinPeriodPrice, MaxPeriodPrice, buyRatio, sellRatio);
            }
        }

        private void PastPricesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            pp.Add(actualPrice);
            sma.Add(smaProvider.Current);
        }

        private bool Buy(double btc, double actualPrice, double actualAmount)
        {
            if (Ao.LastOrDefault()?.Buy == true)
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
            if (Ao.LastOrDefault()?.Sell == true)
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
