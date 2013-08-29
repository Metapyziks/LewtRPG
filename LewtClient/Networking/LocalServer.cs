using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using Lewt.Shared.Networking;

namespace Lewt.Client.Networking
{
    public class LocalServer : ServerBase
    {
        public override IPAddress IPAddress
        {
            get
            {
                return IPAddress.Loopback;
            }
        }

        public override void CheckForPackets()
        {
            while ( LocalClientServer.ServerToClientPending() )
            {
                BinaryReader reader = LocalClientServer.ReadServerToClientPacket();
                OnReceivePacket( (PacketID) reader.ReadByte(), reader );
                LocalClientServer.EndReadingServerToClientPacket();
            }
        }

        public override BinaryWriter GetWriter()
        {
            return LocalClientServer.StartClientToServerPacket();
        }

        public override bool PacketPending()
        {
            return LocalClientServer.ServerToClientPending();
        }

        public override void SendPacket()
        {
            LocalClientServer.SendClientToServerPacket();
        }
    }
}
