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

        public void Draw(int xOffset = 0)
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

                SolidColorBrush lineBrush = new SolidColorBrush();
                SolidColorBrush pointOutlineBrush = new SolidColorBrush();
                SolidColorBrush pointFillBrush = new SolidColorBrush();

                lineBrush.Color = lineColor;
                pointOutlineBrush.Color = Colors.Black;
                pointFillBrush.Color = Colors.Orange;

                int halfPointSize = pointSize / 2;

                double priceHeight = maxValue - minValue;
                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double priceWidth = values.Count - 1;
                double cWidth = width / priceWidth;
                double cHeight = height / priceHeight;
                double currentX = 0;

                var points = new PointCollection();
                var cXOffset = cWidth * xOffset;
                foreach (double value in values)
                {
                    double y = (value - minValue) * cHeight;
                    points.Add(new Point(currentX + cXOffset, height - y));
                    currentX += cWidth;
                }

                var polyline = new Polyline();
                polyline.Stroke = lineBrush;
                polyline.StrokeThickness = lineWeight;
                polyline.Points = points;
                polyline.ToolTip = graphName;
                graph.Children.Add(polyline);

                if (showPoints)
                {
                    currentX = 0;
                    foreach (double value in values)
                    {
                        double y = (value - minValue) * cHeight;
                        var rect = new Rectangle();
                        rect.Stroke = pointOutlineBrush;
                        rect.Fill = pointFillBrush;
                        rect.Width = pointSize;
                        rect.Height = pointSize;
                        rect.ToolTip = value.ToString(toolTipFormat);
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
