using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AutoTrader.Desktop
{
    public class PriceLine
    {
        private Canvas graph;

        private int lineWeight = 2;
        private string toolTipFormat = "N10";
        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IEnumerable<decimal> values;
        protected decimal value;
        
        private string graphName;

        private static SolidColorBrush outlineBrush = new SolidColorBrush { Color = Colors.White };

        private SolidColorBrush lineBrush;

        static PriceLine()
        {
            outlineBrush.Freeze();
        }

        public PriceLine(Canvas graph, string graphName, IEnumerable<decimal> values, decimal value, Color lineColor, string toolTipFormat = "N10")
        {
            this.graph = graph;
            this.values = values;
            this.value = value;
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public void Draw()
        {
            if (!values.Any())
            {
                return;
            }

            decimal maxValue = values.Max();
            decimal minValue = values.Min();
            if (maxValue == minValue)
            {
                return;
            }

            Dispatcher?.BeginInvoke(() =>
            {
                double cHeight = graph.ActualHeight / (double)(maxValue - minValue);
                double y = graph.ActualHeight - (double)(value - minValue) * cHeight;
                string toolTip = graphName + ":" + value.ToString(toolTipFormat);
                graph.Children.Add(new Line { Stroke = lineBrush, StrokeThickness = lineWeight, X1 = 0, Y1 = y, X2 = graph.ActualWidth, Y2 = y, ToolTip = toolTip });
                graph.Children.Add(new Line { Stroke = outlineBrush, StrokeThickness = 1, X1 = 0, Y1 = y-1, X2 = graph.ActualWidth, Y2 = y-1, ToolTip = toolTip });
                graph.Children.Add(new Line { Stroke = outlineBrush, StrokeThickness = 1, X1 = 0, Y1 = y + 1, X2 = graph.ActualWidth, Y2 = y + 1, ToolTip = toolTip });
            });
        }
    }
}
