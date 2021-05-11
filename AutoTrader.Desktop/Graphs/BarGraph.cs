using AutoTrader.Indicators;
using AutoTrader.Traders;
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
    public class BarGraph<T> where T : ValueBase
    {
        private Canvas graph;

        private IList<T> values;
        private string graphName;
        private string toolTipFormat = "N10";

        private static SolidColorBrush pointFillRedBrush = new SolidColorBrush { Color = Colors.Red };
        private static SolidColorBrush pointFillGreenBrush = new SolidColorBrush { Color = Colors.Green };
        protected Dispatcher Dispatcher => Application.Current?.Dispatcher;

        private DateProvider dateProvider;

        static BarGraph()
        {
            pointFillRedBrush.Freeze();
            pointFillGreenBrush.Freeze();
        }

        public BarGraph(Canvas graph, DateProvider dateProvider, string graphName, IList<T> values)
        {
            this.graph = graph;
            this.graphName = graphName;
            this.values = values.ToList();
            this.dateProvider = dateProvider;
        }

        public void Draw(int skip = 0)
        {
            var drawValues = values.Where(v => v != null).ToList();
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

                double zeroY = graph.ActualHeight / 2;
                double priceHeight = maxValue - minValue;
                double priceWidth = drawValues.Count() - 1;
                double cWidth = graph.ActualWidth / priceWidth;                
                double cHeight = Math.Abs(minValue) > Math.Abs(maxValue) ? zeroY / Math.Abs(minValue) : zeroY / Math.Abs(maxValue);
                double rectWidth = cWidth < 1 ? 1 : cWidth;
                foreach (T value in drawValues)
                {
                    double currentX = dateProvider.GetPosition(value.CandleStick.Date);
                    SetAttributes(pointFillRedBrush, pointFillGreenBrush, value, out var fill, out var toolTip);

                    double y = value.Value * cHeight;
                    double absY = Math.Abs(y);

                    if (rectWidth == 1)
                    {
                        graph.Children.Add(new Line { X1 = currentX, X2 = currentX, Y1 = zeroY, Y2 = zeroY + y, Stroke = fill, ToolTip = toolTip });
                    }
                    else
                    {
                        var rect = new Rectangle { Width = rectWidth, Height = absY < 1 ? 1 : absY, Fill = fill, ToolTip = toolTip };
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
                }
            });
        }

        private void SetAttributes(SolidColorBrush pointFillRedBrush, SolidColorBrush pointFillGreenBrush, T value, out Brush fill, out string toolTip)
        {
            HistValue histValue = value as HistValue;
            fill = histValue.Color == AoColor.Red ? pointFillRedBrush : pointFillGreenBrush;
            toolTip = value.Value.ToString(toolTipFormat);
        }
    }
}
