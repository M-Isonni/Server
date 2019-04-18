using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_MarinesVsAliens
{
    public class ReliableRequest
    {
        public int command;

        public delegate bool CallBack();
        CallBack cb;

        public delegate bool SendCallBack(EndPoint e, byte[] data, int lenght);
        SendCallBack sendCb;

        float time, timer;
        int oldTime;
        byte[] data;
        EndPoint e;

        public ReliableRequest(int command, float time, byte[] data, EndPoint e, SendCallBack sendCb, CallBack cb)
        {
            this.command = command;
            this.sendCb = sendCb;
            this.cb = cb;
            this.time = time;
            oldTime = DateTime.Now.Millisecond;
            this.data = data;
            this.e = e;
        }

        public void Run()
        {
            timer += DateTime.Now.Millisecond - oldTime;
            if (timer >= time)
            {
                time = 0;
                Update();
            }
        }

        public void Update()
        {
            sendCb.Invoke(e, data, data.Length);
        }

        public void InvokeCallBack()
        {
            cb.Invoke();
        }
    }
}
