using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoTrader.Desktop.Graphs;
using AutoTrader.Traders;

namespace AutoTrader.Desktop
{
    public class DateGraph
    {
        private Canvas graph;

        private DateProvider dateProvider;

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;
        
        protected IList<DateTime> values;

        private static SolidColorBrush pointOutlineBrush = new SolidColorBrush { Color = Colors.White };
        private static SolidColorBrush pointFillBrush = new SolidColorBrush { Color = Colors.Black };

        static DateGraph()
        {
            pointOutlineBrush.Freeze();
            pointFillBrush.Freeze();
        }

        public DateGraph(Canvas graph, DateProvider dateProvider, IList<DateTime> values)
        {
            this.graph = graph;
            this.values = values;
            this.dateProvider = dateProvider;
        }

        public void Draw()
        {
            if (!values.Any())
            {
                return;
            }
            Dispatcher?.BeginInvoke(() =>
            {
                DateTime previousDate = DateTime.MinValue;
                double y = graph.ActualHeight / 2;
                double previousTextWidth = 0;
                double previousX = 0;

                foreach (DateTime value in values)
                {
                    if (previousDate.Day != value.Day)
                    {
                        double currentX = dateProvider.GetPosition(value);
                        if (previousX + previousTextWidth <= currentX)
                        {
                            OutlinedText textBlock = new OutlinedText { Text = value.ToString("MM.dd"), Stroke = pointOutlineBrush, Fill = pointFillBrush, StrokeThickness = 1, FontSize = 14, Bold = true };
                            double xt = currentX - textBlock.ActualWidth / 2;
                            Canvas.SetLeft(textBlock, xt);
                            Canvas.SetTop(textBlock, y);
                            previousTextWidth = textBlock.MinWidth;
                            graph.Children.Add(textBlock);
                            previousX = currentX;
                        }
                        previousDate = value;
                    }
                }                
            });
        }
    }
}
