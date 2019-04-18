using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_MarinesVsAliens
{
    public class Room
    {
        MvsAServer server;

        public struct ReliablePacket
        {
            public int id;
            public EndPoint end;
            public byte[] data;
            public int dataLenght;
            public int command;
        }

        public List<ReliablePacket> reliablePackets;

        public delegate bool RoomCommand(int dataLenght);
        public RoomCommand[] commands;

        public List<User> members;
        public int membersCount { get { return members.Count; } }
        int Id;

        Stopwatch t;
        long time, UpdateInterval;

        public Room(int ID, MvsAServer server)
        {
            this.server = server;

            reliablePackets = new List<ReliablePacket>();

            members = new List<User>();

            Id = ID;

            t = new Stopwatch();
            UpdateInterval = 1000;

            commands = new RoomCommand[20];
            commands[(int)Command.ENTERROOM] = ManageEnterRoom;
            commands[(int)Command.READY] = ManageReady;
            commands[(int)Command.TRANSFORM] = ManageTransform;
        }

        public void Update()
        {
            time += t.ElapsedMilliseconds;
            t.Reset();
            t.Start();

            if (time >= UpdateInterval)
            {
                time = 0;
                for (int i = 0; i < reliablePackets.Count; i++)
                    Send(reliablePackets[i].end, reliablePackets[i].data, reliablePackets[i].dataLenght);
                //Console.WriteLine("Reliable packets sent, packets count: " + reliablePackets.Count);
            }
        }

        public int Receive(int lenght)
        {
            int answer = server.data[(int)PacketOffset.ANSWER];
            int command = server.data[(int)PacketOffset.COMMAND];

            if (answer == (int)PacketType.ANSWER)
                return ManageAnswer(command);
            else if (answer == (int)PacketType.REQUEST)
                return ManageRequest(command, lenght);

            return -1;
        }

        public int ManageAnswer(int command)
        {
            int id = server.data[(int)PacketOffset.ID];

            List<int> deletedRequests = new List<int>();
            for (int i = 0; i < reliablePackets.Count; i++)
            {
                if (reliablePackets[i].id == id)
                {
                    if (reliablePackets[i].command == command)
                    {
                        deletedRequests.Add(i);
                        Console.WriteLine("Reliable packet eliminated");
                    }
                }
            }

            for (int i = 0; i < deletedRequests.Count; i++)
                reliablePackets.Remove(reliablePackets[deletedRequests[i]]);

            return (int)PacketType.ANSWER;
        }

        public int ManageRequest(int command, int lenght)
        {
            command = server.data[(int)PacketOffset.COMMAND];

            if (command > 0 && command < commands.Length)
                commands[command].Invoke(lenght);

            return (int)PacketType.REQUEST;
        }

        public void Add(EndPoint endPoint)
        {
            members.Add(new User(endPoint));
        }

        public void Remove(EndPoint endPoint)
        {
            members.Remove(members[GetMemberIndex(endPoint)]);
        }

        public EndPoint GetMemberEndPoint(int id)
        {
            return members[id].endPoint;
        }

        public int GetMemberIndex(EndPoint endP)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].endPoint == endP)
                    return i;
            }
            return -1;
        }

        public bool Contains(EndPoint endPoint)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].endPoint == endPoint)
                    return true;
            }
            return false;
        }

        public bool Send(EndPoint end, byte[] data, int dataLenght)
        {
            return server.Send(end, data, dataLenght);
        }

        public void SendAll(byte[] data, int dataLenght, bool sendToSender = true)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].endPoint == server.CurrentEndPoint && !sendToSender)
                    continue;
                Send(members[i].endPoint, data, dataLenght);
            }
        }

        public void SendReliable(EndPoint end, byte[] data, int dataLenght)
        {
            ReliablePacket r = new ReliablePacket();
            r.end = end;
            r.data = data;
            r.dataLenght = dataLenght;
            r.command = data[(int)PacketOffset.COMMAND];

            reliablePackets.Add(r);
            Console.WriteLine("Reliable Packets Count " + reliablePackets.Count);
        }

        public void SendAllReliable(byte[] data, int dataLenght, bool sendToSender = true)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].endPoint == server.CurrentEndPoint && !sendToSender)
                    continue;
                SendReliable(members[i].endPoint, data, dataLenght);
            }
        }

        //COMMANDS

        public bool ManageEnterRoom(int dataLenght)
        {
            server.data[(int)PacketOffset.COMMAND] = (byte)Command.ENTERROOM;
            server.data[(int)PacketOffset.ANSWER] = (byte)PacketType.REQUEST;

            //true for debug
            SendAllReliable(server.data, dataLenght, false);

            Console.WriteLine("Reliable packet sent to all members of room " + Id);

            return true;
        }

        public bool ManageReady(int dataLenght)
        {
            int memberId = server.data[(int)PacketOffset.ID];
            if (memberId > membersCount - 1)
                return false;

            if (members[memberId].CurrentState != State.READY)
            {
                members[memberId].CurrentState = State.READY;

                server.data[(int)PacketOffset.COMMAND] = (byte)Command.READY;
                server.data[(int)PacketOffset.ANSWER] = (byte)PacketType.REQUEST;
                SendAllReliable(server.data, dataLenght, false);

                return ControlStartMatch(dataLenght);
            }
            return false;
        }

        public bool ControlStartMatch(int dataLenght)
        {
            int readyCount = 0;
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].CurrentState == State.READY)
                    readyCount++;
            }
            if (readyCount == membersCount && readyCount != 0)
            {
                StartMatch(dataLenght);
                return true;
            }
            return false;
        }

        public void StartMatch(int dataLenght)
        {
            server.data[(int)PacketOffset.COMMAND] = (byte)Command.START;
            server.data[(int)PacketOffset.ANSWER] = (byte)PacketType.REQUEST;
            SendAllReliable(server.data, dataLenght, false);
        }

        public bool ManageTransform(int dataLenght)
        {
            server.data[(int)PacketOffset.ANSWER] = (byte)PacketType.ANSWER;
            SendAll(server.data, dataLenght, false);
            return true;
        }
    }

    public enum State { OUTSIDE, LOBBY, READY, PLAY }
    public class User
    {
        public EndPoint endPoint;
        public bool Ready, Start;
        public State CurrentState;

        public User(EndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
    }
}
