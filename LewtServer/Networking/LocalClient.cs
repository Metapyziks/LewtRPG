using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

using Lewt.Shared.Networking;

namespace Lewt.Server.Networking
{
    public class LocalClient : ClientBase
    {
        public override IPAddress IPAddress
        {
            get
            {
                return IPAddress.Loopback;
            }
        }

        public LocalClient()
        {
            SetAuthLevel( 255 );
        }

        public override void CheckForPackets()
        {
            while ( LocalClientServer.ClientToServerPending() )
            {
                BinaryReader reader = LocalClientServer.ReadClientToServerPacket();
                OnReceivePacket( (PacketID) reader.ReadByte(), reader );
                LocalClientServer.EndReadingClientToServerPacket();
            }
        }

        public override BinaryWriter GetWriter()
        {
            return LocalClientServer.StartServerToClientPacket();
        }

        public override bool PacketPending()
        {
            return LocalClientServer.ClientToServerPending();
        }

        public override void SendPacket()
        {
            LocalClientServer.SendServerToClientPacket();
        }
    }
}
