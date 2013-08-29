using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Lewt.Shared.Networking;

namespace Lewt.Server.Networking
{
    public class RemoteClient : ClientBase
    {
        private TcpClient myTcpClient;
        private MemoryStream myDataToSend;

        public override IPAddress IPAddress
        {
            get
            {
                return ( myTcpClient.Client.RemoteEndPoint as IPEndPoint ).Address;
            }
        }

        public RemoteClient( TcpClient tcpClient )
        {
            myTcpClient = tcpClient;
            myTcpClient.NoDelay = true;
        }

        public override void CheckForPackets()
        {
            if ( !myTcpClient.Connected )
            {
                State = ClientState.Disconnected;
                return;
            }

            NetworkStream stream = myTcpClient.GetStream();
            BinaryReader reader = new BinaryReader( stream );

            while ( stream.CanRead && stream.DataAvailable )
                OnReceivePacket( (PacketID) reader.ReadByte(), reader );
        }

        public override BinaryWriter GetWriter()
        {
            myDataToSend = new MemoryStream();
            return new BinaryWriter( myDataToSend );
        }

        public override bool PacketPending()
        {
            return myTcpClient.Available != 0;
        }

        public override void SendPacket()
        {
            try
            {
                NetworkStream stream = myTcpClient.GetStream();
                myDataToSend.Position = 0;
                myDataToSend.CopyTo( stream );
            }
            catch
            {
                State = ClientState.Disconnected;
                GameServer.RemoveClient( this );
            }
            myDataToSend.Dispose();
        }

        public override void Disconnect( DisconnectReason reason )
        {
            base.Disconnect( reason );

            myTcpClient.Close();
        }
    }
}
