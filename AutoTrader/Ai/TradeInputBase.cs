using Microsoft.ML.Data;
using System.Linq;

namespace AutoTrader.Ai
{
    public class TradeInputBase
    {
        public static string[] InputColumnNames
        {
            get
            {
                var members = typeof(BuyInput).GetProperties().Where(p => p.CanWrite).Select(m => m.Name).Where(n => n != "IsBuy").ToArray();
                return members;
            }
        }

        [LoadColumn(0)]
        public float Open { get; set; }

        [LoadColumn(1)]
        public float Close { get; set; }

        [LoadColumn(2)]
        public float Low { get; set; }

        [LoadColumn(3)]
        public float High { get; set; }

        [LoadColumn(4)]
        public float SmaSlow { get; set; }

        [LoadColumn(5)]
        public float SmaFast { get; set; }

        [LoadColumn(6)]
        public float Rsi { get; set; }

        [LoadColumn(7)]
        public float Ema24 { get; set; }

        [LoadColumn(8)]
        public float Ema48 { get; set; }

        [LoadColumn(9)]
        public float Ema100 { get; set; }

        [LoadColumn(10)]
        public float StoIndex { get; set; }

        [LoadColumn(11)]
        public float IsAboveSmaSlow { get; set; }

    }
}