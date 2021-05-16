using AutoTrader.Db.Entities;
using AutoTrader.Desktop.Graphs;
using AutoTrader.Indicators;
using AutoTrader.Traders;
using AutoTrader.Traders.Bots;
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

        private static SolidColorBrush buyBrush = new SolidColorBrush { Color = Colors.IndianRed };
        private static SolidColorBrush sellBrush = new SolidColorBrush { Color = Colors.LightGreen };

        private static RotateTransform rotate45 = new RotateTransform(45);

        private DateProvider dateProvider;

        static ValueGraph()
        {
            buyBrush.Freeze();
            sellBrush.Freeze();
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
            rotate45.Freeze();
        }

        public ValueGraph(Canvas graph, DateProvider dateProvider, string graphName, IList<T> values, Color lineColor, bool showPoints = false, string toolTipFormat = "N10", int lineWeight = 2)            
        {
            this.graph = graph;
            this.values = values.ToList();
            this.graphName = graphName;
            this.toolTipFormat = toolTipFormat;
            this.lineWeight = lineWeight;
            this.showPoints = showPoints;
            this.dateProvider = dateProvider;

            lineBrush = new SolidColorBrush { Color = lineColor };
            lineBrush.Freeze();
        }

        public Tuple<double?, double> Draw(double? fixedCheight = null, double fixedMinValue = 0, IList<TradeItem> trades = null, IList<TradeOrder> tradeOrders = null)
        {
            double? cHeight = null;
            if (!values.Any() || values.Any(v => v != null && double.IsNaN(v.Value)))
            {
                return new Tuple<double?, double> (cHeight, 0);
            }

            var drawValues = values.Where(v => v != null && v.Value != -1);
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
                int pointWidth = lineWeight * 3;
                int halfPointSize = pointWidth / 2;
                double priceHeight = maxValue - minValue;
                double height = graph.ActualHeight;
                double priceWidth = values.Count - 1;
                cHeight = fixedCheight.HasValue ? fixedCheight.Value : height / priceHeight;
                minValue = fixedCheight.HasValue ? fixedMinValue : minValue;

                var points = new PointCollection();
                foreach (T value in drawValues)
                {
                    double y = (value.Value - minValue) * cHeight.Value;
                    double currentX = dateProvider.GetPosition(value.CandleStick.Date);
                    points.Add(new Point(currentX, height - y));
                }
                graph.Children.Add(new Polyline { Stroke = lineBrush, StrokeThickness = lineWeight, Points = points, ToolTip = graphName });

                DateTime previousDate = drawValues.First().CandleStick.Date;
                T lastValue = drawValues.Last();
                foreach (T value in drawValues)
                {
                    if (value == null || value.Value == -1)
                    {
                        continue;
                    }
                    double currentX = dateProvider.GetPosition(value.CandleStick.Date);
                    double y = (value.Value - minValue) * cHeight.Value;

                    DateTime currenDate = value.CandleStick.Date;
                    RotateTransform currentTransform = null;
                    var tradeValue = value as TradeValueBase;

                    var tradeItem = trades?.FirstOrDefault(ti => ti.Date == value.CandleStick.Date);
                    var tradeOrder =
                        value == lastValue ?
                            tradeOrders?.FirstOrDefault(to => to.BuyDate >= previousDate && to.BuyDate > value.CandleStick.Date) :
                            tradeOrders?.FirstOrDefault(to => to.BuyDate >= previousDate && to.BuyDate <= value.CandleStick.Date);

                    DrawTradeOrder(height, currentX, y, tradeOrder);

                    if ((tradeValue?.ShowTrade == true && (tradeValue?.IsBuy == true || tradeValue?.IsSell == true)) || showPoints || tradeItem != null)
                    {
                        Brush currentBrush = pointFillBrush;
                        string prefix = GetSellBuyPrefix(rotate45, ref currentTransform, tradeValue, tradeItem, ref currentBrush);
                        string toolTip = prefix + " " + value.Value.ToString(toolTipFormat) + ", volume: " + value.CandleStick.volume  + Environment.NewLine + value.CandleStick?.Date;
                        var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = currentBrush, Width = pointWidth, Height = pointWidth, ToolTip = toolTip };
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

        private static string GetSellBuyPrefix(RotateTransform rotate, ref RotateTransform currentTransform, TradeValueBase tradeValue, TradeItem tradeItem, ref Brush currentBrush)
        {
            string prefix = string.Empty;
            if (tradeValue?.IsBuy == true || tradeItem?.Type == TradeType.Buy)
            {
                currentBrush = buyBrush;
                prefix = "Buy at";
                currentTransform = rotate;
            }
            if (tradeValue?.IsSell == true || tradeItem?.Type == TradeType.Sell)
            {
                currentBrush = sellBrush;
                prefix = "Sell at";
            }

            return prefix;
        }

        private void DrawTradeOrder(double height, double currentX, double y, TradeOrder tradeOrder)
        {
            if (tradeOrder != null)
            {
                Brush currentBrush = pointFillBrush;
                string operation = string.Empty;
                string prefix = string.Empty;
                if (tradeOrder.State == TradeOrderState.OPEN)
                {
                    operation = "Buy";
                    prefix = "B";
                    currentBrush = buyBrush;
                }
                else
                if (tradeOrder.State == TradeOrderState.CLOSED)
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
