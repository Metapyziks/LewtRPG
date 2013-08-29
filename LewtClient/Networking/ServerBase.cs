using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ResourceLib;

using Lewt.Shared.World;
using Lewt.Shared.Entities;
using Lewt.Shared.Networking;
using Lewt.Shared;
using Lewt.Shared.Stats;
using Lewt.Shared.Items;

namespace Lewt.Client.Networking
{
    public enum ServerState
    {
        PendingHandshake,
        PendingAuthentication,
        RequestingWorldMap,
        WorldMap,
        RequestingMap,
        Playing,
        Disconnected
    }

    public class ClientInfo
    {
        public Int16 ClientID;
        public Player PlayerEntity;
        public String Nickname;
        public Map CurrentMap;
    }

    public class ServerBase : RemoteNetworkedObject
    {
        private DateTime myLastReceivedTime;
        private DateTime mySyncRequestTime;

        private String myName;
        private String myPassword;
        private int myResourceCount;
        private OverworldMap myOverworld;
        private Map myCurrentMap;
        private int myChunkCount;

        private ClientInfo[] myClients;
        private int myMaxClients;
        private int myClientCount;

        internal ServerState State;

        public double SecondsSinceLastPacket
        {
            get
            {
                return ( DateTime.Now - myLastReceivedTime ).TotalSeconds;
            }
        }

        public int MaxClients
        {
            get
            {
                return myMaxClients;
            }
        }

        public int ClientCount
        {
            get
            {
                return myClientCount;
            }
        }

        public ClientInfo[] Clients
        {
            get
            {
                ClientInfo[] clients = new ClientInfo[ myClientCount ];

                for ( int i = 0, j = 0; i < myMaxClients; ++i )
                {
                    if ( myClients[ i ] != null )
                        clients[ j++ ] = myClients[ i ];

                    if ( j == myClientCount )
                        break;
                }

                return clients;
            }
        }

        public String Name
        {
            get
            {
                return myName;
            }
        }

        public OverworldMap Overworld
        {
            get
            {
                return myOverworld;
            }
        }

        public Map Map
        {
            get
            {
                return myCurrentMap;
            }
        }

        public ServerBase( String password = "" )
        {
            myPassword = password;
            State = ServerState.PendingHandshake;
            myLastReceivedTime = DateTime.Now;
        }

        protected override void OnReceivePacket( PacketID id, BinaryReader reader )
        {
            myLastReceivedTime = DateTime.Now;

            switch ( id )
            {
                case PacketID.CheckActive:
                    ReceiveCheckActive( reader );
                    break;
                case PacketID.Handshake:
                    ReceiveHandshake( reader );
                    break;
                case PacketID.Authenticate:
                    ReceiveAuthenticate( reader );
                    break;
                case PacketID.ResourceRequest:
                    ReceiveResourceRequest( reader );
                    break;
                case PacketID.Resource:
                    ReceiveResource( reader );
                    break;
                case PacketID.PlayerJoin:
                    ReceivePlayerJoin( reader );
                    break;
                case PacketID.PlayerLeave:
                    ReceivePlayerLeave( reader );
                    break;
                case PacketID.PlayerEnterMap:
                    ReceivePlayerEnterMap( reader );
                    break;
                case PacketID.PlayerLeaveMap:
                    ReceivePlayerLeaveMap( reader );
                    break;
                case PacketID.ChatMessage:
                    ReceiveChatMessage( reader );
                    break;
                case PacketID.WorldRequest:
                    ReceiveWorldRequest( reader );
                    break;
                case PacketID.PostWorld:
                    ReceivePostWorld( reader );
                    break;
                case PacketID.MapRequest:
                    ReceiveMapRequest( reader );
                    break;
                case PacketID.InteriorChunk:
                    ReceiveInteriorChunk( reader );
                    break;
                case PacketID.ExteriorChunk:
                    ReceiveExteriorChunk( reader );
                    break;
                case PacketID.DiscardChunk:
                    ReceiveDiscardChunk( reader );
                    break;
                case PacketID.PostMap:
                    ReceivePostMap( reader );
                    break;
                case PacketID.SyncTime:
                    ReceiveSyncTime( reader );
                    break;
                case PacketID.ModifyInventory:
                    ReceiveModifyInventory( reader );
                    break;
                case PacketID.EntityAdded:
                    ReceiveEntityAdded( reader );
                    break;
                case PacketID.EntityRemoved:
                    ReceiveEntityRemoved( reader );
                    break;
                case PacketID.EntityUpdated:
                    ReceiveEntityUpdated( reader );
                    break;
                case PacketID.CharPointRequest:
                    ReceiveCharPointRequest( reader );
                    break;
                case PacketID.CharacterCreate:
                    ReceiveCharacterCreate( reader );
                    break;
                case PacketID.Disconnect:
                    ReceiveDisconnect( reader );
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public virtual void Disconnect( DisconnectReason reason )
        {
            SendDisconnect( reason );
            State = ServerState.Disconnected;
        }
        
        public void SendCheckActive()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CheckActive );
            SendPacket();
        }

        private void ReceiveCheckActive( BinaryReader reader )
        {
            SendCheckActive();
        }

        public void SendHandshake()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Handshake );
            writer.Write( NetworkConstants.ProtocolVersion );
            writer.Write( GameClient.Nickname );
            SendPacket();
        }

        private void ReceiveHandshake( BinaryReader reader )
        {
            myName = reader.ReadString();

            State = ServerState.PendingAuthentication;
            SendAuthenticate( reader.ReadBoolean() );
        }

        public void SendAuthenticate( bool passwordRequired )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Authenticate );
            if( passwordRequired )
                writer.Write( myPassword );
            SendPacket();
        }

        private void ReceiveAuthenticate( BinaryReader reader )
        {
            GameClient.SetID( reader.ReadInt16() );

            myMaxClients = reader.ReadUInt16();

            myClients = new ClientInfo[ myMaxClients ];
            
            myClients[ GameClient.ID ] = new ClientInfo { ClientID = GameClient.ID, Nickname = GameClient.Nickname };

            myClientCount = 1;

            State = ServerState.RequestingMap;

            SendResourceRequest();
        }

        public void SendResourceRequest()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ResourceRequest );

            if ( this is LocalServer )
                writer.Write( (ushort) 0 );
            else
            {
                List<String> details = new List<string>();

                foreach ( String filePath in Directory.GetFiles( "Data" ) )
                {
                    if ( !filePath.EndsWith( Res.DefaultResourceExtension ) )
                        continue;

                    Dictionary<ArchiveProperty, String> props = Res.LoadArchiveProperties( filePath );

                    if ( props[ ArchiveProperty.Destination ] != ArchiveDest.Server.ToString() )
                    {
                        details.Add( props[ ArchiveProperty.Name ] );
                        details.Add( props[ ArchiveProperty.Hash ] );
                    }
                }

                if ( !Directory.Exists( "Data" + Path.DirectorySeparatorChar + "Downloaded" ) )
                    Directory.CreateDirectory( "Data" + Path.DirectorySeparatorChar + "Downloaded" );

                foreach ( String filePath in Directory.GetFiles( "Data" + Path.DirectorySeparatorChar + "Downloaded" ) )
                {
                    if ( !filePath.EndsWith( Res.DefaultResourceExtension ) )
                        continue;

                    Dictionary<ArchiveProperty, String> props = Res.LoadArchiveProperties( filePath );

                    if ( props[ ArchiveProperty.Destination ] != ArchiveDest.Server.ToString() )
                    {
                        details.Add( props[ ArchiveProperty.Name ] );
                        details.Add( props[ ArchiveProperty.Hash ] );
                    }
                }

                writer.Write( (ushort) ( details.Count / 2 ) );

                for ( int i = 0; i < details.Count; i += 2 )
                {
                    if ( details[ i ].Length <= 16 )
                        writer.Write( details[ i ] );
                    else
                        writer.Write( details[ i ].Substring( 0, 16 ) );

                    writer.Write( details[ i + 1 ] );
                }
            }

            SendPacket();
        }

        public void ReceiveResourceRequest( BinaryReader reader )
        {
            myResourceCount = reader.ReadUInt16();

            GameClient.ClientMessage( myResourceCount.ToString() + " file" + ( myResourceCount == 1 ? "" : "s" ) + " must be downloaded from the server.\n" );

            if ( this is LocalServer )
                SendCharPointRequest();
            else if ( myResourceCount == 0 )
            {
                ushort count = reader.ReadUInt16();

                Dictionary<String, Dictionary<ArchiveProperty, String>> foundArchives = new Dictionary<string, Dictionary<ArchiveProperty, string>>();

                foreach ( String filePath in Directory.GetFiles( "Data" ) )
                {
                    if ( !filePath.EndsWith( Res.DefaultResourceExtension ) )
                        continue;

                    Dictionary<ArchiveProperty, String> props = Res.LoadArchiveProperties( filePath );

                    if ( props[ ArchiveProperty.Destination ] != ArchiveDest.Server.ToString() )
                        foundArchives.Add( filePath, props );
                }

                foreach ( String filePath in Directory.GetFiles( "Data" + Path.DirectorySeparatorChar + "Downloaded" ) )
                {
                    if ( !filePath.EndsWith( Res.DefaultResourceExtension ) )
                        continue;

                    Dictionary<ArchiveProperty, String> props = Res.LoadArchiveProperties( filePath );

                    if ( props[ ArchiveProperty.Destination ] != ArchiveDest.Server.ToString() )
                        foundArchives.Add( filePath, props );
                }

                Res.UnmountAllArchives();
                Res.UnloadAllArchives();

                for ( int i = 0; i < count; ++i )
                {
                    String name = reader.ReadString();
                    String hash = reader.ReadString();

                    bool found = false;

                    foreach ( String filePath in foundArchives.Keys )
                    {
                        if ( foundArchives[ filePath ][ ArchiveProperty.Name ].StartsWith( name ) &&
                            foundArchives[ filePath ][ ArchiveProperty.Hash ] == hash )
                        {
                            int archive = Res.LoadArchive( filePath );
                            Res.MountArchive( archive );
                            found = true;
                            break;
                        }
                    }

                    if ( !found )
                    {
                        Disconnect( DisconnectReason.ResourceNotFound );
                        GameClient.ClientMessage( "Failed to join the server: Resource with name '" + name + "' not found or not at correct version!\n" );
                        break;
                    }
                }

                Lewt.Shared.Scripts.Compile( Lewt.Shared.Scripts.Destination.Client );
                Lewt.Shared.Scripts.Initialise();

                SendCharPointRequest();
            }
        }

        private void ReceiveResource( BinaryReader reader )
        {
            ushort resourceNo = reader.ReadUInt16();

            String name = reader.ReadString().Replace( " ", "" ).ToLower();

            GameClient.ClientMessage( "(" + ( resourceNo + 1 ).ToString() + "/" + myResourceCount.ToString() + ") Downloading " + name + "\n" );
            
            char sep = Path.DirectorySeparatorChar;
            name = "Data" + sep + "Downloaded" + sep + name;

            int num = 0;

            while ( File.Exists( name + "_" + num + Res.DefaultResourceExtension ) )
                ++num;

            int length = reader.ReadInt32();

            FileStream fileStr = new FileStream( name + "_" + num + Res.DefaultResourceExtension, FileMode.Create, FileAccess.Write );
            byte[] buffer = new byte[ 2048 ];
            int read = 0;
            while ( read < length )
            {
                int toRead = Math.Min( 2048, length - read );
                reader.Read( buffer, 0, toRead );
                fileStr.Write( buffer, 0, toRead );
                read += 2048;
            }
            fileStr.Close();

            if ( resourceNo == myResourceCount - 1 )
                SendResourceRequest();
        }

        public void SendWorldRequest()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.WorldRequest );
            SendPacket();

            State = ServerState.RequestingWorldMap;
        }

        private void ReceiveWorldRequest( BinaryReader reader )
        {
            myOverworld = new OverworldMap( reader, false );

            SendPostWorld();
        }

        public void SendPostWorld()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PostWorld );
            SendPacket();
        }

        private void ReceivePostWorld( BinaryReader reader )
        {
            myClientCount = reader.ReadInt16();

            for ( int i = 0; i < myClientCount; ++i )
            {
                Int16 id = reader.ReadInt16();
                String name = reader.ReadString();
                UInt16 mapID = reader.ReadUInt16();

                myClients[ id ] = new ClientInfo { ClientID = id, Nickname = name };

                if ( mapID != 0xFFFF )
                    myClients[ id ].CurrentMap = Overworld.GetMap( mapID );
            }

            ++myClientCount;

            State = ServerState.WorldMap;
        }

        private void ReceivePlayerJoin( BinaryReader reader )
        {
            Int16 id = reader.ReadInt16();
            String name = reader.ReadString();

            myClients[ id ] = new ClientInfo { ClientID = id, Nickname = name };
            myClientCount++;

            GameClient.ClientMessage( name + " joined (slot " + id.ToString() + ")\n" );
        }

        private void ReceivePlayerLeave( BinaryReader reader )
        {
            Int16 id = reader.ReadInt16();
            String name = myClients[ id ].Nickname;
            myClients[ id ] = null;
            myClientCount--;

            GameClient.ClientMessage( name + " left (slot " + id.ToString() + ")\n" );
        }

        private void ReceivePlayerEnterMap( BinaryReader reader )
        {
            Int16 id = reader.ReadInt16();
            UInt16 mapId = reader.ReadUInt16();

            myClients[ id ].CurrentMap = GameClient.Overworld.GetMap( mapId );

            string name = myClients[ id ].Nickname;

            if ( Map != null && Map.ID == mapId )
            {
                UInt32 entId = reader.ReadUInt32();

                Player player = Map.GetEntity( entId ) as Player;
                player.PlayerName = name;

                myClients[ id ].PlayerEntity = player;
            }

            GameClient.ClientMessage( name + " entered map #" + mapId.ToString() + "\n" );
        }

        public void SendPlayerLeaveMap()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PlayerLeaveMap );
            SendPacket();

            myCurrentMap = null;
            State = ServerState.WorldMap;
        }

        private void ReceivePlayerLeaveMap( BinaryReader reader )
        {
            Int16 id = reader.ReadInt16();
            string name = myClients[ id ].Nickname;

            Map map = myClients[ id ].CurrentMap;
            myClients[ id ].CurrentMap = null;
            myClients[ id ].PlayerEntity = null;

            GameClient.ClientMessage( name + " left map #" + map.ID.ToString() + "\n" );
        }

        public void SendMapRequest( UInt16 mapID )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.MapRequest );
            writer.Write( mapID );
            SendPacket();
        }

        private void ReceiveMapRequest( BinaryReader reader )
        {
            bool isOverworld = reader.ReadBoolean();
            if ( isOverworld )
                myCurrentMap = Overworld;
            else
            {
                UInt16 id = reader.ReadUInt16();
                myCurrentMap = Overworld.GetMap( id );

                myCurrentMap.Clear();
                myChunkCount = reader.ReadUInt16();
            }

            GameClient.ClientMessage( "Loading Chunks..." );
        }

        private void ReceiveInteriorChunk( BinaryReader reader )
        {
            ushort chunkNo = reader.ReadUInt16();

            Chunk.Load( reader.BaseStream, myCurrentMap );

            if ( chunkNo == myChunkCount - 1 )
            {
                myCurrentMap.PostWorldInitialize();
                SendPostMap();

                GameClient.ClientMessage( myChunkCount + " chunks loaded\n" );
            }
        }

        private void ReceiveExteriorChunk( BinaryReader reader )
        {
            int x = reader.ReadInt16() * Overworld.ChunkWidth;
            int y = reader.ReadInt16() * Overworld.ChunkHeight;

            OverworldTile tile = Overworld.GetOverworldTile( x, y );

            tile.LoadChunks( reader );
        }

        private void ReceiveDiscardChunk( BinaryReader reader )
        {
            int x = reader.ReadInt16() * Overworld.ChunkWidth;
            int y = reader.ReadInt16() * Overworld.ChunkHeight;

            OverworldTile tile = Overworld.GetOverworldTile( x, y );

            tile.UnloadChunks();
        }

        public void SendPostMap()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PostMap );
            SendPacket();

            mySyncRequestTime = DateTime.Now;
        }

        private void ReceivePostMap( BinaryReader reader )
        {
            bool received = reader.ReadBoolean();

            if ( received )
            {
                UInt32 playerEntID = reader.ReadUInt32();
                GameClient.SetPlayerEntity( Map.GetEntity( playerEntID ) as Player );

                for ( int i = 0; i < myMaxClients; ++i )
                {
                    if ( myClients[ i ] != null && myClients[ i ].ClientID != GameClient.ID &&
                        myClients[ i ].CurrentMap == Map )
                    {
                        UInt32 entID = reader.ReadUInt32();
                        String name = myClients[ i ].Nickname;

                        Player ent = Map.GetEntity( entID ) as Player;
                        ent.PlayerName = name;

                        myClients[ i ].PlayerEntity = ent;
                    }
                }

                State = ServerState.Playing;
            }
            else
                SendPostMap();
        }

        private void ReceiveSyncTime( BinaryReader reader )
        {
            Double replyDelay = ( DateTime.Now - mySyncRequestTime ).TotalSeconds;

            ulong ticks = reader.ReadUInt64() + Tools.SecondsToTicks( replyDelay / 2.0 );

            Map.SetStartTime( DateTime.Now, ticks );
        }

        public void SendChatMessage( String message )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ChatMessage );
            writer.Write( message );
            SendPacket();
        }

        private void ReceiveChatMessage( BinaryReader reader )
        {
            Int16 clientID = reader.ReadInt16();

            if ( clientID == -1 )
                GameClient.ClientMessage( reader.ReadString() + "\n" );
            else
                GameClient.ClientMessage( myClients[ clientID ].Nickname + ": " + reader.ReadString() + "\n" );
        }

        public void SendCharacterMove( Player player, WalkDirection dir )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharacterMove );
            writer.Write( (byte) dir );
            writer.Write( Map.TimeTicks );
            writer.Write( player.OriginX );
            writer.Write( player.OriginY );
            SendPacket();
        }

        public void SendCharacterStop( Player player )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharacterStop );
            writer.Write( player.OriginX );
            writer.Write( player.OriginY );
            SendPacket();
        }

        public void SendSpellCast( Player caster, double angle )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.SpellCast );
            writer.Write( caster.EntityID );
            writer.Write( caster.OriginX );
            writer.Write( caster.OriginY );
            writer.Write( (byte) Math.Round( angle / Math.PI * 2 ) );
            SendPacket();
        }

        public void SendPlayerUse( Player user, Entity target )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.UseEntity );
            writer.Write( user.EntityID );
            writer.Write( target.EntityID );
            SendPacket();
        }

        public void SendPlayerViewInventory( Player player )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ViewInventory );
            writer.Write( player.EntityID );
            SendPacket();
        }

        public void SendTransferItem( Entity owner, Entity target, InventorySlot slot )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ModifyInventory );
            writer.Write( (byte) 0x00 );
            writer.Write( owner.EntityID );
            writer.Write( target.EntityID );
            writer.Write( slot.ID );
            SendPacket();
        }

        public void SendTransferItem( Entity entityA, Entity entityB, InventorySlot slotA, InventorySlot slotB )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ModifyInventory );
            writer.Write( (byte) 0x01 );
            writer.Write( entityA.EntityID );
            writer.Write( entityB.EntityID );
            writer.Write( slotA.ID );
            writer.Write( slotB.ID );
            SendPacket();
        }

        private void ReceiveModifyInventory( BinaryReader reader )
        {
            IContainer owner = GameClient.Map.GetEntity( reader.ReadUInt32() ) as IContainer;
            bool wholeInventory = reader.ReadBoolean();

            if ( wholeInventory )
                owner.Inventory = new Inventory( owner as Entity, reader );
            else
            {
                UInt16 slots = reader.ReadUInt16();

                for ( int i = 0; i < slots; ++i )
                {
                    InventorySlot slot = owner.Inventory[ reader.ReadUInt16() ];
                    int count = reader.ReadInt32();
                    if ( count == 0 )
                        slot.Clear();
                    else
                    {
                        slot.Item = Item.Load( GameClient.Map, reader );
                        slot.Count = count;
                    }
                }
            }
        }

        public void SendUseItem( Player player, InventorySlot slot )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.UseItem );
            writer.Write( player.EntityID );
            writer.Write( slot.ID );
            SendPacket();
        }

        public void SendCharPointRequest()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharPointRequest );
            SendPacket();
        }

        private void ReceiveCharPointRequest( BinaryReader reader )
        {
            GameClient.CharacterBaseAttribPoints = reader.ReadUInt16();
            GameClient.CharacterBaseSkillPoints = reader.ReadUInt16();
            GameClient.CharacterUnusedAttribPoints = reader.ReadUInt16();
            GameClient.CreateCharacter = true;
        }

        public void SendCharacterCreate( CharacterCreationOutput output )
        {
            myClients[ GameClient.ID ].Nickname = output.PlayerName;

            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharacterCreate );
            writer.Write( output.PlayerName );
            writer.Write( (ushort) CharAttribute.GetAll().Length );
            foreach ( CharAttribute attrib in CharAttribute.GetAll() )
            {
                writer.Write( attrib.ID );
                writer.Write( (byte) output.GetAttributePoints( attrib ) );
            }
            writer.Write( (ushort) CharSkill.GetAll().Length );
            foreach ( CharSkill skill in CharSkill.GetAll() )
            {
                writer.Write( skill.ID );
                writer.Write( (byte) output.GetBaseSkillPoints( skill ) );
            }

            SendPacket();
        }

        private void ReceiveCharacterCreate( BinaryReader reader )
        {
            SendWorldRequest();
        }

        public void SendDisconnect( DisconnectReason reason )
        {
            try
            {
                BinaryWriter writer = GetWriter();
                writer.Write( (byte) PacketID.Disconnect );
                writer.Write( (byte) reason );
                SendPacket();
            }
            catch
            {

            }
        }

        public void SendCharacterResurrect() {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Resurrect );
            SendPacket();
        }

        private void ReceiveEntityAdded( BinaryReader reader )
        {
            Entity ent = Entity.Load( reader, true );
            Map.AddEntity( ent );
            if ( ent is Light )
                ( ent as Light ).Update();
        }

        private void ReceiveEntityRemoved( BinaryReader reader )
        {
            UInt32 entID = reader.ReadUInt32();
            Map.RemoveEntity( entID );
        }

        private void ReceiveEntityUpdated( BinaryReader reader )
        {
            UInt32 entID = reader.ReadUInt32();
            Entity ent = Map.GetEntity( entID );
            byte[] data = reader.ReadBytes( reader.ReadUInt16() );
            if( ent != null )
                ent.ReceiveStateUpdate( data );
        }

        private void ReceiveDisconnect( BinaryReader reader )
        {
            DisconnectReason reason = (DisconnectReason) reader.ReadByte();
            String message = reader.ReadString();
            State = ServerState.Disconnected;

            GameClient.ClientMessage( "You were dropped from the server (" + reason.ToString() + ")\n" );
        }
    }
}
