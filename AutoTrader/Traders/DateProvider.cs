using System;

namespace AutoTrader.Traders
{
    public class DateProvider
    {
        private DateTime minDate;
        private DateTime maxDate;

        private double cWidth;
        private double canvasWidth;
        private long minDateTicks;

        public DateTime MinDate 
        {
            get => minDate;
            set
            {
                minDate = value;
                minDateTicks = MinDate.Ticks;
                cWidth = canvasWidth / (MaxDate.Ticks - minDateTicks);
            }
        }
        public DateTime MaxDate
        {
            get => maxDate;
            set
            {
                maxDate = value;
                cWidth = canvasWidth / (MaxDate.Ticks - minDateTicks);
            }
        }

        public double Width
        {
            set
            {
                canvasWidth = value;
                cWidth = canvasWidth / (MaxDate.Ticks - minDateTicks);
            }
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

        public double GetPosition(DateTime date)
        {
            return cWidth * (date.Ticks - minDateTicks);
        }
    }
}
