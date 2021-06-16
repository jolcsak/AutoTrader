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

        public void StoreTradeOrder(TradeOrderType type, string orderId, double price, double amount, double targetAmount, double fee, string currency, TradePeriod period, string botName, TradeOrderState orderState)
        {
            Store.OrderBooks.Save(new TradeOrder(type, orderId, price, amount, targetAmount, currency, fee, TraderId, orderState, period, botName));
        }

        public bool Buy(double amount, ActualPrice actualPrice, TradePeriod period, string bot)
        {
            Logger.Info($"Try to buy {TargetCurrency}");
            if (actualPrice.SellAmount > amount)
            {
                var orderResponse = NiceHashApi.Order(TargetCurrency + "BTC", isBuy: true, amount, actualPrice.SellPrice, isMarket: false);
                string state = orderResponse?.state;

                if (state == "FULL" || state == "ENTERED")
                {
                    if (state == "FULL")
                    {
                        return StoreBuyFull(amount, period, bot, orderResponse);
                    }
                    else if (state == "ENTERED")
                    {
                        Logger.Info($"LIMIT BUY {TargetCurrency} : BTC Amount={amount}");
                        StoreTradeOrder(TradeOrderType.LIMIT, orderResponse.orderId, 0 , amount, 0, 0, TargetCurrency, period, bot, TradeOrderState.OPEN_ENTERED);
                        return true;
                    }
                    return false;
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

        private bool StoreBuyFull(double amount, TradePeriod period, string bot, OrderTrade orderResponse)
        {
            var r = NiceHashApi.GetOrderSummary(TargetCurrency + "BTC", orderResponse.orderId);
            if (r != null)
            {
                Logger.Info($"MARKET BUY {TargetCurrency} : Price={r.price}, Amount={amount}, Qty={r.qty}, SecQty={r.sndQty}");
                StoreTradeOrder(TradeOrderType.LIMIT, orderResponse.orderId, r.price, amount, r.qty, r.fee, TargetCurrency, period, bot, TradeOrderState.OPEN);
                return true;
            }
            else
            {
                Logger.Err($"BUY: Can't query order with market: {TargetCurrency}, id: {orderResponse.orderId}");
                return false;
            }
        }

        public bool Sell(double actualPrice, TradeOrder tradeOrder, bool isMarket = false)
        {
            string msg = $" at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%";
            Logger.Info("Try to sell" + msg);
            OrderTrade orderResponse = NiceHashApi.Order(tradeOrder.Currency + "BTC", isBuy: false, tradeOrder.TargetAmount - tradeOrder.Fee, tradeOrder.Price, isMarket);
            string state = orderResponse?.state;
            if (state == "FULL" || state == "ENTERED")
            {
                bool isMarketSell = state == "FULL";
                tradeOrder.SellOrderId = orderResponse.orderId;
                tradeOrder.State = isMarketSell ? TradeOrderState.CLOSED : TradeOrderState.ENTERED;
                tradeOrder.SellBtcAmount = orderResponse.executedSndQty;
                tradeOrder.SellPrice = actualPrice;
                tradeOrder.SellDate = DateTime.Now;
                
                Store.OrderBooks.SaveOrUpdate(tradeOrder);

                string sellMsg = isMarketSell ? "Sold" : "Limit sell placed";
                Logger.Info(sellMsg + msg);
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
            foreach (TradeOrder tradeOrder in AllTradeOrders.Where(to => to.State == TradeOrderState.OPEN))
            {
                if (!onlyProfitable || tradeOrder.ActualYield > yield)
                {
                    Sell(tradeOrder.ActualPrice, tradeOrder, isMarket: true);
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

        public bool CancelLimit(TradeOrder tradeOrder)
        {
            string id = tradeOrder.SellOrderId ?? tradeOrder.BuyOrderId;
            var r = NiceHashApi.CancelOrder(tradeOrder.Currency + "BTC", id);
            if (r?.state == "CANCELLED")
            {
                tradeOrder.State = TradeOrderState.CANCELLED;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
                return true;
            }
            return false;
        }
    }
}
