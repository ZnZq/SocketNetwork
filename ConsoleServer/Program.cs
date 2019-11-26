using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  SocketNetwork;

namespace ConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            List<SocketClient> clients = new List<SocketClient>();
            SokcetServer server = new SokcetServer();
            server.OnAccepted += client =>
            {
                Console.WriteLine($"{client.RemoteEndPoint} Connected!");
                clients.Add(client);
                client.OnDisconnect += sender =>
                {
                    Console.WriteLine($"{sender.RemoteEndPoint} Disconnected!");
                    clients.Remove(sender);
                };
                client.OnDataReceived += (sender, buffer) =>
                {
                    Console.WriteLine($"{sender.RemoteEndPoint} > {Encoding.UTF8.GetString(buffer.BufStream.ToArray())}");
                    sender.Send(buffer.BufStream.ToArray());
                };
            };

            while (true)
            {
                server.Start(8085);

                Console.WriteLine("Started!");
                Console.ReadLine();

                server.Stop();
                clients.Clear();

                Console.WriteLine("Stopped!");
                Console.ReadLine();
            }
        }
    }
}
