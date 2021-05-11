using System;

namespace AutoTrader.Traders
{
    public class DateProvider
    {
        public DateTime MinDate { get; private set; }
        public DateTime MaxDate { get; private set; }

        public DateProvider() : this(DateTime.Now.AddMonths(-1), DateTime.Now)
        {
        }

        public DateProvider(DateTime minDate, DateTime maxDate)
        {
            if (minDate > maxDate)
            {
                MinDate = maxDate;
                MaxDate = minDate;
            }
            else
            {
                MinDate = minDate;
                MaxDate = maxDate;
            }
        }

        public double GetPosition(double canvasWidth, DateTime date)
        {
            double dateWidth = MaxDate.Ticks - MinDate.Ticks;
            double cWidth = canvasWidth / dateWidth;
            return cWidth * (date.Ticks - MinDate.Ticks);
        }
    }
}
