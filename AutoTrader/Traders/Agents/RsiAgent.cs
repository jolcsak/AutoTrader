using AutoTrader.GraphProviders;
using System;
using System.Collections.Generic;

namespace AutoTrader.Traders.Agents
{
    public class RsiAgent : IAgent
    {
        private const double OVERBOUGHT = 70;
        private const double OVERSOLD = 30;

        protected GraphCollection graphCollection;

        public IList<RsiValue> Rsi => graphCollection.Rsi;

        public RsiAgent(GraphCollection graphCollection)
        {
            this.graphCollection = graphCollection;
        }

        public bool IsBuy => Rsi.Count > 0 && Rsi[Rsi.Count - 1].IsBuy;
        public bool IsSell => Rsi.Count > 0 && Rsi[Rsi.Count - 1].IsSell;

        public bool Buy(int i)
        {
            if (i > 0)
            {
                Rsi[i].IsBuy = Rsi[i - 1].Value < OVERSOLD && Rsi[i].Value > OVERSOLD;
            }
            return Rsi[i].IsBuy;
        }

        public bool Sell(int i)
        {
            if (i > 0)
            {
                Rsi[i].IsSell = Rsi[i - 1].Value > OVERBOUGHT && Rsi[i].Value < OVERBOUGHT;
            }
            return Rsi[i].IsSell;
        }

        public List<TradeItem> RefreshAll()
        {
            List<TradeItem> tradeItems = new List<TradeItem>();
            for (int i = 0; i < Rsi.Count; i++)
            {
                bool isBuy = false;
                bool isSell = Sell(i);
                if (!isSell)
                {
                    isBuy = Buy(i);
                }
                if (isBuy || isSell)
                {                    
                    tradeItems.Add(new TradeItem(Rsi[i].CandleStick.Date, Rsi[i].CandleStick.close, isBuy ? TradeType.Buy : TradeType.Sell));
                }
            }
            return tradeItems;
        }
    }
}
