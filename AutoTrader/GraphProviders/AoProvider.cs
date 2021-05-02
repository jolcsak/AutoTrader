﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.GraphProviders
{
    public class AoProvider
    {
        private static int lastAmps = 5;

        private IList<double> data;

        public SmaProvider SlowSmaProvider { get; set; }
        public SmaProvider FastSmaProvider { get; set; }

        public IList<AoValue> Ao { get; } = new List<AoValue>();

        public AoValue Current => Ao.Any() ? Ao.Last() : null;

        public double Frequency
        {
            get
            {
                int frequency = 0;
                for (int i = 1; i < Ao.Count; i++)
                {
                    if (i > 0 && Math.Sign(Ao[i].Value * Ao[i - 1].Value) < 0)
                    {
                        frequency++;
                    }
                    i++;
                }
                return frequency > 0 ?  (double)frequency / Ao.Count : 0;
            }
        }

        public double Amplitude
        {
            get
            {
                if (Ao.Count > 0)
                {
                    double max = Math.Abs(Ao.Select(ao => ao.Value).Max());
                    double min = Math.Abs(Ao.Select(ao => ao.Value).Min());
                    List<double> amplitudes = new List<double>();
                    for(int i = 1; i < Ao.Count; i++)
                    {
                        if (i > 0 && Ao[i].Color != Ao[i - 1].Color)
                        {
                            amplitudes.Add(Ao[i].Value >= 0 ? Ao[i].Value / max : Math.Abs(Ao[i].Value) / min);
                        }
                        i++;
                    }
                    var amp = amplitudes.Skip(amplitudes.Count > lastAmps ? amplitudes.Count - lastAmps : 0);
                    if (amp.Any())
                    {
                        return amp.Average();
                    }
                }
                return 0;
            }
        }

        public AoProvider(IList<double> data, int slowPeriod = 34, int fastPeriod = 5)
        {
            this.data = data;
            SlowSmaProvider = new SmaProvider(data, slowPeriod);
            FastSmaProvider = new SmaProvider(data, fastPeriod);
            Calculate();
        }

        public void Calculate()
        {
            double previousMa = -1;
            for (int i = 0; i < SlowSmaProvider.Sma.Count; i++)
            {                
                double slowMa = SlowSmaProvider.Sma[i];
                double fastMa = FastSmaProvider.Sma[i];
                var ma = fastMa - slowMa;
                if (fastMa > -1 && slowMa > -1)
                {
                    Ao.Add(new AoValue { Value = ma, Price = slowMa, Color = previousMa > ma ? AoColor.Red : AoColor.Green, SmaIndex = i });
                }
                previousMa = ma;
            }
        }
    }
}
