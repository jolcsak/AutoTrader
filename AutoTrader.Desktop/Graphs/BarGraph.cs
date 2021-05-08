using AutoTrader.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace AutoTrader.Desktop
{
    public class BarGraph<T> where T : ValueBase
    {
        private Canvas graph;

        private IList<T> values;
        private string graphName;
        private string toolTipFormat = "N10";

        SolidColorBrush buyFillColor;
        SolidColorBrush sellFillColor;
        private static SolidColorBrush pointFillRedBrush = new SolidColorBrush { Color = Colors.Red };
        private static SolidColorBrush pointFillGreenBrush = new SolidColorBrush { Color = Colors.Green };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.DimGray };
        protected Dispatcher Dispatcher => Application.Current?.Dispatcher;

        static BarGraph()
        {
            pointFillRedBrush.Freeze();
            pointFillGreenBrush.Freeze();
            pointFillBrush.Freeze();
        }

        public BarGraph(Canvas graph, string graphName, IList<T> values, Color buyColor, Color sellColor)
        {
            this.graph = graph;
            this.graphName = graphName;
            this.values = values;

            buyFillColor = new SolidColorBrush { Color = buyColor };
            sellFillColor = new SolidColorBrush { Color = sellColor };

            buyFillColor.Freeze();
            sellFillColor.Freeze();
        }

        public void Draw(int skip = 0)
        {
            var drawValues = values.Where(v => v != null);
            if (!drawValues.Any())
            {
                return;
            }
            Dispatcher?.Invoke(() =>
            {
                if (!drawValues.Any())
                {
                    return;
                }
                double maxValue = drawValues.Max(v => v.Value);
                double minValue = drawValues.Min(v => v.Value);
                if (maxValue == minValue)
                {
                    return;
                }

                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double zeroY = height / 2;
                double priceHeight = maxValue - minValue;
                double priceWidth = drawValues.Count() - 1;
                double cWidth = width / priceWidth;                
                double cHeight = Math.Abs(minValue) > Math.Abs(maxValue) ? zeroY / Math.Abs(minValue) : zeroY / Math.Abs(maxValue);
                double rectWidth = cWidth < 1 ? 1 : cWidth;
                double currentX = 0;
                int i = 0;    
                foreach (T value in drawValues)
                {
                    SetAttributes(buyFillColor, sellFillColor, pointFillRedBrush, pointFillGreenBrush, value, out var fill, out var toolTip);

                    double y = value.Value * cHeight;
                    double absY = Math.Abs(y);

                    if (rectWidth == 1)
                    {
                        graph.Children.Add(new Line { X1 = currentX, X2 = currentX, Y1 = zeroY, Y2 = zeroY + y, Stroke = fill, ToolTip = i + " "+ toolTip });
                    }
                    else
                    {
                        var rect = new Rectangle { Width = rectWidth, Height = absY < 1 ? 1 : absY, Fill = fill, ToolTip = i + " " + toolTip };
                        Canvas.SetLeft(rect, currentX);
                        if (Math.Sign(value.Value) >= 0)
                        {
                            Canvas.SetTop(rect, zeroY - y);
                        }
                        else
                        {
                            Canvas.SetBottom(rect, zeroY + y);
                        }
                        graph.Children.Add(rect);
                    }
                    currentX += cWidth;
                    i++;
                }
            });
        }

        private void SetAttributes(SolidColorBrush buyFillColor, SolidColorBrush sellFillColor, SolidColorBrush pointFillRedBrush, SolidColorBrush pointFillGreenBrush, T value, out Brush fill, out string toolTip)
        {
            AoValue aoValue = value as AoValue;

            if (aoValue != null)
            {
                if (aoValue.Buy)
                {
                    fill = buyFillColor;
                    toolTip = $"Buy at: {value.Value.ToString(toolTipFormat)}";
                }
                else if (aoValue.Sell)
                {
                    fill = sellFillColor;
                    toolTip = $"Sell at: {value.Value.ToString(toolTipFormat)}";
                }
                else
                {
                    fill = aoValue.Color == AoColor.Red ? pointFillRedBrush : pointFillGreenBrush;
                    toolTip = value.Value.ToString(toolTipFormat);
                }
            }
            else
            {
                fill = pointFillBrush;
                toolTip = value.Value.ToString(toolTipFormat);
            }
        }
    }
}
