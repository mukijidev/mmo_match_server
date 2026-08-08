using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MMO.Protocol;

namespace MMO.EchoDummy
{

    public static class Program
    {

        //
        private const string DefaultIp = "127.0.0.1";
        private const int DefaultCount = 1000;
        private const int DefaultIntervalMs = 1000;




        public static async Task<int> Main() 
        { 
           Console.OutputEncoding = Encoding.UTF8;

            string ip = AskString("server ip", DefaultIp);
            int port = AskInt("server port", NetConfig.ServerPort);
            int count = AskInt("number of dummy 1 ~ 5000", DefaultCount);
            int intervalMs = AskInt("interval(ms)", DefaultIntervalMs);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);

            Console.WriteLine($"ip={ep}, count = {count} interval = {intervalMs}ms");
            Console.WriteLine($"procs = {Environment.ProcessorCount} freq = {Stopwatch.Frequency}");
            Console.WriteLine("[Q] [q] stop");

            using CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Task[] dummies = await StartDummies(count, ep, intervalMs, cts.Token);
            Task allDone = Task.WhenAll(dummies);

            await MonitorUntilStop(allDone);
            await StopDummies(cts, allDone, count);


            return 0;
        }

        private static async Task<Task[]> StartDummies(int count, IPEndPoint ep, int intervalMs, CancellationToken ct)
        {
            Task[] dummies = new Task[count];
            long startedAt = Stopwatch.GetTimestamp();

            for(int i = 0; i < count; i++)
            {
                dummies[i] = Dummy.Loop(ep, intervalMs, ct);

                if (i % 250 == 249)
                {
                    await Task.Delay(20);
                }

            }

            Console.WriteLine($"start done : {count} tasks in {ElapsedMs(startedAt):F0} ms");
            return dummies;
        }

        private static async Task MonitorUntilStop(Task allDone)
        {
            while(!allDone.IsCompleted)
            {
                await Task.WhenAny(allDone, Task.Delay(1000));

                DummyMonitor.Print();

                if(!Console.IsInputRedirected && Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if(keyInfo.KeyChar == 'q' || keyInfo.KeyChar =='Q')
                    {
                        break;
                    }
                }
            }
        }

        private static async Task StopDummies(CancellationTokenSource cts, Task allDone, int count)
        {
            long stopStart = Stopwatch.GetTimestamp();
            cts.Cancel();

            try
            {
                //WaitForMultipleObjects;
                await allDone;
            }
            catch (Exception)
            {

            }

            Console.WriteLine($"all {count} dummies stopped in {ElapsedMs(stopStart):F0}ms");
            DummyMonitor.Print();

        }

        private static double ElapsedMs(long startTicks)
        {
            return (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
        }

        private static string AskString(string label,  string defaultValue)
        {
            Console.WriteLine($"{label} ({defaultValue})");
            string line = Console.ReadLine();
            if(string.IsNullOrWhiteSpace(line))
            {
                return defaultValue;
            }
            return line.Trim();
        }



        private static int AskInt(string label, int defaultValue)
        {
            Console.WriteLine($"{label} ({defaultValue})");
            string line = Console.ReadLine();
            int value;
            if (string.IsNullOrWhiteSpace(line) || !int.TryParse(line, out value))
            {
                return defaultValue;    
            }
            return value;
        }

    }
}