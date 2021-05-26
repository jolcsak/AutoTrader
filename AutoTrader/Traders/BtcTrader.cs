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

        protected DateTime lastBuy = DateTime.MinValue;

        protected static ISeller seller = new Seller();


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
                return;
            }

            ActualPrice = new ActualPrice(TargetCurrency, orderBooks);

            LastPriceDate = DateTime.Now;

            bool isNewPeriod = lastUpdate.AddMinutes(60) < LastPriceDate;

            if (PreviousPrice?.SellPrice != ActualPrice?.SellPrice || PreviousPrice?.BuyPrice != ActualPrice?.BuyPrice || isNewPeriod)
            {
                double btcBalance = RefreshBalance();

                BotManager.Refresh(isNewPeriod);

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
                            Buy(MinBtcTradeAmount, ActualPrice, BotManager.LastTrade.Period, BotManager.LastTrade.Bot);
                            lastBuy = BotManager.LastTrade.Date;
                        }
                    }
                }

                Sell(ActualPrice);
                SaveOrderBooksPrices();
            }

            PreviousPrice = ActualPrice;

            Logger.LogTradeOrders(AllTradeOrders);

            LogProfit();

            Logger.LogCurrency(this, ActualPrice);
        }

        private void LogProfit()
        {
            var lastMonthOrders = AllTradeOrders.Where(to => to.State == TradeOrderState.CLOSED && to.SellDate >= DateTime.Now.AddMonths(-1)).ToList();
            var lastWeekOrders = lastMonthOrders.Where(to => to.SellDate >= DateTime.Now.AddDays(-7)).ToList();
            var lastDayOrders = lastWeekOrders.Where(to => to.SellDate >= DateTime.Now.AddDays(-1));

            double lastMonthProfit = 100 * lastMonthOrders.Sum(o => o.SellBtcAmount) / lastMonthOrders.Sum(o => o.Amount);
            double lastWeekProfit = 100 * lastWeekOrders.Sum(o => o.SellBtcAmount) / lastWeekOrders.Sum(o => o.Amount);
            double lastDayProfit = 100 * lastDayOrders.Sum(o => o.SellBtcAmount) / lastDayOrders.Sum(o => o.Amount);

            Logger.LogProfit(lastDayProfit, lastWeekProfit, lastMonthProfit);
        }

        private bool Sell(ActualPrice actualPrice)
        {
            if (TradeOrders.Any())
            {
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.State == TradeOrderState.OPEN))
                {
                    SellType sellType = seller.ShouldSell(actualPrice, tradeOrder, BotManager.LastTrade);
                    if (sellType != SellType.None)
                    {
                        Logger.Info($"{TargetCurrency}: {sellType} sell at price {actualPrice}, yield: {tradeOrder.ActualYield}");
                        Sell(actualPrice, tradeOrder);
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
            else
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
