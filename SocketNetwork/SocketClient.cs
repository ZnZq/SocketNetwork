using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace SocketNetwork
{
    public class SocketClient
    {
        /*
         * https://www.youtube.com/watch?v=PCwS7F2uK3Q with changes by ZnZ
         */
        public delegate void ConnectEventHandler(SocketClient sender, bool connected);
        public event ConnectEventHandler OnConnect;

        public delegate void DisconnectedEventHandler(SocketClient sender);
        public event DisconnectedEventHandler OnDisconnect;
        public event DisconnectedEventHandler OnTryConnect;

        public delegate void DataReceivedEventHandler(SocketClient sender, ReceiveBuffer e);
        public event DataReceivedEventHandler OnDataReceived;

        public delegate void SendEventHandler(SocketClient sender, int sent);
        public event SendEventHandler OnSend;

        private byte[] lenBuffer;
        private ReceiveBuffer buffer;
        private Socket socket;

        public bool IsServerClient { get; private set; }

        private IPEndPoint _RemoteEndPoint;
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return _RemoteEndPoint ?? (socket != null
                           ? (IPEndPoint)socket.RemoteEndPoint
                           : new IPEndPoint(IPAddress.None, 0));
            }
        }

        private IPEndPoint _LocalEndPoint;
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return _LocalEndPoint ?? (socket != null
                           ? (IPEndPoint)socket.LocalEndPoint
                           : new IPEndPoint(IPAddress.None, 0));
            }
        }

        public bool Connected { get => socket?.Connected ?? false; }
        public int ConnectTimeout { get; set; } = 1000;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">if null, you can call Connect, else it`s server client</param>
        public SocketClient(Socket s = null)
        {
            socket = s;
            IsServerClient = s != null;
            lenBuffer = new byte[4];
            if (IsServerClient)
            {
                _LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
                _RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
                ReceiveAsync();
            }
        }

        public void Connect(string ip, int port)
        {
            if (IsServerClient)
                throw new Exception("This is server client, you can`t connect to another socket");

            Close(false);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = 1000,
                ReceiveTimeout = 1000
            };

            try
            {
                OnTryConnect?.Invoke(this);
                socket.Connect(ip, port);
            }
            catch { }

            if (socket.Connected)
            {
                _LocalEndPoint = (IPEndPoint) socket.LocalEndPoint;
                _RemoteEndPoint = (IPEndPoint) socket.RemoteEndPoint;
                ReceiveAsync();
            }

            OnConnect?.Invoke(this, socket.Connected);
        }

        public void Close(bool withDisconnectEvent = true, [CallerMemberName]string name = null)
        {
            if (socket != null)
            {
                if (socket.Connected)
                    socket.Disconnect(false);
                socket.Close();
                socket = null;
                if (withDisconnectEvent)
                    OnDisconnect?.Invoke(this);
            }

            buffer.Dispose();
            _LocalEndPoint = null;
            _RemoteEndPoint = null;
        }

        private void ReceiveAsync()
        {
            socket.BeginReceive(lenBuffer, 0, lenBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int rec = socket.EndReceive(ar);

                if (rec == 0)
                {
                    Close();
                    return;
                }

                if (rec != 4)
                    throw new Exception();

                buffer = new ReceiveBuffer(BitConverter.ToInt32(lenBuffer, 0));

                socket.BeginReceive(buffer.Buffer, 0, buffer.Buffer.Length, SocketFlags.None, ReceivePacketCallback, null);
            }
            catch (SocketException se)
            {
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                        Close();
                        break;
                }
            }
            catch (ObjectDisposedException) { return; }
            catch (NullReferenceException) { return; }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private void ReceivePacketCallback(IAsyncResult ar)
        {
            int rec = socket.EndReceive(ar);

            if (rec <= 0)
                return;

            buffer.BufStream.Write(buffer.Buffer, 0, rec);
            buffer.ToReceive -= rec;

            if (buffer.ToReceive > 0)
            {
                Array.Clear(buffer.Buffer, 0, buffer.Buffer.Length);
                socket.BeginReceive(buffer.Buffer, 0, buffer.Buffer.Length, SocketFlags.None, ReceivePacketCallback, null);
                return;
            }

            buffer.BufStream.Position = 0;
            OnDataReceived?.Invoke(this, buffer);
            buffer.Dispose();

            ReceiveAsync();
        }

        public void Send(byte[] data, int index = 0, int len = -1)
        {
            if (len <= -1)
                len = data.Length;

            socket.BeginSend(BitConverter.GetBytes(len), 0, 4, SocketFlags.None, SendCallback, null);
            socket.BeginSend(data, index, len, SocketFlags.None, SendCallback, null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = socket.EndSend(ar);

                OnSend?.Invoke(this, sent);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SEND ERROR\n{ex.Message}");
            }
        }
    }
}
