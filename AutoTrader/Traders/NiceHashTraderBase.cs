﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Agents;

namespace AutoTrader.Traders
{
    public abstract class NiceHashTraderBase : ITrader
    {
        private const string FIAT = "HUF";

        protected static NiceHashApi NiceHashApi => NiceHashApi.Instance;

        protected static Store Store => Store.Instance;

        public string TargetCurrency { get; protected set; }

        protected virtual ITradeLogger Logger => TradeLogManager.GetLogger(GetType());

        public string TraderId => this.GetType().Name;

        public virtual IList<TradeOrder> TradeOrders => Store.OrderBooks.GetOrdersForTrader(this);

        public virtual IList<TradeOrder> AllTradeOrders => Store.OrderBooks.GetAllOrders(this);

        public GraphCollection GraphCollection { get; }
        public double Frequency => GraphCollection.AoProvider.Frequency;
        public double Amplitude => GraphCollection.AoProvider.Amplitude;
        public double Order => GraphCollection.AoProvider != null ? GraphCollection.AoProvider.Frequency * GraphCollection.AoProvider.Amplitude : 0;
        public DateTime LastPriceDate { get; set; } = DateTime.MinValue;

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public NiceHashTraderBase()
        {
            GraphCollection = new GraphCollection(this);
        }

        public virtual void Trade(bool canBuy)
        {
        }

        public virtual ActualPrice GetAndStoreCurrentOrders()
        {
            return null;
        }

        public void StoreTradeOrder(string orderId, double price, double amount, double targetAmount, double fee, string currency)
        {
            Store.OrderBooks.Save(new TradeOrder(orderId, price, amount, targetAmount, currency, fee, TraderId));
        }

        public void Sell(double actualPrice, TradeOrder tradeOrder)
        {
            Logger.Info($"Time to sell at price {actualPrice}, amount: {tradeOrder.TargetAmount}, buy price: {tradeOrder.Price}, sell price: {actualPrice}, yield: {actualPrice / tradeOrder.Price * 100}%");
            OrderTrade orderResponse = NiceHashApi.Order(tradeOrder.Currency + "BTC", isBuy: false, tradeOrder.TargetAmount - tradeOrder.Fee, tradeOrder.Amount);
            if (orderResponse.state == "FULL")
            {
                tradeOrder.Type = TradeOrderType.CLOSED;
                tradeOrder.SellPrice = actualPrice;
                tradeOrder.SellDate = DateTime.Now;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
            }
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

        protected double RefreshBalance()
        {
            var balance = GetBalance();
            Logger.LogBalance(balance);
            return balance;
        }
    }
}
