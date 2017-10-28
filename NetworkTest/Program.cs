using System;

namespace Example
{
    internal static class Program
    {
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: NetworkTest.exe <reqrep|pair|listen> [other params]");
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            switch (args[0])
            {
                case "pubsub":
                    PubSub.Execute(args);
                    break;
                case "reqrep":
                    ReqRep.Execute(args);
                    break;
                case "pair":
                    Pair.Execute(args);
                    break;
                case "listen":
                    TestListener.Execute(args);
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }
    }
}