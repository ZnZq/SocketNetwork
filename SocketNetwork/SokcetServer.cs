using System;
using System.Net;
using System.Net.Sockets;


namespace SocketNetwork
{
    public class SokcetServer
    {
        /*
         * https://www.youtube.com/watch?v=bbT4IzCoQjs with changes by ZnZ
         */
        public delegate void ClientAcceptedHandler(SocketClient e);
        public event ClientAcceptedHandler OnAccepted;

        private Socket listener;
        public int Port { get; private set; }

        public bool Running { get; private set; }

        public void Start(int port)
        {
            if (Running)
                return;

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, Port = port));
            listener.Listen(0);

            Running = true;
            listener.BeginAccept(AcceptedCallback, null);
        }

        public void Stop()
        {
            if (!Running)
                return;

            listener.Close();
            Running = false;
        }

        private void AcceptedCallback(IAsyncResult ar)
        {
            try
            {
                Socket s = listener.EndAccept(ar);
                OnAccepted?.Invoke(new SocketClient(s));
            } catch { }

            if (Running)
                listener.BeginAccept(AcceptedCallback, null);
        }
    }
}
