using AutoTrader.GraphProviders;
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
    public class ValueGraph<T>  where T : ValueBase
    {
        private Canvas graph;

        private bool showPoints;
        private int lineWeight = 2;
        private string toolTipFormat = "N10";
        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IList<T> values;

        private string graphName;

        private SolidColorBrush lineBrush;
        private static SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.Black };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Yellow };

        static ValueGraph()
        {
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
        }

        public ValueGraph(Canvas graph, string graphName, IList<T> values, Color lineColor, bool showPoints = false, string toolTipFormat = "N10", int lineWeight = 2)            
        {
            this.graph = graph;
            this.values = values;
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;
            this.lineWeight = lineWeight;
            this.showPoints = showPoints; 

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public Tuple<double?, double> Draw(int skip, double? fixedCheight = null, double fixedMinValue = 0)
        {
            double? cHeight = null;
            if (!values.Any() || values.Any(v => double.IsNaN(v.Value)))
            {
                return new Tuple<double?, double> (cHeight, 0);
            }

            var drawValues = values.Skip(skip);
            if (!drawValues.Any())
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            double maxValue = drawValues.Select(v => v.Value).Max();
            double minValue = drawValues.Select(v => v.Value).Min();
            if (maxValue == minValue)
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            Dispatcher?.Invoke(() =>
            {
                int halfPointSize = lineWeight * 3;
                double priceHeight = maxValue - minValue;
                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double priceWidth = values.Count - 1 - skip;
                double cWidth = width / priceWidth;
                cHeight = fixedCheight.HasValue ? fixedCheight.Value : height / priceHeight;
                minValue = fixedCheight.HasValue ? fixedMinValue : minValue;
                double currentX = 0;

                var points = new PointCollection();
                foreach (T value in drawValues)
                {
                    double y = (value.Value - minValue) * cHeight.Value;
                    points.Add(new Point(currentX, height - y));
                    currentX += cWidth;
                }
                graph.Children.Add(new Polyline { Stroke = lineBrush, StrokeThickness = lineWeight, Points = points, ToolTip = graphName });

                currentX = 0;
                int i = 0;
                foreach (T value in drawValues)
                {
                    var tradeValue = value as TradeValueBase;
                    if (tradeValue?.IsBuy == true || tradeValue?.IsSell == true || showPoints)
                    {
                        double y = (value.Value - minValue) * cHeight.Value;
                        string prefix = "";
                        int pointWidth = lineWeight * 2;
                        if (tradeValue?.IsBuy == true)
                        {
                            prefix = "Buy at";
                            pointWidth = lineWeight * 3;
                        }
                        if (tradeValue?.IsSell == true)
                        {
                            prefix = "Sell at";
                            pointWidth = lineWeight * 3;
                        }

                        var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = pointFillBrush, Width = pointWidth, Height = lineWeight *3, ToolTip = prefix + " " + value.Value.ToString(toolTipFormat) };
                        Canvas.SetLeft(rect, currentX - halfPointSize);
                        Canvas.SetBottom(rect, y - halfPointSize);
                        graph.Children.Add(rect);
                    }
                    currentX += cWidth;
                    i++;
                }
            });
            return new Tuple<double?, double>(cHeight, minValue);
        }
    }
}
