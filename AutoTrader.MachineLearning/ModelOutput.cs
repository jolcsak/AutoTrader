using System;

namespace AutoTrader.MachineLearning
{
    public class ModelOutput
    {
        public Single[] ForecastedPrices { get; set; }

        public Single[] LowerBoundPrices { get; set; }
        public Single[] UpperBoundPrices { get; set; }
    }
}
