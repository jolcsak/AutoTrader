using System;
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

        protected static int ShortStopLossPercentage = -25;

        protected static int LongStopLossPercentage = -35;

        protected static int ShortTradeMaxAgeInHours = 24;

        protected static int LongTradeMaxAgeInHours = 24 * 4;

        protected DateTime lastBuy = DateTime.MinValue;


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

            if (PreviousPrice?.SellPrice != ActualPrice?.SellPrice || PreviousPrice?.BuyPrice != ActualPrice?.BuyPrice || isNewPeriod)
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
                        if (BotManager.LastTrade.Date >= lastBuy.AddHours(1))
                        {
                            Logger.Info($"{TargetCurrency}: Buy at {DateTime.Now} : prev={PreviousPrice},curr={ActualPrice}");
                            Logger.Info(BotManager.LastTrade.ToString());
                            Buy(MinBtcTradeAmount, ActualPrice, BotManager.LastTrade.Period);
                            lastBuy = BotManager.LastTrade.Date;
                        }
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
                            Logger.Info($"{TargetCurrency}: Profit sell at price {actualPrice}, yield: {tradeOrder.ActualYield}");
                            Sell(actualPrice, tradeOrder);
                        }
                    }
                    else
                    {
                        bool isShortSell = tradeOrder.Period == TradePeriod.Short && (tradeOrder.ActualYield < ShortStopLossPercentage || tradeOrder.BuyDate.AddHours(ShortTradeMaxAgeInHours) < DateTime.Now);
                        bool isLongSell = tradeOrder.Period == TradePeriod.Long && (tradeOrder.ActualYield < LongStopLossPercentage || tradeOrder.BuyDate.AddHours(LongTradeMaxAgeInHours) < DateTime.Now);
                        if (isShortSell || isLongSell)
                        {
                            Logger.Warn($"{TargetCurrency}: Loss sell at price {actualPrice}, yield: {tradeOrder.ActualYield:N2}");
                            Sell(actualPrice, tradeOrder);
                        }
                    }
                }
            }
            return true;
        }

        private void Sell(ActualPrice actualPrice, TradeOrder tradeOrder)
        {
            Logger.Info(tradeOrder.ToString());
            if (BotManager.LastTrade != null)
            {
                Logger.Info(BotManager.LastTrade.ToString());
            }

            if (Sell(actualPrice.BuyPrice, tradeOrder))
            {
                Logger.Info("Sold.");
            }
            {
                Logger.Err("Sell failed!");
            }
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
