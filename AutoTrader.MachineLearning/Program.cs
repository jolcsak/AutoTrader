using AutoTrader.Log;
using System;
using System.IO;
using System.Reflection;

namespace AutoTrader.MachineLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger.Init();
            new TraderThread().ExportTraindData(AssemblyDirectory + "\\..\\..\\..\\LearningData");
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
