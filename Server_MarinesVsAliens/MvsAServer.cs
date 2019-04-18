using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.SqlServer;

namespace Server_MarinesVsAliens
{
    public enum Command { ERROR, ALIVE, ENTERROOM, NEWPLAYER, PLAYERSINROOM, NOROOMS, CHARACTERSELECTION, READY, START, TRANSFORM, SHOOT, STATUS, DEATH, EXIT, DISCONNECT, CHAT }
    public enum PacketOffset { ROOM = 0, COUNTER = 1, TIME = 2, IAMPLAYER = 4, ID = 5, COMMAND = 6, ANSWER = 7, PAYLOAD = 8 }
    public enum PacketType { ANSWER, REQUEST }

    public class MvsAServer : GameServer
    {
        public const int secondsBeforeInvalidPacket = 1;
        
        public int maxRoomSize, maxRoomNumber, minPacketSize;

        public List<Room> rooms;
        public Dictionary<EndPoint, int> usersInRooms;

        public MvsAServer(IGameTransport transport, EndPoint serverEndPoint, int minPacketSize, int maxRoomSize, int maxRoomNumber) : base(transport, serverEndPoint)
        {
            rooms = new List<Room>();
            usersInRooms = new Dictionary<EndPoint, int>();

            this.maxRoomSize = maxRoomSize;
            this.maxRoomNumber = maxRoomNumber;
            this.minPacketSize = minPacketSize;
        }

        public override bool ExtractPacket(byte [] dataReceived,int dataLenght)
        {
            if (dataLenght < minPacketSize)
                return false;

            int minute = dataReceived[(int)PacketOffset.TIME];
            int seconds = dataReceived[(int)PacketOffset.TIME + 1];
            if (CalculateTimeDifference(DateTime.Now.Minute, DateTime.Now.Second, minute, seconds) >= secondsBeforeInvalidPacket)
                return false;

            int command = dataReceived[(int)PacketOffset.COMMAND];
            int answer = dataReceived[(int)PacketOffset.ANSWER];
            int counter = dataReceived[(int)PacketOffset.COUNTER];
            int id = dataReceived[(int)PacketOffset.ID];

            if (command == (int)Command.ENTERROOM && answer == (int)PacketType.REQUEST)
                ManageRoomEnter(dataReceived,dataLenght);
            else
            {
                int roomId = dataReceived[(int)PacketOffset.ROOM];
                rooms[roomId].Receive(dataReceived,dataLenght, command, id, counter);
            }
            return true;
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < rooms.Count; i++)
                rooms[i].machine.Update();
        }

        public bool ManageRoomEnter(byte[] data,int dataLenght)
        {
            if (usersInRooms.ContainsKey(currentEndPoint))
            {
                JoinRoom(usersInRooms[currentEndPoint], dataLenght,data);
                return false;
            }

            //search for valid room
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].membersCount < maxRoomSize)
                {
                    JoinRoom(i, dataLenght,data);
                    usersInRooms.Add(currentEndPoint, i);
                    return true;
                }
            }

            //create new room if possible
            if (rooms.Count < maxRoomNumber)
            {
                CreateRoom();
                JoinRoom(rooms.Count - 1, dataLenght,data);
                usersInRooms.Add(currentEndPoint, rooms.Count - 1);
                return true;
            }
            else
            {
                //send no room left
                ManageNoRooms(dataLenght);
                return false;
            }
        }

        public void CreateRoom()
        {
            rooms.Add(new Room(rooms.Count, this));
            Console.WriteLine("New Room Crated. Room Id: " + (rooms.Count - 1));
        }

        public void ManageNoRooms(int dataLenght)
        {
            data = new byte[minPacketSize];
            //counter
            data[(int)PacketOffset.COMMAND] = (byte)Command.NOROOMS;
            data[(int)PacketOffset.ANSWER] = (byte)PacketType.ANSWER;

            Send(currentEndPoint, data, dataLenght);
        }

        public bool JoinRoom(int roomID, int dataLenght,byte[] data)
        {
            //add client to the room
            byte[] name = new byte[dataLenght - (int)PacketOffset.PAYLOAD];
            int id = rooms[roomID].Add(currentEndPoint, name);
            if (id != -1)
            {
                data[(int)PacketOffset.ID] = (byte)id;
                Array.Copy(data, (int)PacketOffset.PAYLOAD, name, 0, name.Length);
                rooms[roomID].ManageEnterRoom(data,dataLenght, name);
                return true;
            }
            return false;
        }

        public bool Send(EndPoint endPoint, byte[] data, int lenght)
        {
            if (transport.SendTo(data, lenght, endPoint) == lenght)
                return true;
            return false;
        }

        public void DebugSend(EndPoint endPoint, Command command, PacketType type)
        {
            data[(int)PacketOffset.ANSWER] = (byte)type;
            data[(int)PacketOffset.COMMAND] = (byte)command;
            Send(currentEndPoint, data, data.Length);
        }

        public int CalculateTimeDifference(int minuteA, int secondsA, int minuteB, int secondsB)
        {
            if (minuteA == minuteB)
            {
                return secondsA - secondsB;
            }
            else
            {
                int diff = 60 - secondsB;
                return secondsA + diff;
            }
        }
    }
}