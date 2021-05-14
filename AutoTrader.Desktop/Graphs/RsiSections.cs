using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AutoTrader.Traders.Bots;

namespace AutoTrader.Desktop
{
    public class RsiSections
    {
        private Canvas graph;

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;

        private static SolidColorBrush redBrush = new SolidColorBrush { Color = new Color { R = 255, G = 100, B = 100, A = 100 } };

        static RsiSections() 
        {
            redBrush.Freeze();
        }

        public RsiSections(Canvas graph)
        {
            this.graph = graph;
        }

        public void Draw()
        {
            Dispatcher?.BeginInvoke(() =>
            {
                double overSoldY = graph.ActualHeight * RsiBot.OVERSOLD / 100;
                double overBoughtY = graph.ActualHeight * RsiBot.OVERBOUGHT / 100;

                var overSoldRect = new Rectangle { Stroke = redBrush, Fill = redBrush, Width = graph.ActualWidth, Height = overSoldY, ToolTip ="OVERSOLD" };
                Canvas.SetBottom(overSoldRect, 0);

                var overBoughtRect = new Rectangle { Stroke = redBrush, Fill = redBrush, Width = graph.ActualWidth, Height = graph.ActualHeight - overBoughtY, ToolTip = "OVERBOUGHT" };
                Canvas.SetTop(overSoldRect, overBoughtY);

                graph.Children.Add(overSoldRect);
                graph.Children.Add(overBoughtRect);
            });
        }
    }
}
