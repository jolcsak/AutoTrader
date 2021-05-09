﻿using System;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        public const string BTC = "BTC";

        protected DateTime lastUpdate = DateTime.MinValue;

        public static double MinBtcTradeAmount = 0.00024;

        protected double actualPrice;
        protected double actualAmount;
        protected double previousPrice = double.MaxValue;
        protected double changeRatio;
        private double lastBotPrice  = double.MaxValue;

        public BtcTrader(string targetCurrency) : base()
        {
            TargetCurrency = targetCurrency;
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
            bool isChanged = lastPrice.Price != actualOrder.Price || lastPrice.Amount != actualOrder.Amount;
            if (isChanged)
            {
                lastPrice.Price = actualOrder.Price;
                lastPrice.Amount = actualOrder.Amount;
                lastPrice.Date = DateTime.Now;

                Store.Prices.Save(new Price(DateTime.Now, TargetCurrency, actualOrder.Price));
                Store.LastPrices.SaveOrUpdate(lastPrice);
            }

            Logger.Info($"{TargetCurrency} price: {actualOrder.Price}, amount: {actualOrder.Amount}" + (isChanged ? " - CHANGED" : ""));

            return actualOrder;
        }

        public override void Trade(bool canBuy)
        {
            double btcBalance = RefreshBalance();

            //if (btcBalance == 0)
            //{
            //    return;
            //}

            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                Logger.Err("No orderbook returned!");
                return;
            }

            var actualOrder = new ActualPrice { Currency = TargetCurrency, Price = orderBooks.sell[0][0], Amount = orderBooks.sell[0][1] };

            actualPrice = actualOrder.Price;
            actualAmount = actualOrder.Amount;
            LastPriceDate = DateTime.Now;

            if (lastBotPrice == double.MaxValue)
            {
                lastBotPrice = actualPrice;
            }

            if (previousPrice != actualPrice)
            {
                bool isSpike = actualPrice.IsSpike(lastBotPrice);
                if (isSpike)
                {
                    Logger.Info($"{TargetCurrency} => Spike at {DateTime.Now} : prev={previousPrice},curr={actualPrice}");
                }
                if (lastUpdate.AddMinutes(30) < DateTime.Now || isSpike)
                {
                    BotManager.Refresh(actualOrder);
                    lastUpdate = DateTime.Now;
                    lastBotPrice = actualPrice;
                }

                if (TradeSettings.CanBuy && canBuy && btcBalance >= MinBtcTradeAmount)
                {
                    if (BotManager.IsBuy)
                    {
                        Logger.Info($"{TargetCurrency}: Buy at {DateTime.Now} : prev={previousPrice},curr={actualPrice}");
                        Buy(MinBtcTradeAmount, actualPrice, actualAmount);
                    }
                }

                Sell(actualPrice);
            }

            if (previousPrice == double.MaxValue)
            {
                previousPrice = actualPrice;
            }

            changeRatio = actualPrice / previousPrice;
            previousPrice = actualPrice;

            SaveOrderBooksPrices();

            Logger.Info($"Change: {changeRatio}, Cur: {actualPrice} x {actualAmount}");
            Logger.LogTradeOrders(AllTradeOrders);
            Logger.LogCurrency(this, actualPrice, actualAmount);
        }

        private bool Sell(double actualPrice)
        {
            if (BotManager.IsSell)
            {
                Logger.Info($"{TargetCurrency}: Time to sell at price {actualPrice}");
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.Type == TradeOrderType.OPEN))
                {
                    Logger.Info($"{TargetCurrency}: Buy price: {tradeOrder.Price}, Actual Price: {actualPrice},  Yield: {actualPrice / tradeOrder.Price:N6}");
                    if (actualPrice >= (tradeOrder.Price * TradeSettings.MinSellYield))
                    {
                        if (Sell(actualPrice, tradeOrder))
                        {
                            Logger.Info("Sold.");
                        }
                        {
                            Logger.Err("Sell failed!");
                        }
                    }
                    else
                    {
                        Logger.Info("Yield too low.");
                    }
                }
            }
            return true;
        }

        protected override double GetBalance()
        {
            return NiceHashApi.GetBalance(BTC);
        }

        private void SaveOrderBooksPrices()
        {
            foreach (TradeOrder tradeOrder in TradeOrders.Where(to => to.Type == TradeOrderType.OPEN && to.ActualPrice != actualPrice))
            {
                tradeOrder.ActualPrice = actualPrice;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
            }
        }
    }
}
