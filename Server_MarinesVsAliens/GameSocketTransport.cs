﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server_MarinesVsAliens
{
    public class GameSocketTransport : IGameTransport
    {
        public IPEndPoint endPoint;
        public Socket Socket
        {
            get { return socket; }
        }
        private Socket socket;

        public GameSocketTransport(string address, short port)
        {
            IPAddress ipAddress = IPAddress.Any;
            if (address != null)
                ipAddress = IPAddress.Parse(address);

            endPoint = new IPEndPoint(ipAddress, port);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.Bind(endPoint);            
        }

        public bool Poll(int microSeconds)
        {
            return socket.Poll(microSeconds, SelectMode.SelectRead);
        }

        public int Receive(byte[] buffer)
        {
            return socket.Receive(buffer);
        }

        public int ReceiveFrom(byte[] buffer, ref EndPoint endPoint)
        {
            try
            {
                return socket.ReceiveFrom(buffer, ref endPoint);
            }
            catch
            {
                Console.WriteLine("Size buffer too big");
                return -1;
            }
        }

        public int Send(byte[] buffer, int size)
        {
            return socket.Send(buffer, 0, size, SocketFlags.None);
        }

        public int SendTo(byte[] buffer, int size, EndPoint endPoint)
        {
            try
            {
                return socket.SendTo(buffer, size, SocketFlags.None, endPoint);
            }
            catch
            {
                Console.WriteLine("Packet not sent");
                return 0;
            }
        }
    }
}
