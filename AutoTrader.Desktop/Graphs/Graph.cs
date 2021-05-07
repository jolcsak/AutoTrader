using System;
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

        private int lineWeight = 2;
        private string toolTipFormat = "N10";
        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IEnumerable<double> values;

        private string graphName;
        private bool showPoints;

        private SolidColorBrush lineBrush;
        private static SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.Black };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Orange };

        static Graph()
        {
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
        }

        public Graph(Canvas graph, string graphName, IEnumerable<double> values, Color lineColor, bool showPoints, string toolTipFormat = "N10", int lineWeight = 2)
        {
            this.graph = graph;
            this.values = values;
            this.graphName = graphName;
            this.showPoints = showPoints;
            this.toolTipFormat = toolTipFormat;
            this.lineWeight = lineWeight;

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public Tuple<double?, double> Draw(int skip, double? fixedCheight = null, double fixedMinValue = 0)
        {
            double? cHeight = null;
            if (!values.Any() || values.Any(v => double.IsNaN(v)))
            {
                return new Tuple<double?, double> (cHeight, 0);
            }

            var drawValues = values.Skip(skip);
            if (!drawValues.Any())
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            double maxValue = drawValues.Max();
            double minValue = drawValues.Min();
            if (maxValue == minValue)
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            Dispatcher?.Invoke(() =>
            {
                int pointSize = 6;
                int halfPointSize = pointSize / 2;
                double priceHeight = maxValue - minValue;
                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double priceWidth = values.Count() - 1 - skip;
                double cWidth = width / priceWidth;
                cHeight = fixedCheight.HasValue ? fixedCheight.Value : height / priceHeight;
                minValue = fixedCheight.HasValue ? fixedMinValue : minValue;
                double currentX = 0;

                var points = new PointCollection();
                foreach (double value in drawValues)
                {
                    double y = (value - minValue) * cHeight.Value;
                    points.Add(new Point(currentX, height - y));
                    currentX += cWidth;
                }
                graph.Children.Add(new Polyline { Stroke = lineBrush, StrokeThickness = lineWeight, Points = points, ToolTip = graphName });

                if (showPoints)
                {
                    currentX = 0;
                    foreach (double value in drawValues)
                    {
                        double y = (value - minValue) * cHeight.Value;
                        var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = pointFillBrush, Width = pointSize, Height = pointSize, ToolTip = value.ToString(toolTipFormat)};
                        Canvas.SetLeft(rect, currentX - halfPointSize);
                        Canvas.SetBottom(rect, y - halfPointSize);
                        graph.Children.Add(rect);
                        currentX += cWidth;
                    }
                }
            });
            return new Tuple<double?, double>(cHeight, minValue);
        }
    }
}
