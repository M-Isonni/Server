using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Server_MarinesVsAliens
{
    public enum Command { ERROR, JOIN, ENTERROOM, NEWPLAYER, NOROOMS, CHARACTERSELECTION, READY, START, TRANSFORM, SHOOT, STATUS, DEATH, EXIT, DISCONNECT }
    public enum PacketOffset { ROOM = 0, COUNTER = 1, TIME = 5, ID = 9, TYPE = 10, COMMAND = 11, ANSWER = 12 }
    public enum PacketType { ANSWER, REQUEST }

    public class MvsAServer : GameServer
    {
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

        public override bool ExtractPacket(int dataLenght)
        {
            if (dataLenght < minPacketSize)
                return false;

            int command = data[(int)PacketOffset.COMMAND];

            if (command == (int)Command.JOIN)
                ManageRoomEnter(dataLenght);
            else
            {
                int roomId = data[(int)PacketOffset.ROOM];
                rooms[roomId].Receive(dataLenght);
            }
            return true;
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < rooms.Count; i++)
                rooms[i].Update();
        }

        public bool ManageRoomEnter(int dataLenght)
        {
            if (usersInRooms.ContainsKey(currentEndPoint))
            {
                JoinRoom(usersInRooms[currentEndPoint], dataLenght, false);
                return false;
            }

            //search for valid room
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].membersCount < maxRoomSize)
                {
                    JoinRoom(i, dataLenght);
                    return true;
                }
            }

            //create new room if possible
            if (rooms.Count < maxRoomNumber)
            {
                CreateRoom();
                JoinRoom(rooms.Count - 1, dataLenght);
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
            data[(int)PacketOffset.COMMAND] = (byte)Command.NOROOMS;
            data[(int)PacketOffset.ANSWER] = (byte)PacketType.ANSWER;

            Send(currentEndPoint, data, dataLenght);
        }

        public bool JoinRoom(int roomID, int dataLenght, bool AddToRoom = true)
        {
            //add client to the room
            if (AddToRoom)
            {
                Console.WriteLine("New User In Room {0}, Members Count: {1}", roomID, rooms[roomID].membersCount);

                rooms[roomID].Add(currentEndPoint);
                rooms[roomID].ManageEnterRoom(dataLenght);
                usersInRooms.Add(currentEndPoint, roomID);
            }

            data[(int)PacketOffset.ROOM] = (byte)roomID;
            data[(int)PacketOffset.ID] = (byte)(rooms[roomID].GetMemberIndex(currentEndPoint));
            data[(int)PacketOffset.ANSWER] = (byte)PacketType.ANSWER;

            return Send(currentEndPoint, data, dataLenght);
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
    }
}