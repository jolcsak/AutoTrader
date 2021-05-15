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

        public static double MinBtcTradeAmount = 0.00025;
      
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

            var actualOrder = new ActualPrice(TargetCurrency, orderBooks);

            LastPrice lastPrice = Store.LastPrices.GetLastPriceForTrader(this) ?? new LastPrice { Currency = TargetCurrency };
            bool isChanged = lastPrice.Price != actualOrder.BuyPrice || lastPrice.Amount != actualOrder.BuyAmount;
            if (isChanged)
            {
                lastPrice.Price = actualOrder.BuyPrice;
                lastPrice.Amount = actualOrder.BuyAmount;
                lastPrice.Date = DateTime.Now;

                Store.Prices.Save(new Price(DateTime.Now, TargetCurrency, actualOrder.BuyPrice));
                Store.LastPrices.SaveOrUpdate(lastPrice);
            }

            Logger.Info($"{TargetCurrency} price: {actualOrder.BuyPrice}, amount: {actualOrder.BuyAmount}" + (isChanged ? " - CHANGED" : ""));

            return actualOrder;
        }

        public override void Trade(bool canBuy)
        {
            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                Logger.Err("No orderbook returned!");
                return;
            }

            ActualPrice = new ActualPrice(TargetCurrency, orderBooks);

            LastPriceDate = DateTime.Now;

            bool isNewPeriod = lastUpdate.AddMinutes(60) < LastPriceDate;

            if (PreviousPrice?.BuyPrice != ActualPrice?.BuyPrice|| isNewPeriod)
            {
                double btcBalance = RefreshBalance();

                BotManager.Refresh(ActualPrice, isNewPeriod);

                if (isNewPeriod)
                {
                    lastUpdate = DateTime.Now;
                }

                if (TradeSettings.CanBuy && canBuy && btcBalance >= MinBtcTradeAmount)
                {
                    if (BotManager.LastTrade?.Type == TradeType.Buy)
                    {
                        Logger.Info($"{TargetCurrency}: Buy at {DateTime.Now} : prev={PreviousPrice},curr={ActualPrice}");
                        Logger.Info(BotManager.LastTrade.ToString());
                        Buy(MinBtcTradeAmount, ActualPrice, BotManager.LastTrade.Period);
                    }
                }

                Sell(ActualPrice);

                SaveOrderBooksPrices();
            }

            PreviousPrice = ActualPrice;

            Logger.LogTradeOrders(AllTradeOrders);
            Logger.LogCurrency(this, ActualPrice);
        }

        private bool Sell(ActualPrice actualPrice)
        {
            if (TradeOrders.Any())
            {
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.State == TradeOrderState.OPEN))
                {
                    if (actualPrice.BuyPrice >= (tradeOrder.Price * TradeSettings.MinSellYield))
                    {
                        if (tradeOrder.Period == TradePeriod.Short || (tradeOrder.Period == TradePeriod.Long && BotManager.LastTrade?.Type == TradeType.Sell))
                        {
                            Logger.Info($"{TargetCurrency}: Time to sell at price {actualPrice}");
                            Logger.Info(tradeOrder.ToString());
                            Logger.Info(BotManager.LastTrade.ToString());

                            if (Sell(actualPrice.BuyPrice, tradeOrder))
                            {
                                Logger.Info("Sold.");
                            }
                            {
                                Logger.Err("Sell failed!");
                            }
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
            foreach (TradeOrder tradeOrder in TradeOrders.Where(to => to.State == TradeOrderState.OPEN && to.ActualPrice != ActualPrice.BuyPrice))
            {
                tradeOrder.ActualPrice = ActualPrice.BuyPrice;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
            }
        }
    }
}
