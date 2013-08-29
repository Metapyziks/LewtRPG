using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Lewt.Shared.Networking;

namespace Lewt.Client.Networking
{
    public class RemoteServer : ServerBase
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

        public RemoteServer( TcpClient tcpClient )
        {
            myTcpClient = tcpClient;
        }

        public override void CheckForPackets()
        {
            NetworkStream stream = myTcpClient.GetStream();
            BinaryReader reader = new BinaryReader( stream );

            while ( stream.DataAvailable )
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
                State = ServerState.Disconnected;
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
