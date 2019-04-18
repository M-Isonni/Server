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
        public delegate void RunMachineState();
        public RunMachineState[] runState;
        public enum RoomState { LOBBY,GAME};
        MvsAServer server;
        public StateMachine machine;
        public List<ReliablePacket> reliablePackets;
        List<User> usersToRemove;
        public delegate bool RoomCommand(byte[] data, int dataLenght);
        public RoomCommand[] commands;

        public List<User> members;
        public int membersCount { get { return members.Count; } }
        int Id;

        Stopwatch t;
        float time, UpdateInterval;

        public Room(int ID, MvsAServer server)
        {
            this.server = server;
            reliablePackets = new List<ReliablePacket>();
            usersToRemove = new List<User>();
            members = new List<User>();
            machine = new StateMachine(this);
            runState = new RunMachineState[2];            
            runState[(int)Room.RoomState.LOBBY] = Update;
            runState[(int)Room.RoomState.GAME] = Update;
            Id = ID;
            t = new Stopwatch();
            UpdateInterval = 1f;
            commands = new RoomCommand[20];
            commands[(int)Command.CHARACTERSELECTION] = ManageCharacterSelection;
            commands[(int)Command.READY] = ManageReady;
            commands[(int)Command.TRANSFORM] = ManageTransform;
            commands[(int)Command.PLAYERSINROOM] = ManageSendAll;
            commands[(int)Command.CHAT] = Chat;
        }

        public void Update()
        {
            //reliable packets update
            time += GameServer.DeltaTime;

            if (time >= UpdateInterval)
            {
                time = 0;

                List<ReliablePacket> deletedRequests = new List<ReliablePacket>();
                for (int i = 0; i < reliablePackets.Count; i++)
                {
                    reliablePackets[i].Update();
                    if (reliablePackets[i].Dead)
                        deletedRequests.Add(reliablePackets[i]);
                    else
                        Send(reliablePackets[i].end, reliablePackets[i].data, reliablePackets[i].dataLenght, reliablePackets[i].id);
                }

                for (int i = 0; i < deletedRequests.Count; i++)
                    reliablePackets.Remove(deletedRequests[i]);
            }

            //users update


            byte[] id = new byte[] { 0 };
            byte[] command = new byte[] { (byte)Command.ALIVE };
            byte[] answer = new byte[] { 1 };
            byte[] payload = new byte[1];
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer, payload }, out packet);

            for (int i = 0; i < members.Count; i++)
            {
                members[i].Update();
                if (members[i].Disconnected)
                    usersToRemove.Add(members[i]);
                else
                {
                    int Id = members[i].id;
                    packet[0] = (byte)Id;
                    Send(members[i].endPoint, packet, packet.Length, Id);
                }
            }
            for (int i = 0; i < usersToRemove.Count; i++)
            {
                SendDisconnection(usersToRemove[i].endPoint);
                if (members.Contains(usersToRemove[i]))
                    members.Remove(usersToRemove[i]);
            }
            usersToRemove.Clear();
        }

        public static byte[] GetPayload(byte[] data, int index, int length)
        {
            byte[] newData = new byte[length];
            Array.Copy(data, index, newData, 0, length);
            return newData;
        }

        public bool Chat(byte[] data, int size)
        {
            byte[] id = new byte[] { (byte)data[(int)PacketOffset.ID] };
            byte[] command = new byte[] { (byte)Command.CHAT };
            byte[] answer = new byte[] { 1 };
            byte[] payload = GetPayload(data, (int)PacketOffset.PAYLOAD, (int)(size - PacketOffset.PAYLOAD));
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer, payload }, out packet);
            SendAll(packet, packet.Length, null, true);
            return true;
        }

        public int Receive(byte[] data, int lenght, int command, int id, int counter)
        {
            int answer = data[(int)PacketOffset.ANSWER];
            int memberId = GetMemberFromId(id);
            if (memberId == -1)
                return -1;

            if (counter <= members[memberId].oldCounter)
            {
                if (counter == 0 && members[memberId].counter != 255)
                    return -1;
            }

            members[memberId].oldCounter = counter;
            members[memberId].ResetTime();
            if(memberId==0)
            {
                int a = 0;
            }
            if (answer == (int)PacketType.ANSWER)
                return ManageAnswer(data, command);
            else if (answer == (int)PacketType.REQUEST)
                return ManageRequest(data, command, lenght);

            return -1;
        }

        public int ManageAnswer(byte[] data, int command)
        {
            int id = data[(int)PacketOffset.ID];
            Command Command = Command.ERROR;
            List<ReliablePacket> deletedRequests = new List<ReliablePacket>();
            for (int i = 0; i < reliablePackets.Count; i++)
            {
                if (reliablePackets[i].id == id)
                {
                    if (reliablePackets[i].command == command)
                    {
                        Command = (Command)command;//CHECK IF IT'S OK IN THIS POSITION
                        deletedRequests.Add(reliablePackets[i]);
                        //Console.WriteLine("Reliable packet eliminated");
                    }
                }
            }

            for (int i = 0; i < deletedRequests.Count; i++)
                reliablePackets.Remove(deletedRequests[i]);
            //MOVED HERE TO CHECK AFTER WE GET A RESPONSE FOR THE READIES
            if (Command == Command.READY)
                if (ControlStartMatch())
                {
                    StartMatch();
                    machine.ChangeState(RoomState.GAME);
                }

            return (int)PacketType.ANSWER;
        }

        public int ManageRequest(byte[] data, int command, int lenght)
        {
            if (commands[command] != null)
                commands[command].Invoke(data, lenght);

            return (int)PacketType.REQUEST;
        }

        public int Add(EndPoint endPoint, byte[] name)
        {
            if (!Contains(endPoint))
            {
                members.Add(new User(endPoint, GetFreeID(), 10, name));
                return members[members.Count - 1].id;
            }
            return -1;
        }

        int GetFreeID()
        {
            while (true)
            {
                int index = 0;
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].id == index)
                    {
                        index++;
                        break;
                    }
                }
                return index;
            }
        }

        public void Remove(EndPoint endPoint)
        {
            if (Contains(endPoint))
                members.Remove(members[GetMemberID(endPoint)]);
        }

        public EndPoint GetMemberEndPoint(int id)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].id == id)
                    return members[i].endPoint;
            }
            return null;
        }

        public int GetMemberID(EndPoint endP)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].endPoint.ToString() == endP.ToString())
                    return members[i].id;
            }
            return -1;
        }

        public int GetMemberFromId(int id)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].id == id)
                    return i;
            }
            return -1;
        }

        public bool Contains(EndPoint endPoint)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].endPoint.ToString() == endPoint.ToString())
                    return true;
            }
            return false;
        }

        public bool Send(EndPoint end, byte[] data, int dataLenght, int id)
        {
            //header
            byte[] room = new byte[] { (byte)Id };
            int index = GetMemberFromId(id);
            if (index == -1)
                return false;
            members[index].counter++;
            int count = members[index].counter;
            if (count > 255)
            {
                count = 0;
                members[index].counter = count;
            }
            byte[] counter = new byte[] { (byte)count };
            byte[] timer = new byte[] { (byte)DateTime.Now.Minute, (byte)DateTime.Now.Second };
            byte[] iamplayer = new byte[] { 0 };
            byte[] packet;

            UnitePackets(new byte[][] { room, counter, timer, iamplayer, data }, out packet);
            return server.Send(end, packet, packet.Length);
        }

        public void SendAll(byte[] data, int dataLenght, EndPoint sender, bool sendToSender = true)
        {
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].endPoint == sender && !sendToSender)
                    continue;
                Send(members[i].endPoint, data, dataLenght, members[i].id);
            }
        }

        public void SendReliable(EndPoint end, byte[] data, int dataLenght, int command, int id)
        {
            ReliablePacket r = new ReliablePacket(5);
            r.id = id;
            r.end = end;
            byte[] packet = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                packet[i] = data[i];
            r.data = packet;
            r.dataLenght = dataLenght;
            r.command = command;

            reliablePackets.Add(r);
            if ((int)command == (int)Command.DISCONNECT)
                Console.WriteLine("sent disconnection");
        }

        public void SendAllReliable(byte[] data, int dataLenght, int command, EndPoint sender, bool sendToSender = true)
        {

            for (int i = 0; i < membersCount; i++)
            {

                if (members[i].endPoint == sender && !sendToSender)
                    continue;
                SendReliable(members[i].endPoint, data, dataLenght, command, members[i].id);
            }
        }

        void UnitePackets(byte[][] packets, out byte[] packet)
        {
            int dataLenght = 0;
            int offset = 0;
            for (int i = 0; i < packets.Length; i++)
                dataLenght += packets[i].Length;
            byte[] data = new byte[dataLenght];
            for (int i = 0; i < packets.Length; i++)
            {
                Array.Copy(packets[i], 0, data, offset, packets[i].Length);
                offset += packets[i].Length;
            }
            packet = data;
        }

        //COMMANDS

        public bool ManageEnterRoom(byte[] data, int dataLenght, byte[] name)
        {
            int memberId = data[(int)PacketOffset.ID];
            byte[] id = new byte[] { (byte)memberId };
            byte[] command = new byte[] { (byte)Command.ENTERROOM };
            byte[] answer = new byte[] { (byte)PacketType.REQUEST };
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer, name }, out packet);

            //send to current end point his id
            SendReliable(members[GetMemberFromId(memberId)].endPoint, packet, packet.Length, (int)Command.ENTERROOM, memberId);

            //send to others room members new member command
            packet[1] = (byte)Command.NEWPLAYER;
            SendAllReliable(packet, dataLenght, (int)Command.NEWPLAYER, members[GetMemberFromId(memberId)].endPoint, false);

            return true;
        }

        public bool ManageCharacterSelection(byte[] data, int dataLenght)
        {
            int memberId = data[(int)PacketOffset.ID];
            byte characterData = data[(int)PacketOffset.PAYLOAD];
            int index = GetMemberFromId(memberId);
            if (index == -1)
            {
                //send no 
                return false;
            }
            else if (members[index].character == (int)characterData)
            {
                return false;
            }
            byte[] id = new byte[] { (byte)memberId };
            byte[] command = new byte[] { (byte)Command.CHARACTERSELECTION };
            byte[] answer = new byte[] { (byte)PacketType.REQUEST };
            byte[] character = new byte[] { characterData };
            byte[] packet;
            foreach (User u in members)
            {
                if (u.character == characterData)
                {
                    character = new byte[] { 255 };
                    UnitePackets(new byte[][] { id, command, answer, character }, out packet);
                    Send(members[index].endPoint, packet, packet.Length, memberId);
                    return true;
                }
            }
            members[index].character = characterData;
            UnitePackets(new byte[][] { id, command, answer, character }, out packet);
            SendAllReliable(packet, packet.Length, (int)Command.CHARACTERSELECTION, members[index].endPoint, true);
            return true;
        }

        public bool ManageReady(byte[] data, int dataLenght)
        {
            int memberId = data[(int)PacketOffset.ID];
            int index = GetMemberFromId(memberId);
            if (index == -1)
            {
                //send no 
                return false;
            }

            if (members[index].CurrentState != State.READY)
            {
                members[index].CurrentState = State.READY;

                byte[] id = new byte[] { (byte)memberId };
                byte[] command = new byte[] { (byte)Command.READY };
                byte[] answer = new byte[] { (byte)PacketType.REQUEST };
                byte[] packet;
                UnitePackets(new byte[][] { id, command, answer }, out packet);
                SendAllReliable(packet, packet.Length, (int)Command.READY, members[index].endPoint, true);
            }
            return false;
        }

        public bool ControlStartMatch()
        {
            int readyCount = 0;
            for (int i = 0; i < membersCount; i++)
            {
                if (members[i].CurrentState == State.READY)
                    readyCount++;
            }
            if (readyCount == membersCount && readyCount != 0)
                return true;
            return false;
        }

        public void StartMatch()
        {
            byte[] id = new byte[] { 0 };
            byte[] command = new byte[] { (byte)Command.START };
            byte[] answer = new byte[] { (byte)PacketType.REQUEST };
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer }, out packet);
            SendAllReliable(packet, packet.Length, (int)Command.START, null, true);
        }

        public bool ManageTransform(byte[] dataReceived, int dataLenght)
        {
            byte[] id = new byte[] { dataReceived[(int)PacketOffset.ID] };
            byte[] command = new byte[] { (byte)Command.TRANSFORM };
            byte[] answer = new byte[] { (byte)PacketType.ANSWER };
            byte[] data = new byte[dataLenght - (int)PacketOffset.PAYLOAD];
            Array.Copy(dataReceived, (int)PacketOffset.PAYLOAD, data, 0, data.Length);
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer, data }, out packet);

            SendAll(packet, packet.Length, members[GetMemberFromId(id[0])].endPoint, false);
            return true;
        }

        public void SendDisconnection(EndPoint end)
        {
            byte[] id = new byte[] { (byte)GetMemberID(end) };
            byte[] command = new byte[] { (byte)Command.DISCONNECT };
            byte[] answer = new byte[] { (byte)PacketType.REQUEST };
            byte[] packet;
            UnitePackets(new byte[][] { id, command, answer }, out packet);
            SendAllReliable(packet, packet.Length, (int)Command.DISCONNECT, end, false);
        }

        public bool ManageSendAll(byte[] data, int datalenght)
        {
            int Id = data[(int)PacketOffset.ID];
            List<byte[]> packets = new List<byte[]>();
            byte[] packet;

            packets.Add(new byte[] { (byte)(members.Count - 1) });

            //send to current end point all members of the room
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].id == Id)
                    continue;
                byte[] id = new byte[] { (byte)members[i].id };
                byte[] avatar = new byte[] { (byte)members[i].character };
                byte[] lenght = new byte[] { (byte)members[i].name.Length };
                UnitePackets(new byte[][] { id, avatar, lenght, members[i].name }, out packet);
                packets.Add(packet);
            }

            byte[][] p = packets.ToArray();
            UnitePackets(p, out packet);

            byte[] currId = new byte[] { (byte)Id };
            byte[] command = new byte[] { (byte)Command.PLAYERSINROOM };
            byte[] answer = new byte[] { (byte)PacketType.REQUEST };
            byte[] finalPacket;
            UnitePackets(new byte[][] { currId, command, answer, packet }, out finalPacket);
            SendReliable(members[GetMemberFromId(Id)].endPoint, finalPacket, finalPacket.Length, (int)Command.PLAYERSINROOM, Id);
            return true;
        }
    }

    public enum State { OUTSIDE, LOBBY, READY, PLAY }
    public class User
    {
        public int id;
        public byte[] name;
        public int character = -1;
        public EndPoint endPoint;
        public State CurrentState;
        public int counter, oldCounter;
        public bool Disconnected;
        float timer, deathTime;

        public User(EndPoint endPoint, int id, float deathTime, byte[] name)
        {

            this.endPoint = endPoint;
            this.id = id;
            this.deathTime = deathTime;
            this.name = name;
        }

        public void Update()
        {
            timer += GameServer.DeltaTime;
            if (timer >= deathTime)
                Disconnected = true;
        }

        public void ResetTime()
        {
            timer = 0;
        }
    }

    public class ReliablePacket
    {
        public int id;
        public EndPoint end;
        public byte[] data;
        public int dataLenght;
        public int command;
        float t, lifespan;
        public bool Dead;

        public ReliablePacket(long lifespanInMilliseconds)
        {
            this.lifespan = lifespanInMilliseconds;
        }

        public void Update()
        {
            t += GameServer.DeltaTime;
            if (t >= lifespan)
            {
                Dead = true;
            }
        }
    }
}
