using System;
using System.Text;
using SocketNetwork;

namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketClient client = new SocketClient();
            client.OnTryConnect += sender => Console.WriteLine("Try connect...");
            client.OnConnect += (sender, connected) =>
            {
                Console.WriteLine(connected ? "Connected!" : "Connection failed.");
                if (!connected)
                {
                    client.Connect("127.0.0.1", 8085);
                }
                else
                {
                    Console.WriteLine(sender.LocalEndPoint);
                }
            };
            client.OnDisconnect += sender =>
            {
                Console.WriteLine("Disconnected");
                client.Connect("127.0.0.1", 8085);
            };
            client.OnDataReceived += (sender, buffer) =>
            {
                Console.WriteLine($"{sender.RemoteEndPoint} > {Encoding.UTF8.GetString(buffer.BufStream.ToArray())}");
            };

            client.Connect("127.0.0.1", 8085);

            while (true)
            {
                string msg = Console.ReadLine();
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                client.Send(bytes);
            }
        }
    }
}
