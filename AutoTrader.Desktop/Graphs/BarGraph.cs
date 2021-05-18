using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AutoTrader.Traders;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Desktop
{
    public class BarGraph
    {
        private Canvas graph;

        private IAnalyzable<AnalyzableTick<decimal?>> values;
        private string graphName;
        private string toolTipFormat = "N10";

        private static SolidColorBrush pointFillRedBrush = new SolidColorBrush { Color = Colors.Red };
        private static SolidColorBrush pointFillGreenBrush = new SolidColorBrush { Color = Colors.Green };
        protected Dispatcher Dispatcher => Application.Current?.Dispatcher;

        private DateProvider dateProvider;

        private IList<AnalyzableTick<decimal?>> valueList = new List<AnalyzableTick<decimal?>>();


        protected int count;

        static BarGraph()
        {
            pointFillRedBrush.Freeze();
            pointFillGreenBrush.Freeze();
        }

        public BarGraph(Canvas graph, DateProvider dateProvider, int count, string graphName, IAnalyzable<AnalyzableTick<decimal?>> values)
        {
            this.graph = graph;
            this.graphName = graphName;
            this.values = values;
            this.dateProvider = dateProvider;

            this.count = count;

            for (int i = 0; i < count; i++)
            {
                if (values[i]?.Tick != null)
                {
                    valueList.Add(values[i]);
                }
            }
        }

        public void Draw(int skip = 0)
        {
            var drawValues = valueList;
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
                double maxValue = (double)drawValues.Max(v => v.Tick.Value);
                double minValue = (double)drawValues.Min(v => v.Tick.Value);
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
                foreach (AnalyzableTick<decimal?> value in drawValues)
                {
                    double currentX = dateProvider.GetPosition(value.DateTime.Value.DateTime);
                    SetAttributes(pointFillRedBrush, pointFillGreenBrush, value, out var fill, out var toolTip);

                    double y = (double)value.Tick.Value * cHeight;
                    double absY = Math.Abs(y);

                    if (rectWidth == 1)
                    {
                        graph.Children.Add(new Line { X1 = currentX, X2 = currentX, Y1 = zeroY, Y2 = zeroY + y, Stroke = fill, ToolTip = toolTip });
                    }
                    else
                    {
                        var rect = new Rectangle { Width = rectWidth, Height = absY < 1 ? 1 : absY, Fill = fill, ToolTip = toolTip };
                        Canvas.SetLeft(rect, currentX);
                        if (Math.Sign(value.Tick.Value) >= 0)
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

        private void SetAttributes(SolidColorBrush pointFillRedBrush, SolidColorBrush pointFillGreenBrush, AnalyzableTick<decimal?> value, out Brush fill, out string toolTip)
        {
            fill = pointFillGreenBrush;
            toolTip = value.Tick.Value.ToString(toolTipFormat);
        }
    }
}
