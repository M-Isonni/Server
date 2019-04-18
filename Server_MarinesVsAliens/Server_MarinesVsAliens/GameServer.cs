using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server_MarinesVsAliens
{
    public class GameServer
    {
        protected IGameTransport transport;

        public EndPoint CurrentEndPoint { get { return currentEndPoint; } }
        protected EndPoint currentEndPoint;

        public byte[] data { get; protected set; }

        public GameServer(IGameTransport transport, EndPoint serverEndPoint)
        {
            this.transport = transport;

            currentEndPoint = serverEndPoint;

            data = new byte[4096];
        }

        public void Run()
        {
            while (true)
            {
                if (transport.Poll(100000))
                {
                    data = new byte[4096];
                    int lenght = transport.ReceiveFrom(data, ref currentEndPoint);

                    ExtractPacket(lenght);
                }

                Update();
            }
        }

        public virtual bool ExtractPacket(int dataLenght)
        {
            return true;
        }

        public virtual void Update()
        {

        }
    }
}
