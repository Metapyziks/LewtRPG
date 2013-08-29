using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using Lewt;
using Lewt.Shared.World;
using Lewt.Shared.Entities;

using Lewt.Shared.Networking;

namespace Lewt.Server.Networking
{
    public class ServerMessageEventArgs : EventArgs
    {
        public readonly String Message;

        public ServerMessageEventArgs( String message )
        {
            Message = message;
        }
    }

    public delegate void ServerMessageEventHandler( object sender, ServerMessageEventArgs e );

    public delegate String CommandDelegate( String[] args, ClientBase client );

    public static class GameServer
    {
        private class ServerCommand
        {
            public readonly String ChatCommand;
            public readonly CommandDelegate Delegate;
            public readonly byte AuthLevel;

            public ServerCommand( String chatCommand, CommandDelegate commandDelegate, byte authLevel = 255 )
            {
                ChatCommand = chatCommand;
                Delegate = commandDelegate;
                AuthLevel = authLevel;
            }

            public String Execute( String[] args )
            {
                return Delegate( args, null );
            }

            public String Execute( ClientBase client, String[] args )
            {
                if ( client.AuthLevel < AuthLevel )
                    return "You are not authorized to use that command!";
                else
                    return Delegate( args, client );
            }
        }

        public const int DefaultPort = 31050;
        public const int DefaultMaxClients = 4;
        public const double ThinkInterval = 1.0 / 60.0;

        public static String Name = "Lewt Server";
        public static String Password = "password";

        public static bool PasswordRequired = false;

        private static int myPort;
        private static int myMaxClients;
        private static bool myLan;
        private static bool mySinglePlayer;

        private static bool myRunning;

        private static TcpListener[] myListeners;
        private static ClientBase[] myClients;
        private static int myClientCount;

        private static List<ClientBase> myJoinQueue;

        private static Thread myThread;

        private static OverworldMap myOverworld;
        private static List<Map> myActiveMaps;

        private static DateTime myLastThinkTime;

        private static Dictionary<String, ServerCommand> myCommands;

        public static event ServerMessageEventHandler ServerMessage;

        public static int MaxClients
        {
            get
            {
                return myMaxClients;
            }
        }

        public static int ClientCount
        {
            get
            {
                return myClientCount;
            }
        }

        public static bool Running
        {
            get
            {
                return myRunning;
            }
        }

        public static bool SinglePlayer
        {
            get
            {
                return mySinglePlayer;
            }
        }

        public static OverworldMap Overworld
        {
            get
            {
                return myOverworld;
            }
        }

        public static Map[] ActiveMaps
        {
            get
            {
                return myActiveMaps.ToArray();
            }
        }

        public static ClientBase[] Clients
        {
            get
            {
                ClientBase[] clients = new ClientBase[ myClientCount ];

                for ( int i = 0, j = 0; i < myMaxClients; ++i )
                    if ( myClients[ i ] != null && myClients[ i ].State != ClientState.Disconnected )
                        clients[ j++ ] = myClients[ i ];

                return clients;
            }
        }

        static GameServer()
        {
            myCommands = new Dictionary<string,ServerCommand>();

            RegisterCommand( "say", delegate( String[] args, ClientBase client )
            {
                if ( client == null || client.IsAdmin )
                    ChatMessage( String.Join( " ", args ) );
                else
                    ChatMessage( client, String.Join( " ", args ) );
                return "";
            }, 0 );

            RegisterCommand( "stop", delegate( String[] args, ClientBase client )
            {
                Stop();
                return "Server stopped.";
            }, 255 );

            RegisterCommand( "kick", delegate( String[] args, ClientBase client )
            {
                Int16 id = Int16.Parse( args[ 0 ] );
                ClientBase targClient = GetClient( id );

                if ( client != null && targClient.AuthLevel >= client.AuthLevel )
                    return targClient.Nickname + " cannot be kicked.";

                Kick( id );

                return targClient.Nickname + " has been kicked.";
            }, 127 );

            RegisterCommand( "promote", delegate( String[] args, ClientBase client )
            {
                Int16 id = Int16.Parse( args[ 0 ] );
                ClientBase targClient = GetClient( id );

                if ( client != null && targClient.AuthLevel >= client.AuthLevel )
                    return targClient.Nickname + " cannot be promoted.";

                Promote( id );

                return targClient.Nickname + " has been promoted.";
            }, 255 );

            RegisterCommand( "demote", delegate( String[] args, ClientBase client )
            {
                Int16 id = Int16.Parse( args[ 0 ] );
                ClientBase targClient = GetClient( id );

                if ( client != null && targClient.AuthLevel >= client.AuthLevel )
                    return targClient.Nickname + " cannot be demoted.";

                Demote( id );

                return targClient.Nickname + " has been demoted.";
            }, 255 );

            RegisterCommand( "setauth", delegate( String[] args, ClientBase client )
            {
                Int16 id = Int16.Parse( args[ 0 ] );
                byte level = byte.Parse( args[ 1 ] );
                ClientBase targClient = GetClient( id );

                if ( client != null && targClient.AuthLevel >= client.AuthLevel )
                    return targClient.Nickname + "'s auth level cannot be modified.";

                targClient.SetAuthLevel( level );
                
                return targClient.Nickname + "'s auth level is now " + level + ".";
            }, 255 );

            RegisterCommand( "list", delegate( String[] args, ClientBase client )
            {
                return ListClients();
            }, 0 );
        }

        public static void RegisterCommand( String chatCommand, CommandDelegate commandDelegate, byte authLevel = 255 )
        {
            if ( !myCommands.ContainsKey( chatCommand ) )
                myCommands.Add( chatCommand, new ServerCommand( chatCommand, commandDelegate, authLevel ) );
            else
                myCommands[ chatCommand ] = new ServerCommand( chatCommand, commandDelegate, authLevel );
        }

        public static void Log( String message )
        {
            if ( ServerMessage != null )
                ServerMessage( null, new ServerMessageEventArgs( message ) );
        }

        public static void SinglePlayerStart()
        {
            myMaxClients = 1;
            mySinglePlayer = true;

            myThread = new Thread( new ThreadStart( MainLoop ) );
            myThread.Start();
        }

        public static void Start( int maxClients = DefaultMaxClients, int port = DefaultPort, bool lan = true )
        {
            myPort = port;
            myMaxClients = maxClients;
            myLan = lan;
            mySinglePlayer = false;

            myThread = new Thread( new ThreadStart( MainLoop ) );
            myThread.Start();
        }

        private static void MainLoop()
        {
            if ( mySinglePlayer )
                Log( "Starting single player server\n" );
            else
            {
                List<IPAddress> addresses = new List<IPAddress>();

                IPHostEntry info = Dns.GetHostEntry( IPAddress.Loopback );

                foreach ( IPAddress add in info.AddressList )
                {
                    if ( add.ToString().StartsWith( "5." ) )
                        addresses.Add( add );
                    else if ( add.ToString().StartsWith( "192.168." ) )
                    {
                        if ( myLan )
                            addresses.Add( add );
                    }
                    else if ( !myLan && add.ToString().Contains( '.' ) )
                        addresses.Add( add );
                }

                addresses.Add( IPAddress.Loopback );

                Log( "Starting multi player server..." +
                    "\n    Host: " + info.HostName + " @ " + String.Join<IPAddress>( ", ", addresses.ToArray() ) +
                    "\n    Port: " + myPort.ToString() +
                    "\n    Max clients: " + myMaxClients.ToString() + "\n" );

                myListeners = new TcpListener[ addresses.Count ];

                for ( int i = 0; i < myListeners.Length; ++i )
                    myListeners[ i ] = new TcpListener( addresses[ i ], myPort );
            }

            myClients = new ClientBase[ 8 ];
            myClientCount = 0;

            myJoinQueue = new List<ClientBase>();

            Log( "Loading content..." );

            ResourceLib.Res.LoadFromOrderFile( "Data" + System.IO.Path.DirectorySeparatorChar + "loadorder.txt" );
            Lewt.Shared.Scripts.Compile( Lewt.Shared.Scripts.Destination.Server );
            Lewt.Shared.Scripts.Initialise();

            Log( "Generating world..." );

            myOverworld = new OverworldMap( 32, 32 );
            myOverworld.Generate();

            myOverworld.GetOverworldTile( myOverworld.Width / 2 - 1, myOverworld.Height / 2 - 1 ).LoadChunks();
            myOverworld.GetOverworldTile( myOverworld.Width / 2, myOverworld.Height / 2 - 1 ).LoadChunks();
            myOverworld.GetOverworldTile( myOverworld.Width / 2 - 1, myOverworld.Height / 2 ).LoadChunks();
            myOverworld.GetOverworldTile( myOverworld.Width / 2, myOverworld.Height / 2 ).LoadChunks();

            myActiveMaps = new List<Map>();

            Log( "Complete\n" );

            if( !mySinglePlayer )
                for ( int i = 0; i < myListeners.Length; ++i )
                    myListeners[ i ].Start();

            myRunning = true;
            Log( "Server ready to accept clients\n" );

            myLastThinkTime = DateTime.Now;

            while ( myRunning )
            {
                if( !mySinglePlayer )
                    CheckForConnectionRequests();

                CheckForClientUpdates();

                if ( myClientCount == 0 && myJoinQueue.Count == 0 )
                    Thread.Sleep( 100 );
                else if ( ( DateTime.Now - myLastThinkTime ).TotalSeconds >= ThinkInterval )
                {
                    myLastThinkTime = DateTime.Now;

                    foreach( Map map in myActiveMaps )
                        map.Think( ThinkInterval );
                }
            }
        }

        public static void ActivateMap( Map map )
        {
            if ( !myActiveMaps.Contains( map ) )
            {
                myActiveMaps.Add( map );

                map.EntityAdded += new EntityAddedHandler( SendEntityAdded );
                map.EntityRemoved += new EntityRemovedHandler( SendEntityRemoved );
                map.EntityUpdated += new EntityUpdatedHandler( SendEntityUpdated );
                map.EntityChangeChunk += new EntityChangeChunkHandler( OnEntityChangeChunk );
            }
        }

        public static void AttemptAddLocalClient()
        {
            AttemptAddClient( new LocalClient() );
        }

        public static void AttemptAddClient( ClientBase client )
        {
            Log( "Attempting to add client from " + client.IPAddress.ToString() + "...\n" );

            if ( myClientCount + myJoinQueue.Count >= myMaxClients )
            {
                Log( "Rejected " + client.Nickname + " because server is full\n" );
                client.Disconnect( DisconnectReason.ServerFull );
                return;
            }

            for ( int i = 0; i < myMaxClients; ++i )
                if ( myClients[ i ] == null )
                {
                    bool taken = false;

                    for ( int j = 0; j < myJoinQueue.Count; ++j )
                        if ( myJoinQueue[ j ].ID == i )
                        {
                            taken = true;
                            break;
                        }

                    if ( taken )
                        continue;

                    client.ID = (short) i;
                    break;
                }

            myJoinQueue.Add( client );
        }

        public static void FinishAddingClient( ClientBase client )
        {
            Log( client.Nickname + " has joined the game (slot " + client.ID.ToString() + ")\n" );

            for ( int j = 0; j < myMaxClients; ++j )
                if ( myClients[ j ] != null && client.CurrentMap == myClients[ j ].CurrentMap )
                    myClients[ j ].SendPlayerJoin( client );

            myClients[ client.ID ] = client;
            ++myClientCount;
            return;
        }

        public static void ClientEnterMap( ClientBase client, Map map )
        {
            client.FindLocalChunks();

            for ( int j = 0; j < myMaxClients; ++j )
                if ( myClients[ j ] != null && j != client.ID )
                    myClients[ j ].SendPlayerEnterMap( client, map );
        }

        public static void RemoveClient( ClientBase client, DisconnectReason reason = DisconnectReason.ClientDisconnect )
        {
            if ( client != null && myClients[ client.ID ] == client )
            {
                myClients[ client.ID ] = null;
                --myClientCount;

                if( client.CurrentMap != null )
                    client.CurrentMap.RemoveEntity( client.PlayerEntity );

                Log( client.Nickname + " has left the game (" + reason.ToString() + ")\n" );

                for ( int i = 0; i < myMaxClients; ++i )
                    if ( myClients[ i ] != null && client.CurrentMap == myClients[ i ].CurrentMap )
                        myClients[ i ].SendPlayerLeave( client );
            }
        }

        public static void ClientLeaveMap( ClientBase client )
        {
            for ( int j = 0; j < myMaxClients; ++j )
                if ( myClients[ j ] != null && j != client.ID )
                    myClients[ j ].SendPlayerLeaveMap( client );
        }

        public static ClientBase GetClient( short clientID )
        {
            return myClients[ clientID ];
        }

        private static void CheckForConnectionRequests()
        {
            for( int i = 0; i < myListeners.Length; ++ i )
                while ( Running && myListeners[ i ].Pending() )
                    AttemptAddClient( new RemoteClient( myListeners[ i ].AcceptTcpClient() ) );
        }

        private static void CheckForClientUpdates()
        {
            for ( int i = myMaxClients - 1; i >= 0; --i )
            {
                if ( myClients[ i ] != null )
                {
                    ClientBase client = myClients[ i ];

                    client.CheckForPackets();

                    if ( client.SecondsSinceLastPacket >= 5 && !client.TimingOut )
                        client.SendCheckActive();

                    if ( client.SecondsSinceLastPacket >= 15 )
                        RemoveClient( client, DisconnectReason.Timeout );
                    else if( client.State == ClientState.Disconnected )
                        RemoveClient( client, DisconnectReason.ClientDisconnect );
                }
            }

            if ( myJoinQueue.Count != 0 )
            {
                for ( int i = myJoinQueue.Count - 1; i >= 0; --i )
                {
                    ClientBase client = myJoinQueue[ i ];

                    client.CheckForPackets();

                    if ( client.SecondsSinceLastPacket >= 5 && !client.TimingOut )
                        client.SendCheckActive();

                    if ( client.SecondsSinceLastPacket >= 15 )
                    {
                        myJoinQueue.Remove( client );
                        Log( "Client from " + client.IPAddress + " failed to connect: Client timed out.\n" );
                    }
                    else if ( client.State == ClientState.Disconnected )
                    {
                        myJoinQueue.Remove( client );
                        Log( "Client from " + client.IPAddress + " failed to connect.\n" );
                    }
                    else if ( client.State == ClientState.WorldMap )
                    {
                        myJoinQueue.Remove( client );
                        FinishAddingClient( client );
                    }
                }
            }
        }

        private static void SendEntityAdded( Entity entity )
        {
            byte[] data = entity.ToByteArray( true );

            for ( int i = 0; i < myMaxClients; ++i )
            {
                ClientBase client = myClients[ i ];

                if ( client != null && client.State == ClientState.Playing &&
                    entity.Map == client.CurrentMap &&
                    ( entity is Player || client.LocalChunks.Contains( entity.Chunk ) ) )
                    client.SendEntityAdded( entity, data );
            }
        }

        private static void SendEntityRemoved( Entity entity )
        {
            for ( int i = 0; i < myMaxClients; ++i )
            {
                ClientBase client = myClients[ i ];

                if ( client != null && client.State == ClientState.Playing &&
                    entity.Map == client.CurrentMap &&
                    ( entity is Player || client.LocalChunks.Contains( entity.Chunk ) ) )
                    client.SendEntityRemoved( entity );
            }
        }

        private static void SendEntityUpdated( Entity entity, byte[] data )
        {
            for ( int i = 0; i < myMaxClients; ++i )
            {
                ClientBase client = myClients[ i ];

                if ( client != null && client.State == ClientState.Playing &&
                    entity.Map == client.CurrentMap &&
                    ( entity is Player || client.LocalChunks.Contains( entity.Chunk ) ) )
                    client.SendEntityUpdated( entity, data );
            }
        }

        private static void OnEntityChangeChunk( Entity entity, Chunk oldChunk, Chunk newChunk )
        {
            byte[] entData = null;

            for ( int i = 0; i < myMaxClients; ++i )
            {
                ClientBase client = myClients[ i ];
                if ( client != null && entity.Map == client.CurrentMap )
                {
                    if ( entity == client.PlayerEntity )
                    {
                        if ( client.CurrentMap.IsExterior )
                            client.FindLocalOverworldTiles();

                        client.FindLocalChunks( newChunk );
                    }
                    else if ( !( entity is Player ) )
                    {
                        bool wasLocal = client.LocalChunks.Contains( oldChunk );
                        bool nowLocal = client.LocalChunks.Contains( newChunk );

                        if ( wasLocal && !nowLocal )
                            client.SendEntityRemoved( entity );
                        else if ( !wasLocal && nowLocal )
                        {
                            if ( entData == null )
                                entData = entity.ToByteArray( true );

                            client.SendEntityAdded( entity, entData );
                        }
                    }
                }
            }
        }

        public static String RunCommand( String command, ClientBase client = null )
        {
            String cmdType = command.Split( ' ' )[ 0 ];
            String[] cmdArgs = ( command.Length > cmdType.Length ?
                command.Substring( cmdType.Length + 1 ).Split( ' ' ) :
                new String[ 0 ] );

            if ( !myCommands.ContainsKey( cmdType ) )
                return "Command not found!";
            else
                return myCommands[ cmdType ].Execute( client, cmdArgs );
        }

        public static void ChatMessage( String message )
        {
            Log( "Server: " + message + "\n" );

            for ( int i = 0; i < myMaxClients; ++i )
                if ( myClients[ i ] != null )
                    myClients[ i ].SendChatMessage( message );
        }

        public static void ChatMessage( ClientBase sender, String message )
        {
            Log( sender.Nickname + ": " + message + "\n" );

            for ( int i = 0; i < myMaxClients; ++i )
                if ( myClients[ i ] != null )
                    myClients[ i ].SendChatMessage( sender, message );
        }

        public static String ListClients()
        {
            String str = "";
            str += myClientCount + " client" + ( myClientCount == 1 ? "" : "s" ) + " connected\n";
            str += "--------------------------------\n";
            str += " ID  | Name             | EntID \n";
            str += "--------------------------------\n";

            for ( int i = 0; i < myMaxClients; ++i )
            {
                if ( myClients[ i ] != null )
                {
                    String printStr = " " + i.ToString();
                    while ( printStr.Length < 5 )
                        printStr += " ";
                    printStr += "| " + myClients[ i ].Nickname;
                    while ( printStr.Length < 24 )
                        printStr += " ";
                    printStr += "| " + myClients[ i ].PlayerEntity.EntityID.ToString() + "\n";
                    str += printStr;
                }
            }

            return str;
        }

        public static void Kick( Int16 clientID )
        {
            if ( myClients[ clientID ] != null )
            {
                myClients[ clientID ].Disconnect( DisconnectReason.Kicked );
                RemoveClient( myClients[ clientID ], DisconnectReason.Kicked );
            }
        }

        public static void Promote( Int16 clientID )
        {
            if ( myClients[ clientID ] != null )
                myClients[ clientID ].Promote();
        }

        public static void Demote( Int16 clientID )
        {
            if ( myClients[ clientID ] != null )
                myClients[ clientID ].Demote();
        }

        public static void Stop()
        {
            for ( int i = 0; i < myMaxClients; ++i )
                if ( myClients[ i ] != null )
                    myClients[ i ].Disconnect( DisconnectReason.ServerStopping );

            if( !mySinglePlayer )
                for( int i = 0; i < myListeners.Length; ++ i )
                    myListeners[ i ].Stop();

            myRunning = false;
        }
    }
}
