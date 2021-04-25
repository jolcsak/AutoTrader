using System;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Agents;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        public const string BTC = "BTC";

        protected static double minBtcTradeAmount = 0.0001; 

        protected DateTime lastPriceDate = DateTime.MinValue;
        protected double actualPrice;
        protected double actualAmount;
        protected double previousPrice = double.MaxValue;
        protected double changeRatio;

        public BtcTrader(string targetCurrency) : base()
        {
            TargetCurrency = targetCurrency;
            AoAgent = new AoAgent(GraphCollection);
        }

        protected override ITradeLogger Logger => TradeLogManager.GetLogger(BTC + "->" + TargetCurrency);

        public override ActualPrice GetAndStoreCurrentOrders()
        {
            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                Logger.Err("No orderbook returned!");
                return null;
            }

            var actualOrder = new ActualPrice { Currency = TargetCurrency, Price = orderBooks.sell[0][0], Amount = orderBooks.sell[0][1] };

            LastPrice lastPrice = Store.LastPrices.GetLastPriceForTrader(this) ?? new LastPrice { Currency = TargetCurrency };
            lastPrice.Price = actualOrder.Price;
            lastPrice.Amount = actualOrder.Amount;
            lastPrice.Date = DateTime.Now;

            Store.Prices.Save(new Price(DateTime.Now, TargetCurrency, actualOrder.Price));
            Store.LastPrices.SaveOrUpdate(lastPrice);

            Logger.Info($"{TargetCurrency} price: {actualOrder.Price}, amount: {actualOrder.Amount}");

            return actualOrder;
        }

        public override void Trade()
        {
            double btcBalance = GetBTCBalance();

            //var r = NiceHashApi.GetOrder("ENJBTC", "1a1365e3-891a-4ba9-9ca6-67f3150f2d16");

            OrderTrade orderResponseSell = NiceHashApi.Order("ENJBTC", isBuy: false, r.qty - r.fee);

            if (btcBalance == 0)
            {
                return;
            }

            Logger.LogBalance(BTC, btcBalance);

            var lastPrice = Store.LastPrices.GetLastPriceForTrader(this);


            if (lastPrice == null || lastPrice.Date == lastPriceDate)
            {
                return;
            }

            actualPrice = lastPrice.Price;
            actualAmount = lastPrice.Amount;
            lastPriceDate = lastPrice.Date;

            AoAgent.Refresh(actualPrice);

            if (previousPrice == double.MaxValue)
            {
                previousPrice = actualPrice;
            }

            changeRatio = actualPrice / previousPrice;

            bool hasChanged = false;
            if (TradeSettings.CanBuy && btcBalance >= minBtcTradeAmount)
            {
                hasChanged |= Buy(minBtcTradeAmount, actualPrice, actualAmount);
            }

            hasChanged |= Sell(actualPrice);
            previousPrice = actualPrice;

            RefreshOrderBooksPrices();

            if (hasChanged)
            {
                Logger.LogTradeOrders(AllTradeOrders, TargetCurrency, actualPrice);
                Logger.Info($"Change: {changeRatio}, Cur: {actualPrice} x {actualAmount}");
            }

            Logger.LogCurrency(this, actualPrice, actualAmount);
        }

        private bool Buy(double amount, double actualPrice, double actualAmount)
        {
            if (AoAgent.IsBuy())
            {
                Logger.Info($"Time to buy at price {actualPrice}, amount: {amount}");

                if (actualAmount > amount)
                {
                    var orderResponse = NiceHashApi.Order(TargetCurrency + "BTC", isBuy: true, amount);
                    if (orderResponse.state == "FULL")
                    {
                        var r = NiceHashApi.GetOrder(TargetCurrency + "BTC", orderResponse.orderId);
                        if (r != null)
                        {
                            StoreTradeOrder(orderResponse.orderId, actualPrice, amount, r.qty, r.fee, TargetCurrency);
                        } else
                        {
                            Logger.Err($"BUY: Can't query order with market: {TargetCurrency}, id: {orderResponse.orderId}");
                        }
                    }
                }   
                else
                {
                    Logger.Warn($"Buy cancelled because actualAmount: {actualAmount} < amount: {amount}!");
                }
            }
            return true;
        }

        private bool Sell(double actualPrice)
        {
            if (AoAgent.IsSell())
            {
                Logger.Info($"Time to sell at price {actualPrice}");
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.Type == TradeOrderType.OPEN))
                {
                    if (actualPrice >= (tradeOrder.Price * TradeSettings.MinSellYield) + tradeOrder.Fee)
                    {
                        Logger.Info($"Time to sell at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%");
                        OrderTrade orderResponse = NiceHashApi.Order(TargetCurrency + "BTC", isBuy: false, tradeOrder.TargetAmount - tradeOrder.Fee);
                        if (orderResponse.state == "FULL")
                        {
                            tradeOrder.Type = TradeOrderType.CLOSED;
                            tradeOrder.SellPrice = actualPrice;
                            tradeOrder.SellDate = DateTime.Now;
                            Store.OrderBooks.SaveOrUpdate(tradeOrder);
                        }
                    }
                }
            }
            return true;
        }

        protected double GetBTCBalance()
        {
            var myBalances = NiceHashApi.GetBalances();
            return myBalances.ContainsKey(BTC) ? myBalances[BTC] : 0;
        }

        private void RefreshOrderBooksPrices()
        {
            foreach (TradeOrder tradeOrder in TradeOrders.Where(to => to.ActualPrice != actualPrice))
            {
                if (actualPrice != tradeOrder.ActualPrice)
                {
                    tradeOrder.ActualPrice = actualPrice;
                    Store.OrderBooks.SaveOrUpdate(tradeOrder);
                }
            }
        }
    }
}
