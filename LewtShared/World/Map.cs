using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;

namespace Lewt.Shared.World
{
    public delegate void EntityAddedHandler( Entity entity );
    public delegate void EntityRemovedHandler( Entity entity );
    public delegate void EntityUpdatedHandler( Entity entity, byte[] data );
    public delegate void EntityChangeChunkHandler( Entity entity, Chunk oldChunk, Chunk newChunk );

    public class Map
    {
        private bool myAlwaysPlaceEntities;
        private SortedList<uint,Entity> myEntities;
        private DateTime myStartTime;
        private ulong myStartTicks;
        private bool myIsInterior;
        private bool myIsServer;

        private UInt32 myLastEntID;

        protected List<Chunk> Chunks;

        public readonly UInt16 ID;

        public EntityAddedHandler EntityAdded;
        public EntityRemovedHandler EntityRemoved;
        public EntityUpdatedHandler EntityUpdated;
        public EntityChangeChunkHandler EntityChangeChunk;

        public bool AlwaysPlaceEntities
        {
            get
            {
                return myAlwaysPlaceEntities;
            }
            set
            {
                myAlwaysPlaceEntities = value;
            }
        }

        public Chunk[] TileChunks
        {
            get
            {
                return Chunks.ToArray();
            }
        }

        public Entity[] Entities
        {
            get
            {
                return myEntities.Values.ToArray();
            }
        }

        public ulong TimeTicks
        {
            get
            {
                return myStartTicks + Tools.SecondsToTicks( ( DateTime.Now - myStartTime ).TotalSeconds );
            }
        }

        public double TimeSeconds
        {
            get
            {
                return TimeTicks / 64.0;
            }
        }

        public bool IsInterior
        {
            get
            {
                return myIsInterior;
            }
        }

        public bool IsExterior
        {
            get
            {
                return !IsInterior;
            }
        }

        public bool IsServer
        {
            get
            {
                return myIsServer;
            }
        }

        public bool IsClient
        {
            get
            {
                return !myIsServer;
            }
        }

        private List<Chunk> myChunksToUpdateLighting;

        public Map( bool isInterior, UInt16 id = 0, bool isServer = false )
        {
            myIsInterior = isInterior;
            ID = id;
            myIsServer = isServer;

            myStartTicks = 0;
            myStartTime = DateTime.Now;
            myAlwaysPlaceEntities = false;

            Chunks = new List<Chunk>();
            myEntities = new SortedList<uint,Entity>();

            myLastEntID = isServer ? 0x00000000 : 0x80000000;

            myChunksToUpdateLighting = new List<Chunk>();
        }

        public void Clear()
        {
            Chunks = new List<Chunk>();
            myEntities = new SortedList<uint, Entity>();

            myLastEntID = IsServer ? 0x00000000 : 0x80000000;

            myChunksToUpdateLighting = new List<Chunk>();
        }

        public void SetStartTime( DateTime startTime, ulong startTicks = 0 )
        {
            myStartTime = startTime;
            myStartTicks = startTicks;
        }

        public virtual void PostWorldInitialize()
        {
            foreach ( Chunk chunk in Chunks )
                chunk.PostWorldInitialize();
        }

        public virtual void UpdateLight( Light light, bool removed = false )
        {
            Chunk[] chunks = Chunks.ToArray();

            if ( removed )
            {
                foreach ( Chunk chunk in chunks )
                    if ( chunk != null && chunk.LightSources.Contains( light ) )
                    {
                        try
                        {
                            chunk.LightSources.Remove( light );
                        }
                        catch
                        {

                        }

                        if ( !myChunksToUpdateLighting.Contains( chunk ) )
                            myChunksToUpdateLighting.Add( chunk );
                    }
            }
            else
            {
                foreach ( Chunk chunk in chunks )
                {
                    if ( chunk != null )
                    {
                        if ( chunk.X <= light.MaximumX &&
                            chunk.Y <= light.MaximumY &&
                            chunk.X + chunk.Width >= light.MinimumX &&
                            chunk.Y + chunk.Height >= light.MinimumY )
                        {
                            if ( !chunk.LightSources.Contains( light ) )
                                chunk.LightSources.Add( light );

                            if ( !myChunksToUpdateLighting.Contains( chunk ) )
                                myChunksToUpdateLighting.Add( chunk );
                        }
                        else if ( chunk.LightSources.Contains( light ) )
                            chunk.LightSources.Remove( light );
                    }
                }
            }
        }

        public virtual void AddEntity( Entity ent )
        {
            bool first = ent.Map != this;

            if ( ent.EntityID == 0xFFFFFFFF || ent.IsRemoved || ent.Chunk != null || ( ent.Map != this && ent.Map != null ) )
                ent.SetID( myLastEntID++ );

            if ( ent.Map != null && ent.Map != this )
                ent.Map.RemoveEntity( ent );

            if ( ent.Chunk != null )
                ent.Chunk.RemoveEntity( ent );

            if ( !myEntities.Keys.Contains( ent.EntityID ) )
                myEntities.Add( ent.EntityID, ent );
            else
                return;

            ent.Map = this;

            ent.Chunk = GetChunk( ent.OriginX, ent.OriginY );
            if( ent.Chunk != null )
                ent.Chunk.AddEntity( ent );

            ent.EnterMap( this );

            if ( IsServer && first && ent.SendToClients && EntityAdded != null )
                EntityAdded( ent );
        }

        public virtual void RelocateEntity( Entity ent )
        {
            Chunk oldChunk = ent.Chunk;
            Chunk newChunk = GetChunk( ent.OriginX, ent.OriginY );

            if ( oldChunk != newChunk )
            {
                if ( oldChunk != null )
                    oldChunk.RemoveEntity( ent );

                ent.Chunk = newChunk;

                if ( newChunk != null )
                    newChunk.AddEntity( ent );
                else
                    RemoveEntity( ent );

                if ( ent is Light )
                {
                    if ( oldChunk != null )
                        oldChunk.UpdateLighting();
                    if ( newChunk != null )
                        newChunk.UpdateLighting();
                }

                if ( oldChunk != null && newChunk != null && EntityChangeChunk != null )
                    EntityChangeChunk( ent, oldChunk, newChunk );
            }
        }

        public virtual void RemoveEntity( UInt32 entID )
        {
            if( myEntities.ContainsKey( entID ) )
                RemoveEntity( myEntities[ entID ] );
        }

        public virtual void RemoveEntity( Entity ent )
        {
            if ( !ent.IsRemoved )
                ent.Remove();

            if ( IsServer && ent.SendToClients && EntityRemoved != null )
                EntityRemoved( ent );

            myEntities.Remove( ent.EntityID );

            if ( ent.Chunk != null )
                ent.Chunk.RemoveEntity( ent );
        }

        public virtual Entity GetEntity( uint entityID )
        {
            if ( myEntities.ContainsKey( entityID ) )
                return myEntities[ entityID ];
            else
                return null;
        }

        internal void AddChunk( Chunk chunk )
        {
            Chunks.Add( chunk );
        }

        internal void RemoveChunk( Chunk chunk )
        {
            Chunks.Remove( chunk );
        }

        public virtual Chunk GetChunk( int x, int y )
        {
            foreach ( Chunk chunk in Chunks )
                if ( x >= chunk.X && y >= chunk.Y && x < chunk.X + chunk.Width && y < chunk.Y + chunk.Height )
                    return chunk;

            return null;
        }

        public Chunk GetChunk( double x, double y )
        {
            return GetChunk( (int) Math.Floor( x ), (int) Math.Floor( y ) );
        }

        public GameTile GetTile( int x, int y )
        {
            Chunk chunk = GetChunk( x, y );
            if ( chunk == null )
            {
                if ( IsInterior )
                    return InteriorTile.Default;
                else
                    return ExteriorTile.Default;
            }

            return chunk.GetTile( x, y ) ?? ( IsInterior ? (GameTile) InteriorTile.Default : (GameTile) ExteriorTile.Default );
        }

        protected void CalculateLighting()
        {
            for ( int i = 0; i < myChunksToUpdateLighting.Count; ++ i )
            {
                Chunk chunk = myChunksToUpdateLighting[ i ];
                chunk.CalculateLighting();
            }

            myChunksToUpdateLighting = new List<Chunk>();
        }

        public void ForceLightUpdate()
        {
            if ( myChunksToUpdateLighting.Count == Chunks.Count )
                return;

            myChunksToUpdateLighting = new List<Chunk>();

            foreach ( Chunk chunk in Chunks )
                myChunksToUpdateLighting.Add( chunk );
        }

        public void Think( double deltaTime )
        {
            Chunk[] chunks = Chunks.ToArray();

            foreach ( Chunk chunk in chunks )
                chunk.Think( deltaTime );

            foreach ( Chunk chunk in chunks )
                chunk.CheckEntityLocations();
        }

        public void Render( bool lighting = true, bool editor = false )
        {
            if ( myChunksToUpdateLighting.Count > 0 )
                CalculateLighting();

            List<Chunk> visibleChunks = new List<Chunk>();
            List<Entity> visibleEnts = new List<Entity>();

            Chunk[] chunks = Chunks.ToArray();

            foreach ( Chunk chunk in chunks )
            {
                if( chunk.X >= MapRenderer.ViewportRight ||
                    chunk.Y >= MapRenderer.ViewportBottom ||
                    chunk.X + chunk.Width <= MapRenderer.ViewportLeft ||
                    chunk.Y + chunk.Height <= MapRenderer.ViewportTop )
                    continue;

                visibleChunks.Add( chunk );

                chunk.RenderTiles();

                if ( lighting && editor )
                    chunk.RenderLighting();

                if( !editor )
                    visibleEnts.AddRange( chunk.Entities );
            }

            if( !editor )
                visibleEnts = visibleEnts.OrderByDescending( x => x.OriginY ).ToList();

            SpriteRenderer.Begin();
            if ( !editor )
                for( int i = visibleEnts.Count - 1; i >= 0; -- i )
                    visibleEnts[ i ].Render();
            else
                foreach ( Chunk chunk in visibleChunks )
                    chunk.RenderEntities( true );
            
            SpriteRenderer.End();

            if ( lighting && !editor )
                foreach ( Chunk chunk in visibleChunks )
                    chunk.RenderLighting();
        }

        public virtual void Dispose()
        {
            foreach ( Chunk chunk in Chunks )
                chunk.Dispose();
        }
    }
}
