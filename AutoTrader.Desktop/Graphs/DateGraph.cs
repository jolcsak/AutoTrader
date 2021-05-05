using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AutoTrader.Desktop.Graphs;

namespace AutoTrader.Desktop
{
    public class DateGraph
    {
        private Canvas graph;

        private int pointSize = 2;

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IList<DateTime> values;

        private static SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.White };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Black };

        static DateGraph()
        {
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
        }

        public DateGraph(Canvas graph, IList<DateTime> values)
        {
            this.graph = graph;
            this.values = values;
        }

        public void Draw(int skip)
        {
            if (!values.Any())
            {
                return;
            }
            Dispatcher?.BeginInvoke(() =>
            {
                var drawValues = values.Skip(skip);
                if (drawValues.Count() == 0)
                {
                    return;
                }

                int halfPointSize = pointSize / 2;
                double priceWidth = values.Count - 1 - skip;
                double cWidth = graph.ActualWidth / priceWidth;
                double currentX = 0;

                currentX = 0;
                DateTime previousDate = DateTime.MinValue;
                double y = graph.ActualHeight / 2 - halfPointSize;

                foreach (DateTime value in drawValues)
                {
                    double x = currentX - halfPointSize;
                    var rect = new Rectangle { Stroke = pointOutlineBrush, Fill = pointFillBrush, Width = pointSize, Height = pointSize, ToolTip = value.ToString()};
                    Canvas.SetLeft(rect, x);
                    Canvas.SetBottom(rect, y);
                    graph.Children.Add(rect);

                    if (previousDate.Day != value.Day)
                    {
                        OutlinedText textBlock = new OutlinedText { Text = value.ToString("MM.dd"), Stroke = pointOutlineBrush, Fill = pointFillBrush, StrokeThickness = 1, FontSize = 14, Bold = true };
                        double xt = currentX - textBlock.ActualWidth / 2;
                        Canvas.SetLeft(textBlock, xt);
                        Canvas.SetTop(textBlock, y);
                        graph.Children.Add(textBlock);
                        previousDate = value;
                    }

                    currentX += cWidth;
                }

            });
        }
    }
}
