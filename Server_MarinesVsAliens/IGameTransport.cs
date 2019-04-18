using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Server_MarinesVsAliens
{
    public interface IGameTransport
    {
        int Send(byte[] buffer, int size);
        int Receive(byte[] buffer);
        int SendTo(byte[] buffer, int size, EndPoint endPoint);
        int ReceiveFrom(byte[] buffer, ref EndPoint endPoint);
        bool Poll(int microSeconds);
    }
}
