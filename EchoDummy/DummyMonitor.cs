using System;
using System.Diagnostics;
using System.Threading;

namespace MMO.EchoDummy
{
    internal static class DummyMonitor
    {
        internal static long Connected;
        internal static long EchoCount;
        internal static long ErrorCount;

        // rtt
        private static long _rttCount;
        private static long _rttTicksTotal;
        private static long _rttTicksMax;

        private const int MaxReportedErrors = 5;
        private const double MicrosPerSecond = 1000000.0;
        public static void RecordRtt(long ticks)
        {
            Interlocked.Increment(ref _rttCount);
            Interlocked.Add(ref _rttTicksTotal, ticks);

            //TODO: interlock 처리할지말지
            if(ticks > _rttTicksMax)
            {
                _rttTicksMax = ticks;
            }
        }

        public static void ReportError(Exception e)
        {
            long n = Interlocked.Increment(ref ErrorCount);
            if ( n <= MaxReportedErrors)
            {
                Console.WriteLine($"[ERROR] {n} {e.GetType().Name} : {e.Message}");
            }
        }

        public static void Print()
        {
            long conn = Interlocked.Read(ref Connected);
            long err = Interlocked.Read(ref ErrorCount);

            long echo = Interlocked.Exchange(ref EchoCount, 0);
            long n = Interlocked.Exchange(ref _rttCount, 0);
            long ticks = Interlocked.Exchange(ref _rttTicksTotal, 0);
            long max = Interlocked.Exchange(ref _rttTicksMax, 0);

            double avgUs;
            if ( n == 0)
            {
                avgUs = 0;
            }
            else
            {
                avgUs = (ticks / (double)n) * MicrosPerSecond / Stopwatch.Frequency;
            }
            double maxUs = max * MicrosPerSecond / Stopwatch.Frequency;

            Console.WriteLine(
               $"[DUMMY] conn={conn}\n" +
               $"[DUMMY] echoTps={echo}\n" +
               $"[DUMMY] rttAvgUs={avgUs:F0}\n" +
               $"[DUMMY] rttMaxUs={maxUs:F0}\n" +
               $"[DUMMY] err={err}\n" +
               $"[DUMMY] threads={ThreadPool.ThreadCount}");
        }


    }
}
