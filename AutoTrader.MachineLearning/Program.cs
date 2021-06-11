using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoTrader.Ai;
using AutoTrader.Log;
using AutoTrader.Traders;
using Microsoft.ML;

namespace AutoTrader.MachineLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            string path = AssemblyDirectory + "\\..\\..\\..\\LearningData";

            TradingBotManager.LastMonths = -12;
            new TraderThread().ExportTraindData(path);

            // https://github.com/dotnet/machinelearning-samples/tree/main/samples/csharp/getting-started/BinaryClassification_HeartDiseaseDetection
            MLContext mLContext = new MLContext();
            TrainData<BuyInput>("IsBuy", path, "BuyTrainingData.txt", "TrainedBuyData.zip", mLContext);
            TrainData<SellInput>("IsSell", path, "SellTrainingData.txt", "TrainedSellData.zip", mLContext);
        }

        private static void TrainData<T>(string label, string path, string trainingFile, string trainedFile, MLContext mLContext)
        {
            var dataView = mLContext.Data.LoadFromTextFile<T>(Path.Combine(path, trainingFile), hasHeader: false, separatorChar: ';');

            // STEP 2: Concatenate the features and set the training algorithm
            var pipeline = mLContext.Transforms.Concatenate("Features", BuyInput.InputColumnNames)
                            .Append(mLContext.BinaryClassification.Trainers.FastTree(labelColumnName: label, featureColumnName: "Features"));

            var trainedModel = pipeline.Fit(dataView);

            mLContext.Model.Save(trainedModel, dataView.Schema, Path.Combine(path, trainedFile));
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(codeBase).Path));
            }
        }
    }
}
