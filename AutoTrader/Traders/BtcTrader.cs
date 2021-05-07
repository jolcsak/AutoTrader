using System;
using System.Linq;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Log;

namespace AutoTrader.Traders
{
    public class BtcTrader : NiceHashTraderBase
    {
        public const string BTC = "BTC";

        protected DateTime lastUpdate = DateTime.MinValue;

        public static double MinBtcTradeAmount = 0.00025;

        protected double actualPrice;
        protected double actualAmount;
        protected double previousPrice = double.MaxValue;
        protected double changeRatio;

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

            var lastPrices = NiceHashApi.GetLastPrices(TargetCurrency + BTC, 1);

            if (lastPrices?.Count() == 0 || lastPrices?[0] == null)
            {
                return;
            }

            var lastPrice = lastPrices[0];
            actualPrice = lastPrice.price;
            actualAmount = lastPrice.qty;
            LastPriceDate = lastPrice.Date;

            if (lastUpdate.AddHours(1) <= DateTime.Now)
            {
                BotManager.Refresh();
                lastUpdate = DateTime.Now;
            }

            if (previousPrice == double.MaxValue)
            {
                previousPrice = actualPrice;
            }

            changeRatio = actualPrice / previousPrice;

            if (TradeSettings.CanBuy && canBuy && btcBalance >= MinBtcTradeAmount)
            {
                if (BotManager.IsBuy)
                {
                    Buy(MinBtcTradeAmount, actualPrice, actualAmount);
                }
            }

            Sell(actualPrice);
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
                Logger.Info($"Time to sell at price {actualPrice}");
                foreach (TradeOrder tradeOrder in TradeOrders.Where(o => o.Type == TradeOrderType.OPEN))
                {
                    if (actualPrice >= (tradeOrder.Price * TradeSettings.MinSellYield))
                    {
                        Sell(actualPrice, tradeOrder);
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
