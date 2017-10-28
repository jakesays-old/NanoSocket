using System;
using System.Text;
using Std.Network.NanoMsg;

namespace Example
{
    class PubSub
    {
        public static void Execute(string[] args)
        {
            const string socketAddress = "ipc:///foo_bar";

            Console.WriteLine("Press return to start");
            Console.ReadLine();

            if (args[1] == "client")
            {
                using (var req = NanoSocket.CreateSubscribeSocket())
                {
                    req.Connect(socketAddress);
                    req.Subscribe("");
                    while (true)
                    {
                        var bits = req.Receive();
                        Console.WriteLine("Message from SERVER: " + Encoding.UTF8.GetString(bits));
                    }

//                    Console.WriteLine("CLIENT finished");
                }
            }

            else if (args[1] == "server")
            {
                using (var rep = NanoSocket.CreatePublishSocket())
                {
                    rep.Bind(socketAddress);

                    var counter = 0;
                    while (true)
                    {
                        Console.WriteLine("waiting to send..");
                        var x = Console.ReadLine();
                        if (x == "exit")
                        {
                            break;
                        }

                        rep.Send(Encoding.UTF8.GetBytes("frob:hello from server " + counter++));
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown argument: " + args[1]);
            }
        }
    }
}