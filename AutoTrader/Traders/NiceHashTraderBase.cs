using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;
using OrderBooks = AutoTrader.Api.OrderBooks;

namespace AutoTrader.Traders
{
    public abstract class NiceHashTraderBase : ITrader
    {
        private const string ENTERED = "ENTERED";
        private const string FULL = "FULL";
        private const string CANCELLED = "CANCELLED";
        private const string PARTIAL = "PARTIAL";
        private static string BTC = NiceHashApi.BTC;

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        public static double FiatRate { get; set; }

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

        protected bool IsActualPricesUpdated()
        {
            if (TradingBotManager.IsBenchmarking)
            {
                ActualPrice = new ActualPrice();
                return true;
            }

            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                return false;
            }
            ActualPrice = new ActualPrice(TargetCurrency, orderBooks);
            return true;
        }

        public void Init()
        {
            if (!IsActualPricesUpdated())
            {
                return;
            }
            Logger.LogCurrency(this, ActualPrice);
        }

        public void StoreTradeOrder(TradeOrderType type, string orderId, double price, double amount, double targetAmount, double fee, string currency, TradePeriod period, string botName, TradeOrderState orderState)
        {
            Store.OrderBooks.Save(new TradeOrder(type, orderId, price, amount, targetAmount, currency, fee, TraderId, orderState, period, botName));
        }

        public TradeResult Buy(double amount, ActualPrice actualPrice, TradePeriod period, string bot)
        {
            Logger.Info($"Try to buy {TargetCurrency}");
            if (actualPrice.SellAmount > amount)
            {
                var orderResponse = NiceHashApi.Order(TargetCurrency + BTC, isBuy: true, amount, actualPrice.SellPrice, isMarket: false);
                string state = orderResponse?.state;
                Logger.Info($"State : {state}");

                if (state == FULL || state == PARTIAL)
                {
                    bool isFull = state == FULL;
                    if (StoreBuyFull(amount, period, bot, orderResponse, isFull? TradeOrderState.OPEN : TradeOrderState.OPEN_ENTERED))
                    {
                        return isFull ? TradeResult.DONE : TradeResult.LIMIT;
                    }
                }
                else if (state == ENTERED)
                {
                    StoreTradeOrder(TradeOrderType.LIMIT, orderResponse.orderId, 0 , amount, 0, 0, TargetCurrency, period, bot, TradeOrderState.OPEN_ENTERED);
                    Logger.Info($"LIMIT BUY PLACED {TargetCurrency} : BTC Amount={amount}");
                    return TradeResult.LIMIT;
                }
            }
            else
            {
                Logger.Warn($"Buy cancelled because actualAmount: {actualPrice.SellAmount} < amount: {amount}!");
            }

            return TradeResult.ERROR;
        }

        public bool StoreBuyFull(double amount, TradePeriod period, string bot, OrderTrade orderResponse, TradeOrderState state = TradeOrderState.OPEN)
        {
            var r = NiceHashApi.GetOrderSummary(TargetCurrency + BTC, orderResponse.orderId);
            if (r != null)
            {
                StoreTradeOrder(TradeOrderType.LIMIT, orderResponse.orderId, r.price, r.sndQty, r.qty, r.fee, TargetCurrency, period, bot, state);
                Logger.Info($"BUY EXECUTED -> {TargetCurrency} : Price={r.price}, Amount={amount}, Qty={r.qty}, SecQty={r.sndQty}");
                return true;
            }
            else
            {
                Logger.Err($"BUY: Can't query order with market: {TargetCurrency}, id: {orderResponse.orderId}");
                return false;
            }
        }

        public bool UpdateBuyFull(TradeOrder tradeOrder, OrderTrade orderResponse, TradeOrderState state = TradeOrderState.OPEN)
        {
            var r = NiceHashApi.GetOrderSummary(TargetCurrency + BTC, orderResponse.orderId);
            Logger.Info($"{TargetCurrency + BTC} fully processed: {orderResponse.orderId}, executedQty={orderResponse.executedQty}, executedSndQty={orderResponse.executedSndQty}, origQty={orderResponse.origQty}, origSndQty={orderResponse.origSndQty}, price={orderResponse.price}, state={orderResponse.state}, side={orderResponse.side}, owner={orderResponse.owner}, type={orderResponse.type}, market={orderResponse.market}");
            Logger.Info($"{TargetCurrency + BTC} fully processed summary: {orderResponse.orderId} <=> {r.id}, qty={r.qty}, fee={r.fee}, sndQty={r.sndQty}, r.price={r.price}");
            if (r != null)
            {
                tradeOrder.Fee += r.fee;

                string tradeType = "SELL";
                if (tradeOrder.State == TradeOrderState.ENTERED)
                {
                    tradeOrder.SellBtcAmount = orderResponse.executedSndQty;
                    tradeOrder.SellPrice = r.price;
                    tradeOrder.SellDate = DateTime.Now;
                }
                if (tradeOrder.State == TradeOrderState.OPEN_ENTERED)
                {
                    tradeType = "BUY";
                    tradeOrder.Amount = r.sndQty;
                    tradeOrder.TargetAmount = r.qty;
                    tradeOrder.Price = r.price;
                    tradeOrder.BuyDate = DateTime.Now;
                }

                tradeOrder.State = state;

                Store.OrderBooks.SaveOrUpdate(tradeOrder);
                Logger.Info($"LIMIT {tradeType} EXECUTED -> {TargetCurrency} : Price={r.price}, Amount={tradeOrder.SellBtcAmount}, Qty={r.qty}, SecQty={r.sndQty}");
                return true;
            }
            else
            {
                Logger.Err($"BUY: Can't query order with market: {TargetCurrency}, id: {orderResponse.orderId}");
                return false;
            }
        }

        public TradeResult Sell(double actualPrice, TradeOrder tradeOrder, bool isMarket = false)
        {
            string msg = $" at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%";
            Logger.Info($"Try to sell {msg}");
            OrderTrade orderResponse = NiceHashApi.Order(tradeOrder.Currency + BTC, isBuy: false, tradeOrder.TargetAmount - tradeOrder.Fee, actualPrice, isMarket);
            string state = orderResponse?.state;
            if (state == FULL || state == ENTERED)
            {
                bool isMarketSell = state == FULL;
                tradeOrder.SellOrderId = orderResponse.orderId;
                tradeOrder.State = isMarketSell ? TradeOrderState.CLOSED : TradeOrderState.ENTERED;
                tradeOrder.SellBtcAmount = orderResponse.executedSndQty;
                tradeOrder.SellPrice = actualPrice;
                tradeOrder.SellDate = DateTime.Now;
                
                Store.OrderBooks.SaveOrUpdate(tradeOrder);

                string sellMsg = isMarketSell ? "Sold" : "Limit sell placed";
                Logger.Info(sellMsg + msg);
                return isMarketSell ? TradeResult.DONE : TradeResult.LIMIT;
            }
            else
            {
                Logger.Err("Sell failed!");
            }
            return TradeResult.ERROR;
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

        protected abstract double GetBalance();

        public double RefreshBalance()
        {
            var balance = GetBalance();
            Logger.LogBalance(balance);
            return balance;
        }

        public bool CancelLimit(TradeOrder tradeOrder, TradeOrderState cancelState)
        {
            string id = tradeOrder.SellOrderId ?? tradeOrder.BuyOrderId;
            var r = NiceHashApi.CancelOrder(tradeOrder.Currency + BTC, id);
            if (r?.state == CANCELLED)
            {
                tradeOrder.State = cancelState;
                tradeOrder.SellPrice = tradeOrder.Price;
                tradeOrder.SellDate = DateTime.Now;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
                return true;
            }
            return false;
        }
    }
}
