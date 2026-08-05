using MMO.Server;
using MMO.Protocol;
using System.Text;
using System.Threading;
using System;


public static class Program
{
    private static MatchServer StartServer()
    {
        MatchServer matchServer = new MatchServer();
        
        bool startSuccess = matchServer.Start(NetConfig.ServerPort);
        if(!startSuccess)
        {
            Console.WriteLine("Start MatchServer Failed");
            return null;
        }

        Console.WriteLine("Start MatchServer Succeed");
        return matchServer;
    }

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Logger.Init("Log");
        Logger.SetLevel(LogLevel.Debug);
        Logger.Log("System", LogLevel.System, "MatchServer start");


        if(!CodecTest.Run())
        {
            Console.WriteLine("codec test failed");
            return 1;
        }

        MatchServer matchServer = StartServer();
        if (matchServer == null)
        {
            return 1;
        }

        while(true)
        {
            Thread.Sleep(999);
            matchServer.LogServerInfo();

            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                char controlKey = keyInfo.KeyChar;

                if (controlKey == 'q' || controlKey == 'Q')
                {
                    Console.WriteLine("Server stop  Keyboard");
                    matchServer.Stop();
                    break;
                }
                else if (controlKey == 'c' || controlKey == 'C')
                {
                    //debugbreak
                    Console.WriteLine("Crash");
                    Environment.FailFast("Crash by keyboard");
                }
                else if (controlKey == 's' || controlKey == 'S')
                {
                    Console.WriteLine($"GC total memory : {GC.GetTotalMemory(false):N0} bytes");
                }
            }

        }

        Logger.Log("System", LogLevel.System, "MatchServer stop");
        return 0;
    }



}