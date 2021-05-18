using System.Collections.Generic;

namespace AutoTrader.Traders.Bots
{
    public class RsiBot : ITradingBot
    {

        public const double OVERBOUGHT = 70;
        public const double OVERSOLD = 30;

        protected TradingBotManager botManager;


        public RsiBot(TradingBotManager botManager)
        {
            this.botManager = botManager;
        }

        public string Name  => nameof(RsiBot);

        public bool IsBuy => false;
        public bool IsSell => false;

        public bool Buy(int i)
        {
            //if (i > 0)
            //{
            //    Rsi[i].IsBuy = Rsi[i - 1].Value >= OVERSOLD && Rsi[i].Value <= OVERSOLD;
            //}
            return false;
        }

        public bool Sell(int i)
        {
            //if (i > 0)
            //{
            //    Rsi[i].IsSell = Rsi[i - 1].Value <= OVERBOUGHT && Rsi[i].Value >= OVERBOUGHT;
            //}
            return false;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();
            for (int i = 0; i < 0; i++)
            {
                bool isBuy = false;
                bool isSell = Sell(i);
                if (!isSell)
                {
                    isBuy = Buy(i);
                }
                if (isBuy || isSell)
                {                    
//                    tradeItems.Add(new TradeItem(Rsi[i].Date.Date, Rsi[i].Date., isBuy ? TradeType.Buy : TradeType.Sell, Name, TradePeriod.Long));
                }
            }
            return tradeItems;
        }
    }
}
