using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibNoise;
using Lewt.Shared.Rendering;
using OpenTK;

namespace Lewt.Shared.World
{
    public class OverworldMap : Map
    {
        private OverworldTile[,] myTiles;
        private bool myTilesChanged;
        private VertexBuffer myVB;

        private Perlin myHeightPerlin;
        private Perlin myTempPerlin;

        private double myStartX;
        private double myStartY;
        private double myStartZ;

        private List<Dungeon> myDungeons;

        public readonly int HorizontalChunks;
        public readonly int VerticalChunks;

        public readonly int ChunkWidth;
        public readonly int ChunkHeight;

        public readonly int Width;
        public readonly int Height;
        
        internal VertexBuffer VertexBuffer
        {
            get
            {
                return myVB;
            }
        }

        public Dungeon[] Dungeons
        {
            get
            {
                return myDungeons.ToArray();
            }
        }

        public OverworldMap( int horizontalChunks, int verticalChunks, int chunkWidth = 32, int chunkHeight = 32 )
            : base( false, 0xFFFF, true )
        {
            HorizontalChunks = horizontalChunks;
            VerticalChunks = verticalChunks;

            ChunkWidth = chunkWidth;
            ChunkHeight = chunkHeight;

            Width = HorizontalChunks * ChunkWidth;
            Height = VerticalChunks * ChunkHeight;

            myTiles = new OverworldTile[ HorizontalChunks, VerticalChunks ];
        }

        public OverworldMap( System.IO.BinaryReader reader, bool isServer )
            : base( false, 0xFFFF, isServer )
        {
            HorizontalChunks = reader.ReadInt16();
            VerticalChunks = reader.ReadInt16();

            ChunkWidth = reader.ReadInt16();
            ChunkHeight = reader.ReadInt16();

            Width = HorizontalChunks * ChunkWidth;
            Height = VerticalChunks * ChunkHeight;

            myTiles = new OverworldTile[ HorizontalChunks, VerticalChunks ];

            for ( int x = 0; x < HorizontalChunks; ++x )
                for ( int y = 0; y < VerticalChunks; ++y )
                {
                    OverworldTile tile = myTiles[ x, y ] = new OverworldTile( x * ChunkWidth, y * ChunkHeight, this );
                    tile.Load( reader );
                }

            myDungeons = new List<Dungeon>();

            UInt16 dungeonCount = reader.ReadUInt16();

            for ( int i = 0; i < dungeonCount; ++i )
            {
                UInt16 id = reader.ReadUInt16();
                String dungClassName = reader.ReadString();
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();

                myDungeons.Add( new Dungeon( id, DungeonClass.Get( dungClassName ), x, y, false ) );
            }

            myVB = new VertexBuffer();
            myTilesChanged = true;
        }

        internal double GetHeight( double x, double y )
        {
            double height = myHeightPerlin.GetValue( myStartX + x / ChunkWidth, myStartY + y / ChunkHeight, myStartZ );
            height += Tools.Max( x / (double) Width, y / (double) Height,
                ( Width - x - 1 ) / (double) Width, ( Height - y - 1 ) / (double) Height ) * 4.0 - 3.5;

            return height;
        }

        internal double GetTemperature( double x, double y )
        {
            return myTempPerlin.GetValue( myStartX + x / ChunkWidth, myStartY + y / ChunkHeight, myStartZ );
        }

        public virtual void Generate( uint seed = 0 )
        {
            if ( seed == 0 )
                seed = (uint) ( Tools.Random() * 0xFFFFFFFF );

            Random rand = new Random( (int) seed );

            myHeightPerlin = new Perlin
            {
                Seed = rand.Next( int.MinValue, int.MaxValue ),
                OctaveCount = 12,
                Frequency = 0.25,
                Lacunarity = 2.0,
                Persistence = 0.5
            };

            myTempPerlin = new Perlin
            {
                Seed = rand.Next( int.MinValue, int.MaxValue ),
                OctaveCount = 8,
                Frequency = 0.25,
                Lacunarity = 2.0,
                Persistence = 0.5
            };

            myStartX = rand.NextDouble() * 256.0 - 128.0;
            myStartY = rand.NextDouble() * 256.0 - 128.0;
            myStartZ = rand.NextDouble() * 256.0 - 128.0;

            for( int x = 0; x < HorizontalChunks; ++x )
                for ( int y = 0; y < VerticalChunks; ++y )
                {
                    OverworldTile tile = myTiles[ x, y ] = new OverworldTile( x * ChunkWidth, y * ChunkHeight, this );

                    double height = GetHeight( ( x + 0.5 ) * ChunkWidth, ( y + 0.5 ) * ChunkHeight );

                    if ( height < 0.0 )
                    {
                        double temp = GetTemperature( ( x + 0.5 ) * ChunkWidth, ( y + 0.5 ) * ChunkHeight );

                        if ( temp < -0.25 )
                        {
                            tile.TileType = OverworldTileType.Water;
                            tile.Alt = (byte) ( rand.Next( 4 ) );
                        }
                        else if ( temp < 0.5 )
                        {
                            tile.TileType = OverworldTileType.Blank;
                            tile.Alt = (byte) ( rand.Next( 4 ) );
                        }
                        else
                        {
                            if( height > -0.5 )
                                tile.TileType = OverworldTileType.EvergreenTree;
                            else
                                tile.TileType = OverworldTileType.RoundTree;
                            tile.Alt = (byte) ( rand.Next( 4 ) );
                        }
                    }
                    else
                    {
                        tile.TileType = OverworldTileType.Mountain;
                        tile.Alt = (byte) ( rand.Next( 2 ) + ( height <= 0.5 ? 2 : 0 ) );
                    }
                }

            PostWorldInitialize();

            myDungeons = new List<Dungeon>();
            UInt16 dungeonID = 0;

            DungeonClass[] classes = DungeonClass.GetAll();

            while ( myDungeons.Count < 24 )
            {
                int x = rand.Next( ChunkWidth * 3, Width - ChunkWidth * 3 );
                int y = rand.Next( ChunkHeight * 3, Height - ChunkHeight * 3 );

                bool canPlace = true;

                foreach ( Dungeon dungeon in myDungeons )
                    if ( System.Math.Abs( dungeon.X - x ) <= 3 * ChunkWidth && System.Math.Abs( dungeon.Y - y ) <= 3 * ChunkHeight )
                    {
                        canPlace = false;
                        break;
                    }

                if ( canPlace )
                {
                    myTiles[ x / ChunkWidth, y / ChunkHeight ].TileType = OverworldTileType.Blank;
                    myDungeons.Add( new Dungeon( dungeonID++, classes[ rand.Next( classes.Length ) ], x, y, true ) );
                }
            }
        }

        public void GenerateChunk( Chunk chunk )
        {
            for( int x = chunk.X; x < chunk.X + chunk.Width; ++ x )
                for ( int y = chunk.Y; y < chunk.Y + chunk.Height; ++y )
                {
                    double height = GetHeight( x, y );
                    double temp = GetTemperature( x, y );

                    ExteriorTile tile = chunk.GetTile( x, y ) as ExteriorTile;
                    tile.IsWall = false;

                    if ( height >= 0.25 )
                    {
                        if ( height >= 0.5 )
                            tile.Skin = 3;
                        else
                            tile.Skin = 1;

                        tile.WallHeight = 1;
                    }
                    else if ( height >= -1.0 && temp < 0.25 )
                        tile.Skin = 1;
                    else if ( height < -0.5 && temp < 0.25 )
                        tile.Skin = 2;
                    else
                        tile.Skin = 0;
                }

            chunk.UpdateTiles();
        }

        internal void AddChunks( OverworldTile tile )
        {
            if( !Chunks.Contains( tile.Chunks[ 0, 0 ] ) )
                foreach ( Chunk chunk in tile.Chunks )
                    AddChunk( chunk );

            foreach ( Chunk chunk in tile.Chunks )
                chunk.PostWorldInitialize();

            int cs = OverworldTile.SubChunkSize;

            int hChunks = ChunkWidth / OverworldTile.SubChunkSize;
            int vChunks = ChunkHeight / OverworldTile.SubChunkSize;

            for ( int x = -1; x < hChunks + 1; ++ x )
            {
                int xt = tile.X + x * cs;

                if ( xt >= 0 && xt < Width )
                {
                    int yt1 = tile.Y - 1 * cs;
                    int yt2 = tile.Y + vChunks * cs;

                    if ( yt1 >= 0 && yt1 < Height )
                    {
                        OverworldTile t = myTiles[ xt / ChunkWidth, yt1 / ChunkHeight ];
                        if( t.ChunksLoaded )
                            t.Chunks[ ( x + hChunks ) % hChunks, vChunks - 1 ].PostWorldInitialize();
                    }

                    if ( yt2 >= 0 && yt2 < Height )
                    {
                        OverworldTile t = myTiles[ xt / ChunkWidth, yt2 / ChunkHeight ];
                        if ( t.ChunksLoaded )
                            t.Chunks[ ( x + hChunks ) % hChunks, 0 ].PostWorldInitialize();
                    }
                }
            }

            for ( int y = -1; y < vChunks + 1; ++y )
            {
                int yt = tile.Y + y * cs;

                if ( yt >= 0 && yt < Height )
                {
                    int xt1 = tile.X - 1 * cs;
                    int xt2 = tile.X + hChunks * cs;

                    if ( xt1 >= 0 && xt1 < Width )
                    {
                        OverworldTile t = myTiles[ xt1 / ChunkWidth, yt / ChunkHeight ];
                        if ( t.ChunksLoaded )
                            t.Chunks[ hChunks - 1, ( y + vChunks ) % vChunks ].PostWorldInitialize();
                    }

                    if ( xt2 >= 0 && xt2 < Width )
                    {
                        OverworldTile t =  myTiles[ xt2 / ChunkWidth, yt / ChunkHeight ];
                        if ( t.ChunksLoaded )
                            t.Chunks[ 0, ( y + vChunks ) % vChunks ].PostWorldInitialize();
                    }
                }
            }

            ForceLightUpdate();
        }

        public override void PostWorldInitialize()
        {
            for ( int x = 0; x < HorizontalChunks; ++x )
                for ( int y = 0; y < VerticalChunks; ++y )
                    myTiles[ x, y ].PostWorldInitialize();

            UpdateTiles();

            base.PostWorldInitialize();
        }

        public void UpdateTiles()
        {
            myTilesChanged = true;
        }

        public Map GetMap( UInt16 id )
        {
            return myDungeons.First( x => x.ID == id );
        }

        public OverworldTile GetOverworldTile( int x, int y )
        {
            if ( x >= 0 && y >= 0 && x < Width && y < Height )
                return myTiles[ x / ChunkWidth, y / ChunkHeight ];
            else
                return new OverworldTile( ( x / ChunkWidth ) * ChunkWidth, ( y / ChunkHeight ) * ChunkHeight, this );
        }

        public OverworldTile GetOverworldTile( double x, double y )
        {
            return GetOverworldTile( (int) System.Math.Floor( x ), (int) System.Math.Floor( y ) );
        }

        public override Chunk GetChunk( int x, int y )
        {
            if ( x >= 0 && x < Width && y >= 0 && y < Height )
                return GetOverworldTile( x, y ).GetChunk( x, y );
            
            return null;
        }

        protected virtual float[] GetRawTileData()
        {
            List<float[]> verts = new List<float[]>();

            int count = 0;

            for ( int x = 0; x < HorizontalChunks; ++x )
                for ( int y = 0; y < VerticalChunks; ++y )
                {
                    float[] data = myTiles[ x, y ].GetRawVertexData();
                    count += data.Length;
                    verts.Add( data );
                }

            float[] vertArr = new float[ count ];

            for ( int i = 0, s = 0; s < count; s += verts[ i++ ].Length )
                Array.Copy( verts[ i ], 0, vertArr, s, verts[ i ].Length );

            return vertArr;
        }

        protected virtual void SendTileData()
        {
            myVB.SetTileData( GetRawTileData() );
            myTilesChanged = false;
        }

        public void RenderTiles()
        {
            if ( myTilesChanged )
                SendTileData();

            MapRenderer.DrawTiles( this );
        }

        public void Save( System.IO.BinaryWriter writer )
        {
            writer.Write( (Int16) HorizontalChunks );
            writer.Write( (Int16) VerticalChunks );

            writer.Write( (Int16) ChunkWidth );
            writer.Write( (Int16) ChunkHeight );

            for ( int x = 0; x < HorizontalChunks; ++x )
                for ( int y = 0; y < VerticalChunks; ++y )
                    myTiles[ x, y ].Save( writer );

            writer.Write( (UInt16) myDungeons.Count );

            foreach ( Dungeon dungeon in myDungeons )
            {
                writer.Write( dungeon.ID );
                writer.Write( dungeon.DungeonClass.Name );
                writer.Write( dungeon.X );
                writer.Write( dungeon.Y );
            }
        }
    }
}
