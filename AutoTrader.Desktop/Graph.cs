using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AutoTrader.Desktop
{
    public class Graph
    {
        private Canvas graph;

        private int pointSize = 6;
        private int lineWeight = 2;
        private string toolTipFormat = "N10";

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IList<double> values;

        private Color lineColor;
        private string graphName;
        private bool showPoints;

        public Graph(Canvas graph, string graphName, IList<double> values, Color lineColor, bool showPoints)
        {
            this.graph = graph;
            this.values = values;
            this.lineColor = lineColor;
            this.graphName = graphName;
            this.showPoints = showPoints;
        }

        public void Draw()
        {
            if (!values.Any())
            {
                return;
            }
            Dispatcher?.BeginInvoke(() =>
            {
                double maxValue = values.Max();
                double minValue = values.Min();
                if (maxValue == minValue)
                {
                    return;
                }

                SolidColorBrush lineBrush = new SolidColorBrush { Color = lineColor };

                int halfPointSize = pointSize / 2;
                double priceHeight = maxValue - minValue;
                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double priceWidth = values.Count - 1;
                double cWidth = width / priceWidth;
                double cHeight = height / priceHeight;
                double currentX = 0;

                var points = new PointCollection();
                foreach (double value in values)
                {
                    double y = (value - minValue) * cHeight;
                    points.Add(new Point(currentX, height - y));
                    currentX += cWidth;
                }
                graph.Children.Add(new Polyline { Stroke = lineBrush, StrokeThickness = lineWeight, Points = points, ToolTip = graphName });

                if (showPoints)
                {
                    SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.Black };
                    SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Orange };

                    currentX = 0;
                    foreach (double value in values)
                    {
                        double y = (value - minValue) * cHeight;
                        var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = pointFillBrush, Width = pointSize, Height = pointSize, ToolTip = value.ToString(toolTipFormat) };
                        Canvas.SetLeft(rect, currentX - halfPointSize);
                        Canvas.SetBottom(rect, y - halfPointSize);
                        graph.Children.Add(rect);
                        currentX += cWidth;
                    }
                }
            });
        }
    }
}
