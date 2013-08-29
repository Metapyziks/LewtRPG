using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

using Lewt.Shared.Entities;
using Lewt.Shared.World;
using Lewt.Shared.Networking;
using Lewt.Shared.Items;

namespace Lewt.Client.Networking
{
    public class ClientMessageEventArgs : EventArgs
    {
        public readonly String Message;

        public ClientMessageEventArgs( String message )
        {
            Message = message;
        }
    }

    public delegate void ClientMessageEventHandler( object sender, ClientMessageEventArgs e );

    public static class GameClient
    {
        public const int DefaultPort = 31050;

        private static ServerBase myServer;
        private static Thread myThread;

        private static Int16 myID;
        private static Player myPlayerEntity;

        public static bool CreateCharacter = false;
        public static int CharacterBaseAttribPoints;
        public static int CharacterBaseSkillPoints;
        public static int CharacterUnusedAttribPoints;

        public static event ClientMessageEventHandler ClientMessageReceived;

        public static Int16 ID
        {
            get
            {
                return myID;
            }
        }

        public static Player PlayerEntity
        {
            get
            {
                return myPlayerEntity;
            }
        }

        public static String Nickname = "Player";

        public static bool Connected
        {
            get
            {
                return myServer != null && myServer.State != ServerState.Disconnected;
            }
        }

        public static bool IsViewingOverworld
        {
            get
            {
                return myServer != null && myServer.State == ServerState.WorldMap;
            }
        }

        public static bool IsPlaying
        {
            get
            {
                return myServer != null && myServer.State == ServerState.Playing;
            }
        }

        public static OverworldMap Overworld
        {
            get
            {
                return myServer.Overworld;
            }
        }

        public static Map Map
        {
            get
            {
                return myServer.Map;
            }
        }

        public static ClientInfo[] Clients
        {
            get
            {
                return myServer.Clients;
            }
        }

        public static bool ConnectLocal()
        {
            myServer = new LocalServer();
            PostConnect();

            return true;
        }

        public static bool Connect( String hostName, int port = DefaultPort, String password = "" )
        {
            TcpClient client;
            try
            {
                client = new TcpClient( hostName, port );
                client.NoDelay = true;
                myServer = new RemoteServer( client );

                PostConnect();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static void PostConnect()
        {
            myServer.SendHandshake();

            myThread = new Thread( new ThreadStart( MainLoop ) );
            myThread.Start();
        }

        private static void MainLoop()
        {
            while ( Connected )
            {
                myServer.CheckForPackets();

                //if ( myServer.SecondsSinceLastPacket >= 15 )
                //    myServer.Disconnect( DisconnectReason.Timeout );

                Thread.Sleep( 10 );
            }
        }

        public static void Disconnect()
        {
            myServer.Disconnect( DisconnectReason.ClientDisconnect );
        }

        public static void ClientMessage( String message )
        {
            if ( ClientMessageReceived != null )
                ClientMessageReceived( null, new ClientMessageEventArgs( message ) );
        }

        public static void SendMapRequest( UInt16 id )
        {
            myServer.SendMapRequest( id );
        }

        public static void SendPlayerLeaveMap()
        {
            myServer.SendPlayerLeaveMap();

            myPlayerEntity = null;
        }

        public static void SendPlayerStartMoving( WalkDirection dir )
        {
            myServer.SendCharacterMove( myPlayerEntity, dir );
        }

        public static void SendPlayerStopMoving()
        {
            myServer.SendCharacterStop( myPlayerEntity );
        }

        public static void SendPlayerCast( double angle )
        {
            myServer.SendSpellCast( myPlayerEntity, angle );
        }

        public static void SendPlayerUse( Entity target )
        {
            myServer.SendPlayerUse( myPlayerEntity, target );
        }

        public static void SendPlayerViewInventory()
        {
            myServer.SendPlayerViewInventory( myPlayerEntity );
        }

        public static void SendTransferItem( Entity owner, Entity target, InventorySlot slot )
        {
            myServer.SendTransferItem( owner, target, slot );
        }

        public static void SendTransferItem( Entity entityA, Entity entityB, InventorySlot slotA, InventorySlot slotB )
        {
            myServer.SendTransferItem( entityA, entityB, slotA, slotB );
        }

        public static void SendUseItem( InventorySlot slot )
        {
            myServer.SendUseItem( myPlayerEntity, slot );
        }

        public static void SendPlayerResurrect() {
            myServer.SendCharacterResurrect();
        }

        public static void ChatMessage( String message )
        {
            myServer.SendChatMessage( message );
        }

        internal static void SetID( Int16 id )
        {
            myID = id;
        }

        internal static void SetPlayerEntity( Player playerEntity )
        {
            myPlayerEntity = playerEntity;
        }

        public static void SendCharacterCreationInfo( CharacterCreationOutput output )
        {
            Nickname = output.PlayerName;
            myServer.SendCharacterCreate( output );
        }
    }
}
