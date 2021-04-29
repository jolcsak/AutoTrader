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
        
        protected IList<double> values;
        protected double value;

        private string graphName;

        private SolidColorBrush lineBrush;

        public PriceLine(Canvas graph, string graphName, IList<double> values, double value, Color lineColor, string toolTipFormat = "N10")
        {
            this.graph = graph;
            this.values = values;
            this.value = value;
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public void Draw(int skip)
        {
            if (!values.Any() || values.Any(v => double.IsNaN(v)))
            {
                return;
            }

            var drawValues = values.Skip(skip);
            if (!drawValues.Any())
            {
                return;
            }

            double maxValue = drawValues.Max();
            double minValue = drawValues.Min();
            if (maxValue == minValue)
            {
                return;
            }

            Dispatcher?.BeginInvoke(() =>
            {
                double cHeight = graph.ActualHeight / (maxValue - minValue);
                double y = graph.ActualHeight - (value - minValue) * cHeight;
                graph.Children.Add(new Line { Stroke = lineBrush, StrokeThickness = lineWeight, X1 = 0, Y1 = y, X2 = graph.ActualWidth, Y2 = y, ToolTip = graphName + ":" + value.ToString(toolTipFormat) });
            });
        }
    }
}
