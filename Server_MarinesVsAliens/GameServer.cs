using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Server_MarinesVsAliens
{
    public class GameServer
    {
        public static float DeltaTime;
        Stopwatch watch;

        protected IGameTransport transport;

        public EndPoint CurrentEndPoint { get { return currentEndPoint; } }
        protected EndPoint currentEndPoint;

        public byte[] data { get; set; }

        public GameServer(IGameTransport transport, EndPoint serverEndPoint)
        {
            this.transport = transport;

            currentEndPoint = serverEndPoint;

            data = new byte[4096];

            watch = new Stopwatch();
        }

        public void Run()
        {
            while (true)
            {
                //poll waits 100000 microseconds (1/10 seconds)
                if (transport.Poll(100000))
                {
                    data = new byte[4096];
                    int lenght;
                    try
                    {
                        lenght= transport.ReceiveFrom(data, ref currentEndPoint);
                    }
                    catch
                    {
                        continue;
                    }              
                    
                    byte[] dataTruncated = new byte[lenght];
                    Array.Copy(data, 0, dataTruncated, 0, lenght);

                    ExtractPacket(dataTruncated,lenght);
                }

                DeltaTime = watch.ElapsedMilliseconds / 1000f;
                watch.Restart();

                Update();
            }
        }

        public virtual bool ExtractPacket(byte[] data,int dataLenght)
        {
            return true;
        }

        public virtual void Update()
        {

        }
    }
}
