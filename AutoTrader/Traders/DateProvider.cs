using System;

namespace AutoTrader.Traders
{
    public class DateProvider
    {
        private DateTime minDate;
        private DateTime maxDate;

        private double cWidth;
        private double canvasWidth;

        public DateTime MinDate 
        {
            get => minDate;
            set
            {
                minDate = value;
                cWidth = canvasWidth / (MaxDate.Ticks - MinDate.Ticks);
            }
        }
        public DateTime MaxDate
        {
            get => maxDate;
            set
            {
                maxDate = value;
                cWidth = canvasWidth / (MaxDate.Ticks - MinDate.Ticks);
            }
        }

        public double Width
        {
            set
            {
                canvasWidth = value;
                cWidth = canvasWidth / (MaxDate.Ticks - MinDate.Ticks);
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
            return cWidth * (date.Ticks - MinDate.Ticks);
        }
    }
}
