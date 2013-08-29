using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ResourceLib;

using Lewt.Shared;
using Lewt.Shared.Entities;
using Lewt.Shared.World;
using Lewt.Shared.Networking;
using Lewt.Shared.Stats;
using Lewt.Shared.Items;

namespace Lewt.Server.Networking
{
    public enum ClientState
    {
        PendingHandshake,
        PendingAuthentication,
        RequestingWorldMap,
        WorldMap,
        RequestingMap,
        Playing,
        Disconnected
    }

    public class ClientBase : RemoteNetworkedObject
    {
        private DateTime myLastReceivedTime;
        private bool myTimingOut;
        private bool myDownloadedResources;
        private byte myAuthLevel;

        public ClientState State;
        public Int16 ID;
        public String Nickname;
        public Player PlayerEntity;

        public Map CurrentMap;

        public List<Chunk> LocalChunks;
        public List<OverworldTile> LocalOverworldTiles;

        public double SecondsSinceLastPacket
        {
            get
            {
                return ( DateTime.Now - myLastReceivedTime ).TotalSeconds;
            }
        }

        public bool TimingOut
        {
            get
            {
                return myTimingOut;
            }
        }

        public byte AuthLevel
        {
            get
            {
                return myAuthLevel;
            }
        }

        public bool IsAdmin
        {
            get
            {
                return myAuthLevel >= 127;
            }
        }

        public ClientBase()
        {
            State = ClientState.PendingHandshake;
            myLastReceivedTime = DateTime.Now;
            myDownloadedResources = false;
            myAuthLevel = 0;

            LocalChunks = new List<Chunk>();
            LocalOverworldTiles = new List<OverworldTile>();
        }

        public void FindLocalOverworldTiles( OverworldTile startTile = null )
        {
            if( CurrentMap is OverworldMap )
                startTile = startTile ?? GameServer.Overworld.GetOverworldTile( PlayerEntity.OriginX, PlayerEntity.OriginY );

            List<OverworldTile> oldTiles = LocalOverworldTiles;
            LocalOverworldTiles = new List<OverworldTile>();

            if ( CurrentMap is OverworldMap )
            {
                int minX = startTile.X - GameServer.Overworld.ChunkWidth;
                int minY = startTile.Y - GameServer.Overworld.ChunkHeight;
                int maxX = startTile.X + GameServer.Overworld.ChunkWidth;
                int maxY = startTile.Y + GameServer.Overworld.ChunkHeight;

                for ( int x = minX; x <= maxX; x += GameServer.Overworld.ChunkWidth )
                    for ( int y = minY; y <= maxY; y += GameServer.Overworld.ChunkHeight )
                    {
                        OverworldTile tile = GameServer.Overworld.GetOverworldTile( x, y );
                        LocalOverworldTiles.Add( tile );

                        if ( !tile.ChunksLoaded )
                            tile.LoadChunks();

                        if ( !oldTiles.Contains( tile ) )
                            SendExteriorChunk( tile );
                        else
                            oldTiles.Remove( tile );
                    }
            }

            foreach ( OverworldTile tile in oldTiles )
            {
                SendDiscardChunk( tile );

                bool dispose = true;

                foreach ( ClientBase client in GameServer.Clients )
                {
                    if ( client != this && client.LocalOverworldTiles.Contains( tile ) )
                    {
                        dispose = false;
                        break;
                    }
                }

                if ( dispose )
                    tile.UnloadChunks();
            }
        }

        public void FindLocalChunks( Chunk startChunk = null )
        {
            startChunk = startChunk ?? PlayerEntity.Chunk;

            List<Chunk> oldChunks = LocalChunks;
            LocalChunks = new List<Chunk>();

            int horzThreshold = 16;
            int vertThreshold = 12;

            foreach ( Chunk chunk in CurrentMap.TileChunks )
            {
                if ( chunk == startChunk || (
                        ( chunk.X - startChunk.X - startChunk.Width ) < horzThreshold &&
                        ( startChunk.X - chunk.X - chunk.Width ) < horzThreshold &&
                        ( chunk.Y - startChunk.Y - startChunk.Height ) < vertThreshold &
                        ( startChunk.Y - chunk.Y - chunk.Height ) < vertThreshold ) )
                {
                    LocalChunks.Add( chunk );

                    if ( !oldChunks.Contains( chunk ) )
                    {
                        foreach ( Entity ent in chunk.Entities )
                            if ( !( ent is Player ) )
                                SendEntityAdded( ent, ent.ToByteArray( true ) );
                    }
                    else
                        oldChunks.Remove( chunk );
                }
            }

            foreach ( Chunk chunk in oldChunks )
                foreach ( Entity ent in chunk.Entities )
                    if( !( ent is Player ) )
                        SendEntityRemoved( ent );
        }

        public void Promote()
        {
            if ( !IsAdmin )
            {
                myAuthLevel = 127;
                SendChatMessage( "You have been promoted to admin!" );
            }
        }

        public void Demote()
        {
            if ( IsAdmin )
            {
                myAuthLevel = 0;
                SendChatMessage( "You have lost your admin powers!" );
            }
        }

        public void SetAuthLevel( byte newLevel )
        {
            myAuthLevel = newLevel;
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
                case PacketID.WorldRequest:
                    ReceiveWorldRequest( reader );
                    break;
                case PacketID.PostWorld:
                    ReceivePostWorld( reader );
                    break;
                case PacketID.PlayerLeaveMap:
                    ReceivePlayerLeaveMap( reader );
                    break;
                case PacketID.ChatMessage:
                    ReceiveChatMessage( reader );
                    break;
                case PacketID.MapRequest:
                    ReceiveMapRequest( reader );
                    break;
                case PacketID.PostMap:
                    ReceivePostMap( reader );
                    break;
                case PacketID.CharacterMove:
                    ReceiveCharacterMove( reader );
                    break;
                case PacketID.CharacterStop:
                    ReceiveCharacterStop( reader );
                    break;
                case PacketID.SpellCast:
                    ReceiveSpellCast( reader );
                    break;
                case PacketID.UseEntity:
                    ReceivePlayerUse( reader );
                    break;
                case PacketID.ViewInventory:
                    ReceivePlayerViewInventory( reader );
                    break;
                case PacketID.ModifyInventory:
                    ReceiveModifyInventory( reader );
                    break;
                case PacketID.UseItem:
                    ReceiveUseItem( reader );
                    break;
                case PacketID.CharPointRequest:
                    ReceiveCharPointRequest( reader );
                    break;
                case PacketID.CharacterCreate:
                    ReceiveCharacterCreate( reader );
                    break;
                case PacketID.Resurrect:
                    ReceiveCharacterResurrect( reader );
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
            State = ClientState.Disconnected;
        }

        public void SendCheckActive()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CheckActive );
            SendPacket();
            myTimingOut = true;
        }

        private void ReceiveCheckActive( BinaryReader reader )
        {
            myTimingOut = false;
        }

        public void SendHandshake()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Handshake );
            writer.Write( GameServer.Name );
            writer.Write( GameServer.PasswordRequired );
            SendPacket();
        }

        private void ReceiveHandshake( BinaryReader reader )
        {
            ushort clientVersion = reader.ReadUInt16();
            if ( clientVersion != NetworkConstants.ProtocolVersion )
            {
                Disconnect( DisconnectReason.ProtocolVersionMismatch );
                return;
            }

            Nickname = reader.ReadString();

            SendHandshake();
        }

        public void SendAuthenticate()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Authenticate );
            writer.Write( (short) ID );
            writer.Write( (ushort) GameServer.MaxClients );
            SendPacket();
        }

        private void ReceiveAuthenticate( BinaryReader reader )
        {
            if ( GameServer.PasswordRequired )
            {
                if ( reader.ReadString() != GameServer.Password )
                {
                    Disconnect( DisconnectReason.BadPassword );
                    return;
                }
            }

            SendAuthenticate();
            State = ClientState.RequestingMap;
        }

        public void SendResourceRequest( int[] neededArchives )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ResourceRequest );
            writer.Write( (ushort) neededArchives.Length );

            if ( neededArchives.Length == 0 && !( this is LocalClient ) )
            {
                List<String> details = new List<string>();

                foreach ( int archive in Res.GetAllLoadedArchives() )
                {
                    if ( Res.GetArchiveDestination( archive ) == ArchiveDest.Server )
                        continue;

                    String name = Res.GetArchiveProperty( archive, ArchiveProperty.Name );
                    String hash = Res.GetArchiveProperty( archive, ArchiveProperty.Hash );

                    if ( name.Length > 16 )
                        name = name.Substring( 0, 16 );

                    details.Add( name );
                    details.Add( hash );
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

            if ( neededArchives.Length > 0 && !( this is LocalClient ) )
            {
                myDownloadedResources = true;

                ushort resourceNo = 0;
                
                foreach ( int archive in neededArchives )
                    SendResource( resourceNo++, archive );
            }
        }

        private void ReceiveResourceRequest( BinaryReader reader )
        {
            ushort archives = reader.ReadUInt16();

            if ( this is LocalClient )
            {
                SendResourceRequest( new int[ 0 ] );
                return;
            }

            String[] clientNames = new String[ archives ];
            String[] clientHashes = new String[ archives ];

            for ( int i = 0; i < archives; ++i )
            {
                clientNames[ i ] = reader.ReadString();
                clientHashes[ i ] = reader.ReadString();
            }

            List<int> neededArchives = new List<int>();

            foreach ( int archive in Res.GetAllLoadedArchives() )
            {
                if ( Res.GetArchiveDestination( archive ) == ArchiveDest.Server )
                    continue;

                String name = Res.GetArchiveProperty( archive, ArchiveProperty.Name );
                String hash = Res.GetArchiveProperty( archive, ArchiveProperty.Hash );

                if ( name.Length > 16 )
                    name = name.Substring( 0, 16 );

                if ( !clientNames.Contains( name ) )
                {
                    neededArchives.Add( archive );
                    continue;
                }

                int index = -1;
                bool found = false;
                while ( ( index = Array.IndexOf( clientNames, name, index + 1 ) ) != -1 && !found )
                    if ( clientHashes[ index ] == hash )
                        found = true;

                if ( !found )
                    neededArchives.Add( archive );
            }

            if ( neededArchives.Count == 0 || !myDownloadedResources )
                SendResourceRequest( neededArchives.ToArray() );
            else
                Disconnect( DisconnectReason.ResourceNotFound );
        }

        public void SendResource( ushort resourceNo, int archive )
        {
            byte[] data = File.ReadAllBytes( Res.GetArchiveProperty( archive, ArchiveProperty.FilePath ) );

            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Resource );
            writer.Write( resourceNo );
            writer.Write( ( Res.GetArchiveDestination( archive ) == ArchiveDest.Client ? "cl_ " : "sh_" ) + Res.GetArchiveProperty( archive, ArchiveProperty.Name ) );
            writer.Write( data.Length );
            writer.Write( data );
            SendPacket();
        }

        public void SendWorldRequest()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.WorldRequest );
            GameServer.Overworld.Save( writer );
            SendPacket();
        }

        private void ReceiveWorldRequest( BinaryReader reader )
        {
            SendWorldRequest();

            State = ClientState.RequestingWorldMap;
        }

        public void SendPostWorld()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PostWorld );
            writer.Write( (Int16) GameServer.Clients.Length );

            foreach ( ClientBase client in GameServer.Clients )
            {
                writer.Write( client.ID );
                writer.Write( client.Nickname );
                writer.Write( client.CurrentMap != null ? client.CurrentMap.ID : 0xFFFF );
            }

            SendPacket();
        }

        private void ReceivePostWorld( BinaryReader reader )
        {
            SendPostWorld();

            State = ClientState.WorldMap;
        }

        public void SendPlayerJoin( ClientBase client )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PlayerJoin );
            writer.Write( client.ID );
            writer.Write( client.Nickname );
            SendPacket();
        }

        public void SendPlayerLeave( ClientBase client )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PlayerLeave );
            writer.Write( client.ID );
            SendPacket();
        }

        public void SendPlayerEnterMap( ClientBase client, Map map )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PlayerEnterMap );
            writer.Write( client.ID );
            writer.Write( map.ID );

            if( CurrentMap == map )
                writer.Write( client.PlayerEntity.EntityID );

            SendPacket();
        }

        public void SendPlayerLeaveMap( ClientBase client )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PlayerLeaveMap );
            writer.Write( client.ID );
            SendPacket();
        }

        private void ReceivePlayerLeaveMap( BinaryReader reader )
        {
            Map map = CurrentMap;
            CurrentMap = null;
            LocalChunks = new List<Chunk>();

            GameServer.ClientLeaveMap( this );
            map.RemoveEntity( PlayerEntity );
        }

        public void SendMapRequest()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.MapRequest );
            writer.Write( CurrentMap is OverworldMap );
            if ( !( CurrentMap is OverworldMap ) )
            {
                writer.Write( CurrentMap.ID );
                writer.Write( (ushort) CurrentMap.TileChunks.Length );
            }
            SendPacket();
        }

        private void ReceiveMapRequest( BinaryReader reader )
        {
            UInt16 id = reader.ReadUInt16();

            if ( id == 0xFFFF )
                CurrentMap = GameServer.Overworld;
            else
                CurrentMap = GameServer.Overworld.GetMap( id );

            if ( CurrentMap is Dungeon )
            {
                Dungeon dungeon = CurrentMap as Dungeon;

                if ( !dungeon.HasGenerated )
                    dungeon.Generate();
            }

            GameServer.ActivateMap( CurrentMap );

            if ( CurrentMap is OverworldMap )
            {
                OverworldMap map = CurrentMap as OverworldMap;

                PlayerEntity.OriginX = map.Width / 2 + 8;
                PlayerEntity.OriginY = map.Height / 2 + 8;
                map.AddEntity( PlayerEntity );
            }
            else if ( CurrentMap is Dungeon )
            {
                Dungeon map = CurrentMap as Dungeon;

                Chunk randChunk = map.TileChunks[ (int) ( Tools.Random() * map.TileChunks.Length ) ];
                PlayerEntity.OriginX = randChunk.X + randChunk.Width / 2;
                PlayerEntity.OriginY = randChunk.Y + randChunk.Height / 2;
                map.AddEntity( PlayerEntity );
            }

            State = ClientState.RequestingMap;
            SendMapRequest();

            FindLocalOverworldTiles();

            if ( CurrentMap is OverworldMap )
                SendPostMap( false );
            else
            {
                Chunk[] chunks = CurrentMap.TileChunks;

                for ( int i = 0; i < chunks.Length; ++i )
                    SendInteriorChunk( (ushort) i, chunks[ i ] );
            }
        }

        public void SendInteriorChunk( ushort chunkNo, Chunk chunk )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.InteriorChunk );
            writer.Write( chunkNo );
            chunk.Save( writer.BaseStream, false );
            SendPacket();
        }

        public void SendExteriorChunk( OverworldTile tile )
        {
            if ( !tile.ChunksLoaded )
                return;

            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ExteriorChunk );
            writer.Write( (Int16) ( tile.X / GameServer.Overworld.ChunkWidth ) );
            writer.Write( (Int16) ( tile.Y / GameServer.Overworld.ChunkHeight ) );

            for ( int x = 0; x < GameServer.Overworld.ChunkWidth / OverworldTile.SubChunkSize; ++x )
                for ( int y = 0; y < GameServer.Overworld.ChunkHeight / OverworldTile.SubChunkSize; ++y )
                    tile.Chunks[ x, y ].Save( writer.BaseStream, false );

            SendPacket();
        }

        public void SendDiscardChunk( OverworldTile tile )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.DiscardChunk );
            writer.Write( (Int16) ( tile.X / GameServer.Overworld.ChunkWidth ) );
            writer.Write( (Int16) ( tile.Y / GameServer.Overworld.ChunkHeight ) );
            SendPacket();
        }

        public void SendPostMap( bool received = true )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.PostMap );
            writer.Write( received );

            if ( received )
            {
                writer.Write( PlayerEntity.EntityID );

                foreach ( ClientBase client in GameServer.Clients )
                    if ( client != this && client.CurrentMap == CurrentMap )
                        writer.Write( client.PlayerEntity.EntityID );
            }
            SendPacket();
        }

        private void ReceivePostMap( BinaryReader reader )
        {
            SendSyncTime();
            SendEntityAdded( PlayerEntity, PlayerEntity.ToByteArray( true ) );
            foreach ( ClientBase client in GameServer.Clients )
                SendEntityAdded( client.PlayerEntity, client.PlayerEntity.ToByteArray( true ) );
            FindLocalChunks( PlayerEntity.Chunk );
            SendPostMap();
            State = ClientState.Playing;

            GameServer.ClientEnterMap( this, CurrentMap );
        }

        public void SendSyncTime()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.SyncTime );
            writer.Write( CurrentMap.TimeTicks );
            SendPacket();
        }

        public void SendChatMessage( String message )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ChatMessage );
            writer.Write( (Int16) ( -1 ) );
            writer.Write( message );
            SendPacket();
        }

        public void SendChatMessage( ClientBase sender, String message )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ChatMessage );
            writer.Write( sender.ID );
            writer.Write( message );
            SendPacket();
        }

        private void ReceiveChatMessage( BinaryReader reader )
        {
            String message = reader.ReadString();
            if ( message.StartsWith( "/" ) )
            {
                String result = GameServer.RunCommand( message.Substring( 1 ), this );
                if( result != "" )
                    SendChatMessage( result );
            }
            else
                GameServer.ChatMessage( this, message );
        }

        private void ReceiveCharacterMove( BinaryReader reader )
        {
            WalkDirection dir = (WalkDirection) reader.ReadByte();
            ulong startTime = reader.ReadUInt64();
            OpenTK.Vector2d startPos = new OpenTK.Vector2d( reader.ReadDouble(), reader.ReadDouble() );

            PlayerEntity.StartWalking( dir, startTime, startPos );
        }

        private void ReceiveCharacterStop( BinaryReader reader )
        {
            double x = reader.ReadDouble();
            double y = reader.ReadDouble();

            PlayerEntity.StopWalking( new OpenTK.Vector2d( x, y ) );
        }

        private void ReceiveSpellCast( BinaryReader reader )
        {
            Player caster = CurrentMap.GetEntity( reader.ReadUInt32() ) as Player;
            caster.OriginX = reader.ReadDouble();
            caster.OriginY = reader.ReadDouble();
            double angle = reader.ReadByte() * Math.PI / 2.0;
            caster.Cast( caster.EquippedSpell, angle );
        }

        private void ReceivePlayerUse( BinaryReader reader )
        {
            Player user = CurrentMap.GetEntity( reader.ReadUInt32() ) as Player;
            Entity target = CurrentMap.GetEntity( reader.ReadUInt32() );

            target.Use( user );
        }

        private void ReceivePlayerViewInventory( BinaryReader reader )
        {
            Player player = CurrentMap.GetEntity( reader.ReadUInt32() ) as Player;
            player.ShowInventory();
        }

        public void SendModifyInventory( Entity owner, params InventorySlot[] modifiedSlots )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.ModifyInventory );
            writer.Write( owner.EntityID );
            writer.Write( modifiedSlots.Length == 0 );

            if ( modifiedSlots == null )
                ( owner as IContainer ).Inventory.Save( writer );
            else
            {
                writer.Write( (UInt16) modifiedSlots.Length );

                foreach ( InventorySlot slot in modifiedSlots )
                {
                    writer.Write( slot.ID );
                    writer.Write( slot.Count );
                    if( slot.HasItem )
                        slot.Item.Save( writer );
                }
            }
            SendPacket();
        }

        private void ReceiveModifyInventory( BinaryReader reader )
        {
            switch( reader.ReadByte() )
            {
                case 0x00:
                    Entity owner = CurrentMap.GetEntity( reader.ReadUInt32() );
                    Entity target = CurrentMap.GetEntity( reader.ReadUInt32() );
                    UInt16 slotID = reader.ReadUInt16();
                    Inventory ownInv = ( owner as IContainer ).Inventory;
                    InventorySlot slot = ownInv[ slotID ];

                    if ( slot.HasItem )
                    {
                        Item item = slot.PopItem();
                        Inventory targInv = ( target as IContainer ).Inventory;

                        if ( targInv.CanAddItem( item ) )
                        {
                            InventorySlot destSlot = targInv.Add( item );

                            SendModifyInventory( owner, slot );
                            SendModifyInventory( target, destSlot );
                        }
                        else
                            slot.PushItem( item );
                    }
                    break;
                case 0x01:
                    Entity entityA = CurrentMap.GetEntity( reader.ReadUInt32() );
                    Entity entityB = CurrentMap.GetEntity( reader.ReadUInt32() );
                    UInt16 slotIDA = reader.ReadUInt16();
                    UInt16 slotIDB = reader.ReadUInt16();
                    Inventory invA = ( entityA as IContainer ).Inventory;
                    InventorySlot slotA = invA[ slotIDA ];
                    Inventory invB = ( entityB as IContainer ).Inventory;
                    InventorySlot slotB = invB[ slotIDB ];

                    bool equippedA = slotA.HasItem && slotA.Item.IsEquipped;
                    bool equippedB = slotB.HasItem && slotB.Item.IsEquipped;

                    slotA.Swap( slotB );

                    if ( invA == invB )
                    {
                        if ( equippedA )
                            slotB.Item.Equip( invA.Owner as Character );
                        if ( equippedB )
                            slotA.Item.Equip( invA.Owner as Character );
                    }

                    SendModifyInventory( entityA, slotA );
                    SendModifyInventory( entityB, slotB );
                    break;
            }
        }

        private void ReceiveUseItem( BinaryReader reader )
        {
            Player player = CurrentMap.GetEntity( reader.ReadUInt32() ) as Player;
            InventorySlot slot = player.Inventory[ reader.ReadUInt16() ];

            if ( slot.HasItem )
            {
                Item item = slot.Item;
                if ( item.IsUseable )
                {
                    List<InventorySlot> changed = new List<InventorySlot>();
                    EventHandler changeDelegate = delegate( object sender, EventArgs e )
                    {
                        changed.Add( sender as InventorySlot );
                    };

                    foreach ( InventorySlot s in player.Inventory.Slots )
                        s.SlotContentsChanged += changeDelegate;

                    player.UseItem( item );

                    foreach ( InventorySlot s in player.Inventory.Slots )
                        s.SlotContentsChanged -= changeDelegate;

                    SendModifyInventory( player, changed.Distinct().ToArray() );
                }
            }
        }

        public void SendEntityAdded( Entity entity, byte[] data )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.EntityAdded );
            writer.Write( data );
            SendPacket();
        }

        public void SendEntityRemoved( Entity entity )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.EntityRemoved );
            writer.Write( entity.EntityID );
            SendPacket();
        }

        public void SendEntityUpdated( Entity entity, byte[] data )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.EntityUpdated );
            writer.Write( entity.EntityID );
            writer.Write( (UInt16) data.Length );
            writer.Write( data );
            SendPacket();
        }

        private void ReceiveCharPointRequest( BinaryReader reader )
        {
            SendCharPointRequest( 10, 5, 50 );
        }

        public void SendCharPointRequest( int baseAttribPoints, int baseSkillPoints, int availibleAttribPoints )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharPointRequest );
            writer.Write( (UInt16) baseAttribPoints );
            writer.Write( (UInt16) baseSkillPoints );
            writer.Write( (UInt16) availibleAttribPoints );
            SendPacket();
        }

        private void ReceiveCharacterCreate( BinaryReader reader )
        {
            Nickname = reader.ReadString();

            PlayerEntity = new Player();

            ushort attribCount = reader.ReadUInt16();
            for ( int i = 0; i < attribCount; ++i )
            {
                CharAttribute attrib = CharAttribute.GetByID( reader.ReadUInt16() );
                int value = reader.ReadByte();

                PlayerEntity.SetBaseAttributeLevel( attrib, value );
            }

            ushort skillCount = reader.ReadUInt16();
            for ( int i = 0; i < skillCount; ++i )
            {
                CharSkill skill = CharSkill.GetByID( reader.ReadUInt16() );
                int value = reader.ReadByte();

                PlayerEntity.SetBaseSkillLevel( skill, value );
            }

            SendCharacterCreate();
        }

        public void SendCharacterCreate()
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.CharacterCreate );
            SendPacket();
        }

        private void ReceiveCharacterResurrect( BinaryReader reader ) {
            PlayerEntity.Resurrect();
        }

        public void SendDisconnect( DisconnectReason reason, String message = "" )
        {
            BinaryWriter writer = GetWriter();
            writer.Write( (byte) PacketID.Disconnect );
            writer.Write( (byte) reason );
            writer.Write( message );
            SendPacket();
        }

        private void ReceiveDisconnect( BinaryReader reader )
        {
            DisconnectReason reason = (DisconnectReason) reader.ReadByte();

            State = ClientState.Disconnected;
            GameServer.RemoveClient( this, reason );
        }
    }
}
