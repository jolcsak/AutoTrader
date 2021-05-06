using System;
using RethinkDb.Driver.Extras.Dao;

namespace AutoTrader
{
    public class TradeSetting : Document<Guid>
    {
        public static TradeSetting Instance { get; set; } = new TradeSetting();

        protected bool canSave = true;

        public bool CanBuy { get; set; } = true;

        public double GameRatio { get; set; } = 1.01;

        public double BuyRatio { get; set; } = 1.10;

        public double SellRatio { get; set; } = 0.90;

        public double MinSellYield { get; set; } = 1.02;

        public bool BalanceGraphVisible { get; set; }

        public bool PriceGraphVisible { get; set; }

        public bool SmaGraphVisible { get; set; }

        public bool AoGraphVisible { get; set; }

        public bool TendencyGraphVisible { get; set; }

        public bool AiPredicitionVisible { get; set; }

        public bool RsiVisible { get; set; }

        public bool MacdVisible { get; set; }


        public bool TradesVisible { get; set; }

        public bool SmaBotEnabled { get; set; }

        public bool RsiBotEnabled { get; set; }

        public bool MacdBotEnabled { get; set; }

        public void SetCanSave(bool canSave)
        {
            this.canSave = canSave;
        }

        public bool CanSave()
        {
            return canSave;
        }
    }
}
