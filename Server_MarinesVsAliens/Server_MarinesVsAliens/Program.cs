﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_MarinesVsAliens
{
    class Program
    {
        static void Main(string[] args)
        {
            GameSocketTransport socket = new GameSocketTransport("127.0.0.1", 555);
            MvsAServer server = new MvsAServer(socket, socket.endPoint, 13, 2, 2);

            server.DebugSend(socket.endPoint, Command.JOIN, PacketType.REQUEST);
            server.DebugSend(socket.endPoint, Command.JOIN, PacketType.REQUEST);

            server.Run();
        }
    }
}
