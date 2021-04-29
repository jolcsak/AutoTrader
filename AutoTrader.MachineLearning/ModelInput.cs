using Microsoft.ML.Data;
using System;

namespace AutoTrader.MachineLearning
{
    public class ModelInput
    {
        [LoadColumn(0)]
        public long Date { get; set; }

        [LoadColumn(1)]
        public Single Price { get; set; }
    }
}
