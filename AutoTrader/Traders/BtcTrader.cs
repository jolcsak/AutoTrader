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

            var actualOrder = new ActualPrice { Currency = TargetCurrency, BuyPrice = orderBooks.buy[0][0], BuyAmount = orderBooks.buy[0][1], SellPrice = orderBooks.sell[0][0], SellAmount = orderBooks.sell[0][1] };

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
            double btcBalance = RefreshBalance();

            OrderBooks orderBooks = NiceHashApi.GetOrderBook(TargetCurrency, BTC);
            if (orderBooks == null)
            {
                Logger.Err("No orderbook returned!");
                return;
            }

            ActualPrice = new ActualPrice { Currency = TargetCurrency, BuyPrice = orderBooks.buy[0][0], BuyAmount = orderBooks.buy[0][1], SellPrice = orderBooks.sell[0][0], SellAmount = orderBooks.sell[0][1] };

            LastPriceDate = DateTime.Now;

            bool isNewPeriod = lastUpdate.AddMinutes(60) < DateTime.Now;

            if (PreviousPrice?.BuyPrice != ActualPrice?.BuyPrice|| isNewPeriod)
            {
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
                        Buy(MinBtcTradeAmount, ActualPrice);
                    }
                }

                Sell(ActualPrice);
            }

            if (PreviousPrice == null)
            {
                PreviousPrice = ActualPrice;
            }

            PreviousPrice = ActualPrice;

            SaveOrderBooksPrices();

            Logger.LogTradeOrders(AllTradeOrders);
            Logger.LogCurrency(this, ActualPrice);
        }

        private bool Sell(ActualPrice actualPrice)
        {
            if (BotManager.LastTrade?.Type == TradeType.Sell)
            {
                if (TradeOrders.Any())
                {
                    Logger.Info($"{TargetCurrency}: Time to sell at price {actualPrice}");
                    Logger.Info(BotManager.LastTrade.ToString());
                    foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.Type == TradeOrderType.OPEN))
                    {
                        Logger.Info($"{TargetCurrency}: Buy price: {tradeOrder.Price}, Actual Price: {actualPrice},  Yield: {actualPrice.BuyPrice / tradeOrder.Price:N8}");
                        if (actualPrice.BuyPrice >= (tradeOrder.Price * TradeSettings.MinSellYield))
                        {
                            if (Sell(actualPrice.BuyPrice, tradeOrder))
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
            }
            return true;
        }

        protected override double GetBalance()
        {
            return NiceHashApi.GetBalance(BTC);
        }

        private void SaveOrderBooksPrices()
        {
            foreach (TradeOrder tradeOrder in TradeOrders.Where(to => to.Type == TradeOrderType.OPEN && to.ActualPrice != ActualPrice.BuyPrice))
            {
                tradeOrder.ActualPrice = ActualPrice.BuyPrice;
                Store.OrderBooks.SaveOrUpdate(tradeOrder);
            }
        }
    }
}
