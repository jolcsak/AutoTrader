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
        public static string BTC = NiceHashApi.BTC;

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
            if (!IsActualPricesUpdated())
            {
                return;
            }

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
            }

            Sell(ActualPrice);
            HandleLimitOrders(ActualPrice);

            SaveOrderBooksPrices();

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

            double lastMonthFiatProfit = lastMonthOrders.Sum(o => o.SellBtcAmount) - lastMonthOrders.Sum(o => o.Amount);
            double lastWeekFiatProfit = lastWeekOrders.Sum(o => o.SellBtcAmount) - lastWeekOrders.Sum(o => o.Amount);
            double lastDayFiatProfit = lastDayOrders.Sum(o => o.SellBtcAmount) - lastDayOrders.Sum(o => o.Amount); ;

            double fiatRate = FiatRate;

            Logger.LogFiatProfit(fiatRate * lastDayFiatProfit, fiatRate * lastWeekFiatProfit, fiatRate * lastMonthFiatProfit);
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

        public void HandleLimitOrders(ActualPrice actualPrice)
        {
            foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.State == TradeOrderState.ENTERED || o.State == TradeOrderState.OPEN_ENTERED))
            {
                TradeOrderState state = tradeOrder.State;

                var orderResponse = NiceHashApi.GetOrder(tradeOrder.Currency, tradeOrder.SellOrderId ?? tradeOrder.BuyOrderId);
                if (orderResponse.state == "FULL")
                {
                    state = state == TradeOrderState.OPEN_ENTERED ? TradeOrderState.OPEN : TradeOrderState.CLOSED;
                    if (!UpdateBuyFull(tradeOrder, orderResponse, state))
                    {
                        Logger.Err($"Error during LIMIT order finalization: {tradeOrder}!");
                    }
                }

                if (state == TradeOrderState.ENTERED || state == TradeOrderState.OPEN_ENTERED)
                {
                    DateTime tradeDate = state == TradeOrderState.OPEN_ENTERED ? tradeOrder.BuyDate : tradeOrder.SellDate;
                    
                    bool isOlder = tradeDate.AddHours(1) < DateTime.Now;
                    bool isSellBelowLossLimit = state == TradeOrderState.ENTERED && tradeOrder.ActualYield > -5;
                    bool isBuyPriceLowered = tradeOrder.SellPrice == 0 && tradeOrder.IsBuyPriceLowered(actualPrice);
                    bool isSellPriceUppered = tradeOrder.IsSellPriceUppered(actualPrice);

                    if (isOlder || isSellBelowLossLimit || isBuyPriceLowered || isSellPriceUppered)
                    {
                        TradeOrderState cancelState = state == TradeOrderState.OPEN_ENTERED ? TradeOrderState.CANCELLED : TradeOrderState.OPEN;
                        Logger.Warn($"Cancel order : {tradeOrder}");
                        if (CancelLimit(tradeOrder, cancelState))
                        {
                            Logger.Warn($"Canceled");
                        }
                        else
                        {
                            Logger.Err($"Cancel failed!");
                        }
                    }
                }
            }
        }

        private void Sell(ActualPrice actualPrice, TradeOrder tradeOrder)
        {
            Logger.Info(tradeOrder.ToString());
            if (BotManager.LastTrade != null)
            {
                Logger.Info(BotManager.LastTrade.ToString());
            }

            TradeResult tradeResult = Sell(actualPrice.BuyPrice, tradeOrder);
            if (tradeResult == TradeResult.LIMIT)
            {
                Logger.Info("Sell limit order placed.");
            }
            if (tradeResult == TradeResult.DONE)
            {
                Logger.Info("Sell done.");
            }
            if (tradeResult == TradeResult.ERROR)
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
