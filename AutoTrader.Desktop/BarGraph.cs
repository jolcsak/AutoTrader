using AutoTrader.GraphProviders;
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
    public class BarGraph
    {
        private Canvas graph;
        private Color buyColor;
        private Color sellColor;

        private IList<AoValue> values;
        private string graphName;

        private string toolTipFormat = "N10";

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;

        public BarGraph(Canvas graph, string graphName, IList<AoValue> values, Color buyColor, Color sellColor)
        {
            this.graph = graph;
            this.values = values;
            this.buyColor = buyColor;
            this.sellColor = sellColor;
            this.graphName = graphName;
        }

        public void Draw(double xOffset = 0)
        {
            if (!values.Any())
            {
                return;
            }
            Dispatcher?.BeginInvoke(() =>
            {
                double maxValue = values.Max(v => v.Value);
                double minValue = values.Min(v => v.Value);
                if (maxValue == minValue)
                {
                    return;
                }

                SolidColorBrush buyFillColor = new SolidColorBrush();
                buyFillColor.Color = buyColor;
                SolidColorBrush sellFillColor = new SolidColorBrush();
                sellFillColor.Color = sellColor;
                SolidColorBrush pointFillRedBrush = new SolidColorBrush();
                pointFillRedBrush.Color = Colors.Red;
                SolidColorBrush pointFillGreenBrush = new SolidColorBrush();
                pointFillGreenBrush.Color = Colors.Green;

                double priceHeight = maxValue - minValue;
                double width = graph.ActualWidth;
                double height = graph.ActualHeight;
                double priceWidth = values.Count - 1;
                double cWidth = width / priceWidth;
                double cHeight = height / priceHeight;
                double currentX = 0;
                double zeroY = height / 2;
                double cXOffset = xOffset * cWidth;

                foreach (AoValue value in values)
                {
                    double y = value.Value * cHeight;
                    double absY = Math.Abs(y);
                    var rect = new Rectangle();
                    if (value.Buy)
                    {
                        rect.Fill = buyFillColor;
                        rect.ToolTip = $"Buy at: {value.Value.ToString(toolTipFormat)}";
                    }
                    else if (value.Sell)
                    {
                        rect.Fill = sellFillColor;
                        rect.ToolTip = $"Sell at: {value.Value.ToString(toolTipFormat)}";
                    }
                    else
                    {
                        rect.Fill = value.Color == AoColor.Red ? pointFillRedBrush : pointFillGreenBrush;
                        rect.ToolTip = value.Value.ToString(toolTipFormat);
                    }
                    rect.Width = cWidth < 1 ? 1 : cWidth;
                    rect.Height = absY < 1 ? 1 : absY;
                    Canvas.SetLeft(rect, currentX + cXOffset);
                    if (Math.Sign(value.Value) >= 0)
                    {
                        Canvas.SetTop(rect, zeroY - y);
                    }
                    else
                    {
                        Canvas.SetBottom(rect, zeroY + y);
                    }
                    graph.Children.Add(rect);
                    currentX += cWidth;
                }
            });
        }

    }
}
