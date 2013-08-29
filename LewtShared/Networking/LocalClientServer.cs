using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Lewt.Shared.Networking
{
    public static class LocalClientServer
    {
        private static int myClientToServerWaitingPackets = 0;
        private static MemoryStream myClientToServerStream = new MemoryStream();
        private static long myLastClientToServerReadPos = 0;
        private static long myLastClientToServerWritePos = 0;

        private static int myServerToClientWaitingPackets = 0;
        private static MemoryStream myServerToClientStream = new MemoryStream();
        private static long myLastServerToClientReadPos = 0;
        private static long myLastServerToClientWritePos = 0;

        public static BinaryWriter StartClientToServerPacket()
        {
            Monitor.Enter( myClientToServerStream );
            myClientToServerStream.Position = myLastClientToServerWritePos;
            return new BinaryWriter( myClientToServerStream );
        }

        public static BinaryWriter StartServerToClientPacket()
        {
            Monitor.Enter( myServerToClientStream );
            myServerToClientStream.Position = myLastServerToClientWritePos;
            return new BinaryWriter( myServerToClientStream );
        }

        public static void SendClientToServerPacket()
        {
            myLastClientToServerWritePos = myClientToServerStream.Position;
            Monitor.Exit( myClientToServerStream );
            ++myClientToServerWaitingPackets;
        }

        public static void SendServerToClientPacket()
        {
            myLastServerToClientWritePos = myServerToClientStream.Position;
            Monitor.Exit( myServerToClientStream );
            ++myServerToClientWaitingPackets;
        }

        public static bool ClientToServerPending()
        {
            return myClientToServerWaitingPackets != 0;
        }

        public static bool ServerToClientPending()
        {
            return myServerToClientWaitingPackets != 0;
        }

        public static BinaryReader ReadClientToServerPacket()
        {
            if ( myClientToServerWaitingPackets == 0 )
                throw new EndOfStreamException();

            --myClientToServerWaitingPackets;

            Monitor.Enter( myClientToServerStream );
            myClientToServerStream.Position = myLastClientToServerReadPos;
            return new BinaryReader( myClientToServerStream );
        }

        public static BinaryReader ReadServerToClientPacket()
        {
            if ( myServerToClientWaitingPackets == 0 )
                throw new EndOfStreamException();

            --myServerToClientWaitingPackets;

            Monitor.Enter( myServerToClientStream );
            myServerToClientStream.Position = myLastServerToClientReadPos;
            return new BinaryReader( myServerToClientStream );
        }

        public static void EndReadingClientToServerPacket()
        {
            myLastClientToServerReadPos = myClientToServerStream.Position;
            if ( myLastClientToServerReadPos == myLastClientToServerWritePos && myClientToServerStream.Length >= 2048 )
            {
                myClientToServerStream.Position = 0;
                myLastClientToServerReadPos = 0;
                myLastClientToServerWritePos = 0;
            }
            Monitor.Exit( myClientToServerStream );
        }

        public static void EndReadingServerToClientPacket()
        {
            myLastServerToClientReadPos = myServerToClientStream.Position;
            if ( myLastServerToClientReadPos == myLastServerToClientWritePos && myServerToClientStream.Length >= 2048 )
            {
                myServerToClientStream.Position = 0;
                myLastServerToClientReadPos = 0;
                myLastServerToClientWritePos = 0;
            }
            Monitor.Exit( myServerToClientStream );
        }

        public static void Reset()
        {
            myClientToServerWaitingPackets = 0;
            myClientToServerStream = new MemoryStream();
            myLastClientToServerReadPos = 0;
            myLastClientToServerWritePos = 0;

            myServerToClientWaitingPackets = 0;
            myServerToClientStream = new MemoryStream();
            myLastServerToClientReadPos = 0;
            myLastServerToClientWritePos = 0;
        }
    }
}
