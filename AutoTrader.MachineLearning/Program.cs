using AutoTrader.Log;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoTrader.MachineLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            string path = AssemblyDirectory + "\\..\\..\\..\\LearningData";

            if (args[0] == "Collect")
            {
                new TraderThread().ExportTraindData(path);
            }
            if (args[0] == "Train")
            {
                // https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting
                MLContext mLContext = new MLContext();
                var dataView = mLContext.Data.LoadFromTextFile<ModelInput>(Path.Combine(path, "AiData.txt"), hasHeader: false, separatorChar: ';');

                var forecastingPipeline = mLContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedPrices",
                    inputColumnName: "Price",
                    windowSize: 100,
                    seriesLength: 1000,
                    trainSize: 270000,
                    horizon: 100,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: "LowerBoundPrices",
                    confidenceUpperBoundColumn: "UpperBoundPrices");

                SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);

                var trainedModel = forecaster.Transform(dataView);

//                mLContext.Model.Save(trainedModel, dataView.Schema, Path.Combine(path, "TrainedAiData.zip"));

                var  outputs = new List<Single>(mLContext.Data.CreateEnumerable<ModelOutput>(trainedModel, true).Select(s => s.ForecastedPrices[0]));
                StringBuilder sb = new StringBuilder();
                foreach(var output in outputs)
                {
                    sb.AppendLine(output.ToString("N8"));
                }

                File.WriteAllText(Path.Combine(path, "FutureAiData.txt"), sb.ToString());
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
