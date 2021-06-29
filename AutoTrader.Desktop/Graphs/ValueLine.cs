using AutoTrader.Db.Entities;
using AutoTrader.Desktop.Graphs;
using AutoTrader.Traders;
using AutoTrader.Traders.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Desktop
{
    public class ValueLine
    {
        private Canvas graph;

        private bool showPoints;
        private int lineWeight = 2;
        private string toolTipFormat = "N10";
        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;

        protected int count;
        private IList<AnalyzableTick<decimal?>> valueList = new List<AnalyzableTick<decimal?>>();

        private string graphName;

        private SolidColorBrush lineBrush;
        private static SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.Black };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Yellow };

        private static SolidColorBrush buyBrush = new SolidColorBrush { Color = Colors.IndianRed };
        private static SolidColorBrush sellBrush = new SolidColorBrush { Color = Colors.LightGreen };

        private static RotateTransform rotate45 = new RotateTransform(45);

        private DateProvider dateProvider;

        static ValueLine()
        {
            buyBrush.Freeze();
            sellBrush.Freeze();
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
            rotate45.Freeze();
        }

        public ValueLine(Canvas graph, DateProvider dateProvider, string graphName, IAnalyzable<AnalyzableTick<decimal?>> values, int count, Color lineColor, bool showPoints = false, string toolTipFormat = "N10", int lineWeight = 2)            
        {
            this.graph = graph;
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;
            this.lineWeight = lineWeight;
            this.showPoints = showPoints;
            this.dateProvider = dateProvider;

            this.count = count;

            for (int i = 0; i < count; i++)
            {
                if (values[i]?.Tick != null)
                {
                    valueList.Add(values[i]);
                }
            }

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public ValueLine(Canvas graph, DateProvider dateProvider, string graphName, List<AnalyzableTick<decimal?>> values, int count, Color lineColor, bool showPoints = false, string toolTipFormat = "N10", int lineWeight = 2)
        {
            this.graph = graph;
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;
            this.lineWeight = lineWeight;
            this.showPoints = showPoints;
            this.dateProvider = dateProvider;

            this.count = count;

            valueList = values;

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public Tuple<double?, double> Draw(double? fixedCheight = null, double fixedMinValue = 0, IList<TradeItem> trades = null, IList<TradeOrder> tradeOrders = null)
        {
            double? cHeight = null;
            if (count == 0)
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            var drawValues = valueList;
            if (!drawValues.Any())
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            double maxValue = (double)drawValues.Select(v => v.Tick.Value).Max();
            double minValue = (double) drawValues.Select(v => v.Tick.Value).Min();
            if (maxValue == minValue)
            {
                return new Tuple<double?, double>(cHeight, 0);
            }

            Dispatcher?.Invoke(() =>
            {
                int pointWidth = lineWeight * 3;
                int halfPointSize = pointWidth / 2;
                double priceHeight = maxValue - minValue;
                double height = graph.ActualHeight;
                double priceWidth = drawValues.Count - 1;
                cHeight = fixedCheight.HasValue ? fixedCheight.Value : height / priceHeight;
                minValue = fixedCheight.HasValue ? fixedMinValue : minValue;

                var points = new PointCollection();
                foreach (AnalyzableTick<decimal?> value in drawValues)
                {
                    double y = ((double)value.Tick.Value - minValue) * cHeight.Value;
                    double currentX = dateProvider.GetPosition(value.DateTime.Value.DateTime);
                    points.Add(new Point(currentX, (double)(height - y)));
                }
                graph.Children.Add(new Polyline { Stroke = lineBrush, StrokeThickness = lineWeight, Points = points, ToolTip = graphName });

                DateTime previousDate = drawValues.First().DateTime.Value.DateTime;
                AnalyzableTick<decimal?> lastValue = drawValues.Last();
                foreach (AnalyzableTick<decimal?> value in drawValues)
                {

                    double currentX = dateProvider.GetPosition(value.DateTime.Value.DateTime);
                    double y = ((double)value.Tick.Value - minValue) * cHeight.Value;

                    DateTime currenDate = value.DateTime.Value.DateTime;
                    RotateTransform currentTransform = null;

                    var buyTradeOrder =
                        value == lastValue ?                            
                            tradeOrders?.FirstOrDefault(to => to.BuyDate >= previousDate && to.BuyDate > value.DateTime.Value.DateTime) :
                            tradeOrders?.FirstOrDefault(to => to.BuyDate >= previousDate && to.BuyDate <= value.DateTime.Value.DateTime);

                    DrawTradeOrder(height, currentX, (double)y, buyTradeOrder, isBuy: true);

                    var sellTradeOrder =
                        value == lastValue ?
                            tradeOrders?.FirstOrDefault(to => to.SellDate >= previousDate && to.SellDate > value.DateTime.Value.DateTime) :
                            tradeOrders?.FirstOrDefault(to => to.SellDate >= previousDate && to.SellDate <= value.DateTime.Value.DateTime);

                    DrawTradeOrder(height, currentX, (double)y, sellTradeOrder, isBuy: false);

                    var tradeItem = trades?.FirstOrDefault(ti => ti.Date == value.DateTime.Value.DateTime);
                    if (tradeItem != null)
                    {
                        Brush currentBrush = pointFillBrush;
                        string tooltip = GetTooltip(value, rotate45, ref currentTransform, buyTradeOrder, tradeItem, ref currentBrush);
                        var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = currentBrush, Width = pointWidth, Height = pointWidth, ToolTip = tooltip };
                        rect.RenderTransformOrigin = new Point(0.5, 0.5);
                        Canvas.SetLeft(rect, currentX - halfPointSize);
                        Canvas.SetBottom(rect, y - halfPointSize);
                        if (currentTransform != null)
                        {
                            rect.RenderTransform = currentTransform;
                        }
                        graph.Children.Add(rect);
                    }
                    previousDate = currenDate;
                }
            });
            return new Tuple<double?, double>(cHeight, minValue);
        }

        private string GetTooltip(AnalyzableTick<decimal?> value, RotateTransform rotate, ref RotateTransform currentTransform, TradeOrder tradeValue, TradeItem tradeItem, ref Brush currentBrush)
        {
            var tooltip = new StringBuilder();
            if (tradeValue?.State == TradeOrderState.OPEN || tradeValue?.State == TradeOrderState.ENTERED || tradeValue?.State == TradeOrderState.OPEN_ENTERED || tradeItem?.Type == TradeType.Buy)
            {
                currentBrush = buyBrush;
                tooltip.Append("Buy at");
                currentTransform = rotate;
            }
            if (tradeValue?.State == TradeOrderState.CLOSED || tradeItem?.Type == TradeType.Sell)
            {
                currentBrush = sellBrush;
                tooltip.Append("Sell at");
            }

            return tooltip.Append(' ').Append(value.Tick.Value.ToString(toolTipFormat)).Append(Environment.NewLine).Append(value.DateTime.Value.DateTime).ToString();
        }

        private void DrawTradeOrder(double height, double currentX, double y, TradeOrder tradeOrder, bool isBuy)
        {
            if (tradeOrder != null)
            {
                Brush currentBrush;
                string operation;
                string prefix;
                if (isBuy)
                {
                    operation = "Buy";
                    prefix = "B";
                    currentBrush = buyBrush;
                }
                else
                {
                    operation = "Sell";
                    prefix = "S";
                    currentBrush = sellBrush;
                }

                var orderTooltip = operation + " at " + tradeOrder.Price.ToString(toolTipFormat);
                double ly = height - y;
                var orderLine = new Line { Stroke = currentBrush, StrokeThickness = lineWeight, X1 = currentX, Y1 = ly, X2 = currentX, Y2 = ly - 20, ToolTip = orderTooltip };
                graph.Children.Add(orderLine);

                OutlinedText textBlock = new OutlinedText { Text = prefix, Stroke = pointOutlineBrush, Fill = currentBrush, StrokeThickness = 1, FontSize = 14, Bold = true, ToolTip = orderTooltip };
                Canvas.SetLeft(textBlock, currentX - 5);
                Canvas.SetTop(textBlock, ly - 30);
                graph.Children.Add(textBlock);
            }
        }
    }
}
