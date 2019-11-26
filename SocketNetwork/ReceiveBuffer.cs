using System.IO;

namespace SocketNetwork
{
    public struct ReceiveBuffer
    {
        public const int BufferSize = 1024;
        public byte[] Buffer;
        public int ToReceive;
        public MemoryStream BufStream;

        public ReceiveBuffer(int toRec)
        {
            Buffer = new byte[BufferSize];
            ToReceive = toRec;
            BufStream = new MemoryStream(toRec);
        }

        public void Dispose()
        {
            Buffer = null;
            ToReceive = 0;
            Close();
            BufStream?.Dispose();
        }

        public void Close()
        {
            if (BufStream != null && BufStream.CanWrite)
                BufStream.Close();
        }
    } 
}
