using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        private const double MIN_PRICE_UP = 1.02;
        public static string BTC = NiceHashApi.BTC;

        protected DateTime lastUpdate = DateTime.MinValue;

        public static double MinBtcTradeAmount = 0.00025;

        protected DateTime lastBuy = DateTime.MinValue;

        protected static ISeller seller = new Seller();

        protected IList<TradeItem> buyCandidates = new List<TradeItem>();

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
                double btcBalance = TradingBotManager.IsBenchmarking ? 0 : RefreshBalance();

                if (!TradingBotManager.IsBenchmarking)
                {
                    BotManager.Refresh(ActualPrice, isNewPeriod);
                }

                if (isNewPeriod)
                {
                    lastUpdate = DateTime.Now;
                }

                if (BotManager.LastTrade?.Type == TradeType.Sell)
                {
                    buyCandidates.Clear();
                }

                if (TradeSettings.CanBuy && canBuy)
                {
                    if (BotManager.LastTrade?.Type == TradeType.Buy && BotManager.LastTrade.Date >= lastBuy.AddHours(1))
                    {
                        Logger.Info("Buy singal received, a buy candidate added:" + BotManager.LastTrade);
                        buyCandidates.Add(BotManager.LastTrade);
                    }

                    foreach (var buyCandidate in buyCandidates.ToList())
                    {
                        if (buyCandidate.Price * MIN_PRICE_UP < ActualPrice.SellPrice)
                        {
                            if (btcBalance >= MinBtcTradeAmount)
                            {
                                Logger.Info($"{TargetCurrency}: Buy at {DateTime.Now} : prev={PreviousPrice},curr={ActualPrice}");
                                Logger.Info(BotManager.LastTrade.ToString());

                                /// TODO: ActualPrice != BotManager.LastTrade.Price!!!!
                                Buy(MinBtcTradeAmount, ActualPrice, BotManager.LastTrade.Period, BotManager.LastTrade.Bot);
                                lastBuy = BotManager.LastTrade.Date;

                                buyCandidates.Remove(buyCandidate);
                                Logger.Info("Buy succeeded.");
                            }
                            else
                            {
                                //Logger.Warn("Not enough balance.");
                            }
                        }
                    }
                }
            }

            if (TradingBotManager.IsBenchmarking)
            {
                BotManager.Refresh(ActualPrice, isNewPeriod);
            }
            else
            {
                Sell(ActualPrice);
                HandleLimitOrders(ActualPrice);

                RefreshTrades();
                SaveOrderBooksPrices();

                PreviousPrice = ActualPrice;

                Logger.LogTradeOrders(AllTradeOrders);
                LogProfit();
            }

            Logger.LogCurrency(this, ActualPrice);
        }

        private void RefreshTrades()
        {
            foreach(var tradeOrder in TradeOrders.Where(to => to.State == TradeOrderState.ENTERED || to.State == TradeOrderState.OPEN || to.State == TradeOrderState.OPEN_ENTERED))
            {
                OrderTrade orderTrade = null;
                switch (tradeOrder.State)
                {
                    case TradeOrderState.OPEN_ENTERED:
                        orderTrade = NiceHashApi.GetOrder(TargetCurrency, tradeOrder.BuyOrderId);
                        break;
                    case TradeOrderState.ENTERED:
                        orderTrade = NiceHashApi.GetOrder(TargetCurrency, tradeOrder.SellOrderId);
                        break;
                }
                if (orderTrade != null)
                {
                    switch (orderTrade.state)
                    {
                        case "CANCELLED":
                            tradeOrder.State = tradeOrder.State == TradeOrderState.OPEN_ENTERED ? TradeOrderState.OPEN : TradeOrderState.CANCELLED;
                            Store.OrderBooks.SaveOrUpdate(tradeOrder);
                            break;
                        case "FULL":
                            tradeOrder.State = tradeOrder.State == TradeOrderState.OPEN_ENTERED ? TradeOrderState.OPEN : TradeOrderState.CLOSED;
                            Store.OrderBooks.SaveOrUpdate(tradeOrder);
                            break;
                    }
                }
            }
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
                        buyCandidates.Clear();
                        Logger.Info($"{TargetCurrency}: buy candidates are cleared.");
                        Logger.Info($"{TargetCurrency}: {sellType} sell at price {actualPrice.BuyPrice}, yield: {tradeOrder.ActualYield}");
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
