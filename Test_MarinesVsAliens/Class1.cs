using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Server_MarinesVsAliens;
using System.Net;

namespace Test_MarinesVsAliens
{
    [TestFixture]
    public class Class1
    {
        GameSocketTransport socket = new GameSocketTransport("127.0.0.1", 555);
        EndPoint end = new IPEndPoint(IPAddress.Any, 555);
        int dataLenght = 13;

        [Test]
        public void GameSocketInit()
        {
            Assert.That(socket.Socket, Is.Not.EqualTo(null));
        }

        [Test]
        public void SocketSendTo()
        {
            Assert.That(socket.SendTo(new byte[3], 3, end), Is.EqualTo(3));
        }

        [Test]
        public void ServerInit()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t, Is.Not.EqualTo(null));
        }

        [Test]
        public void ServerPacketLenght()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.ExtractPacket(0), Is.EqualTo(false));
        }

        [Test]
        public void ServerInitRooms()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.rooms, Is.Not.EqualTo(null));
        }

        [Test]
        public void ServerEnterRoom()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.ManageRoomEnter(0), Is.EqualTo(true));
        }

        [Test]
        public void ServerNoRoom()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 0);

            Assert.That(t.ManageRoomEnter(0), Is.EqualTo(false));
        }

        [Test]
        public void ServerSend()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.Send(end, new byte[10], 10), Is.EqualTo(true));
        }

        [Test]
        public void ServerJoinRoom()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();

            Assert.That(t.JoinRoom(0, 0), Is.EqualTo(true));
        }

        [Test]
        public void ServerRoomAdd()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].membersCount, Is.EqualTo(1));
        }

        [Test]
        public void ServerRoomRemove()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);
            t.rooms[0].Remove(end);

            Assert.That(t.rooms[0].membersCount, Is.EqualTo(0));
        }

        [Test]
        public void ServerRoomGetMemberIndex()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].GetMemberID(end), Is.EqualTo(0));
        }

        [Test]
        public void ServerRoomGetMemberEndPoint()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].GetMemberEndPoint(0), Is.EqualTo(end));
        }

        [Test]
        public void ServerRoomContains()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].Contains(end), Is.EqualTo(true));
        }

        [Test]
        public void ServerRoomReceive()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.data[(int)PacketOffset.ANSWER] = 3;

            Assert.That(t.rooms[0].Receive(dataLenght, 0), Is.EqualTo(-1));
        }

        [Test]
        public void ServerRoomReceiveAnswer()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.data[(int)PacketOffset.ANSWER] = (byte)PacketType.ANSWER;

            Assert.That(t.rooms[0].Receive(dataLenght, 0), Is.EqualTo((int)PacketType.ANSWER));
        }

        [Test]
        public void ServerRoomReceiveRequest()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.data[(int)PacketOffset.ANSWER] = (byte)PacketType.REQUEST;

            Assert.That(t.rooms[0].Receive(dataLenght, 0), Is.EqualTo((int)PacketType.REQUEST));
        }

        [Test]
        public void ServerRoomManageAnswer()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            ReliablePacket p = new ReliablePacket(5);
            p.command = (int)Command.DEATH;
            p.end = t.CurrentEndPoint;
            t.rooms[0].reliablePackets.Add(p);
            t.rooms[0].ManageAnswer((int)Command.DEATH);

            Assert.That(t.rooms[0].reliablePackets.Count, Is.EqualTo(0));
        }

        [Test]
        public void ServerRoomSendReliable()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].SendReliable(end, new byte[dataLenght], dataLenght, (int)Command.ENTERROOM);

            Assert.That(t.rooms[0].reliablePackets.Count, Is.EqualTo(1));
        }

        [Test]
        public void ServerRoomSendReliableAll()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].SendAllReliable(new byte[dataLenght], dataLenght, (int)Command.ENTERROOM);

            Assert.That(t.rooms[0].reliablePackets.Count, Is.EqualTo(t.rooms[0].membersCount));
        }

        [Test]
        public void ServerRoomManageReady()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.data = new byte[t.minPacketSize];

            Assert.That(t.rooms[0].ManageCharacterSelection(dataLenght), Is.EqualTo(false));
        }

        [Test]
        public void ServerRoomManageReadySetMember()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].ManageCharacterSelection(dataLenght), Is.EqualTo(true));
        }

        [Test]
        public void ServerRoomManageReadySetMemberReady()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);
            t.rooms[0].members[0].CurrentState = State.READY;

            Assert.That(t.rooms[0].ManageCharacterSelection(dataLenght), Is.EqualTo(false));
        }

        [Test]
        public void ServerRoomControlStartMatch()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);

            Assert.That(t.rooms[0].ControlStartMatch(0), Is.EqualTo(false));
        }

        [Test]
        public void ServerRoomControlStartMatchAllReady()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);
            t.rooms[0].members[0].CurrentState = State.READY;

            Assert.That(t.rooms[0].ControlStartMatch(0), Is.EqualTo(true));
        }

        [Test]
        public void ServerRoomStartMatch()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);
            t.rooms[0].StartMatch(0);

            Assert.That(t.rooms[0].reliablePackets.Count, Is.EqualTo(t.rooms[0].membersCount - 1));
        }

        [Test]
        public void ServerRoomManageTransform()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);
            t.CreateRoom();
            t.rooms[0].Add(end);
            t.rooms[0].ManageTransform(0);

            Assert.That(t.rooms[0].reliablePackets.Count, Is.EqualTo(t.rooms[0].membersCount - 1));
        }

        [Test]
        public void ServerTimeCalc()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.CalculateTimeDifference(1, 10, 1, 3), Is.EqualTo(7));
        }

            [Test]
        public void ServerTimeDiffMinutes()
        {
            MvsAServer t = new MvsAServer(socket, end, dataLenght, 4, 3);

            Assert.That(t.CalculateTimeDifference(1, 10, 0, 50), Is.EqualTo(20));
        }
    }
}