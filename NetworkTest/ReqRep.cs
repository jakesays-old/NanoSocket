using System;
using System.Text;
using Std.Network.NanoMsg;

namespace Example
{
    public class ReqRep
    {
        static void Reply(string url)
        {
            using (var s = NanoSocket.CreateReplySocket())
            {
                s.Bind(url);
                Console.WriteLine("NODE0: RECEIVED: \"" + Encoding.UTF8.GetString(s.Receive()) + "\"");
                const string sendText = "Goodbye.";
                Console.WriteLine("NODE0: SENDING: \"" + sendText + "\"");
                s.Send(Encoding.UTF8.GetBytes(sendText));
            }
        }

        static void Request(string url)
        {
            using (var s = NanoSocket.CreateRequestSocket())
            {
                s.Connect(url);
                const string sendText = "Hello, World!";
                Console.WriteLine("NODE1: SENDING \"" + sendText + "\"");
                s.Send(Encoding.UTF8.GetBytes(sendText));
                Console.WriteLine("NODE1: RECEIVED: \"" + Encoding.UTF8.GetString(s.Receive()) + "\"");
            }
        }

        public static void Execute(string[] args)
        {
            try
            {
                switch (args[1])
                {
                    case "rep":
                        Reply(args[2]);
                        break;
                    case "req":
                        Request(args[2]);
                        break;
                    default:
                        Console.WriteLine("Usage: req|rep tcp://<addr>:<port>");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Usage: req|rep tcp://<addr>:<port>");
            }
        }
    }
}
