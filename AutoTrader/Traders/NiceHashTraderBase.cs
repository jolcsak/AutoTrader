using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;

namespace AutoTrader.Traders
{
    public abstract class NiceHashTraderBase : ITrader
    {
        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        protected static Store Store => Store.Instance;

        public string TargetCurrency { get; protected set; }

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());

        public string TraderId => GetType().Name;

        public virtual IList<TradeOrder> TradeOrders => Store.OrderBooks.GetOrdersForTrader(this);

        public virtual IList<TradeOrder> AllTradeOrders => Store.OrderBooks.GetAllOrders(this);

        public ActualPrice ActualPrice { get; set; }

        public ActualPrice PreviousPrice { get; set; } = null;

        public TradingBotManager BotManager { get; }
        public double Order => BotManager.ProjectedIncome;
        public DateTime LastPriceDate { get; set; } = DateTime.MinValue;

        protected TradeSetting TradeSettings => TradeSetting.Instance;


        public NiceHashTraderBase()
        {
            BotManager = new TradingBotManager(this);
        }

        public virtual void Trade(bool canBuy)
        {
        }

        public virtual ActualPrice GetAndStoreCurrentOrders()
        {
            return null;
        }

        public void StoreTradeOrder(string orderId, double price, double amount, double targetAmount, double fee, string currency, TradePeriod period)
        {
            Store.OrderBooks.Save(new TradeOrder(orderId, price, amount, targetAmount, currency, fee, TraderId, period));
        }

        public bool Buy(double amount, ActualPrice actualPrice, TradePeriod period)
        {
            Logger.Info($"Try to buy {TargetCurrency}");
            if (actualPrice.SellAmount > amount)
            {
                var orderResponse = NiceHashApi.Order(TargetCurrency + "BTC", isBuy: true, amount);
                if (orderResponse.state == "FULL")
                {
                    Logger.Info($"{TargetCurrency} successfully bought");
                    var r = NiceHashApi.GetOrder(TargetCurrency + "BTC", orderResponse.orderId);
                    if (r != null)
                    {
                        Logger.Info($"{TargetCurrency} : Price={r.price}, Amount={amount}, Qty={r.qty}, SecQty={r.sndQty}");
                        StoreTradeOrder(orderResponse.orderId, r.price, amount, r.qty, r.fee, TargetCurrency, period);
                        return true;
                    }
                    else
                    {
                        Logger.Err($"BUY: Can't query order with market: {TargetCurrency}, id: {orderResponse.orderId}");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Logger.Warn($"Buy cancelled because actualAmount: {actualPrice.SellAmount} < amount: {amount}!");
                return false;
            }
        }   

        public bool Sell(double actualPrice, TradeOrder tradeOrder)
        {
            Logger.Info($"Time to sell at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%");
            OrderTrade orderResponse = NiceHashApi.Order(tradeOrder.Currency + "BTC", isBuy: false, tradeOrder.TargetAmount - tradeOrder.Fee, tradeOrder.Amount);
            if (orderResponse.state == "FULL")
            {
                tradeOrder.Type = TradeOrderType.CLOSED;
                tradeOrder.SellBtcAmount = orderResponse.executedSndQty;
                tradeOrder.SellPrice = actualPrice;
                tradeOrder.SellDate = DateTime.Now;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
                Logger.Info($"Sold at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%");
                return true;
            }
            else
            {
                Logger.Err("Sell failed!");
            }
            return false;
        }

        public void SellAll(bool onlyProfitable)
        {
            double yield = (TradeSettings.MinSellYield - 1) * 100;
            foreach (TradeOrder tradeOrder in AllTradeOrders.Where(to => to.Type == TradeOrderType.OPEN))
            {
                if (!onlyProfitable || tradeOrder.ActualYield > yield)
                {
                    Sell(tradeOrder.ActualPrice, tradeOrder);
                }
            }
            RefreshBalance();
            Logger.LogTradeOrders(AllTradeOrders);
            Logger.Warn($"All orders are sold.");
        }

        public IList<Price> GetAllPastPrices()
        {
            return Store.Prices.GetPricesForTrader(this, int.MaxValue);
        }

        protected abstract double GetBalance();

        public double RefreshBalance()
        {
            var balance = GetBalance();
            Logger.LogBalance(balance);
            return balance;
        }
    }
}
