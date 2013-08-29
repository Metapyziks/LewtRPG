using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Lewt.Shared.World
{
    public enum OverworldTileType : byte
    {
        Blank = 0,
        Water = 1,
        Mountain = 2,
        EvergreenTree = 3,
        RoundTree = 4,
        Path = 5,
        Cave = 6,
        Mine = 7,
        Fort = 8,
        Temple = 9,
        House = 10,
        Wall = 11,
        Gate = 12
    }

    public class OverworldTile : Tile
    {
        public const int SubChunkSize = 16; 

        private static int[] stPathIDs = new int[]
        {
            0x0, // 0000
            0x0, // 0001
            0x0, // 0010
            0x7, // 0011
            0x0, // 0100
            0x5, // 0101
            0x8, // 0110
            0xB, // 0111
            0x0, // 1000
            0xA, // 1001
            0x6, // 1010
            0xE, // 1011
            0x9, // 1100
            0xD, // 1101
            0xC, // 1110
            0xF, // 1111
        };

        private static int[] stWallIDs = new int[]
        {
            0x00, // 0000
            0x58, // 0001
            0x59, // 0010
            0x5A, // 0011
            0x58, // 0100
            0x58, // 0101
            0x5B, // 0110
            0x00, // 0111
            0x59, // 1000
            0x5D, // 1001
            0x59, // 1010
            0x00, // 1011
            0x5C, // 1100
            0x00, // 1101
            0x00, // 1110
            0x00, // 1111
        };

        private OverworldMap myMap;
        private OverworldTile[] myNeighbours;

        private Chunk[,] myChunks;

        public OverworldMap Map
        {
            get
            {
                return myMap;
            }
        }

        public bool ChunksLoaded
        {
            get
            {
                return myChunks != null;
            }
        }

        public Chunk[ , ] Chunks
        {
            get
            {
                return myChunks;
            }
        }

        public OverworldTileType TileType;
        public byte Alt;

        public bool IsBlank
        {
            get
            {
                return TileType == OverworldTileType.Blank;
            }
        }
        public bool IsWater
        {
            get
            {
                return TileType == OverworldTileType.Water;
            }
        }
        public bool IsMountain
        {
            get
            {
                return TileType == OverworldTileType.Mountain;
            }
        }
        public bool IsEvergreenTree
        {
            get
            {
                return TileType == OverworldTileType.EvergreenTree;
            }
        }
        public bool IsRoundTree
        {
            get
            {
                return TileType == OverworldTileType.RoundTree;
            }
        }
        public bool IsTree
        {
            get
            {
                return IsEvergreenTree || IsRoundTree;
            }
        }
        public bool IsPath
        {
            get
            {
                return TileType == OverworldTileType.Path;
            }
        }
        public bool IsCave
        {
            get
            {
                return TileType == OverworldTileType.Cave;
            }
        }
        public bool IsMine
        {
            get
            {
                return TileType == OverworldTileType.Mine;
            }
        }
        public bool IsFort
        {
            get
            {
                return TileType == OverworldTileType.Fort;
            }
        }
        public bool IsTemple
        {
            get
            {
                return TileType == OverworldTileType.Temple;
            }
        }
        public bool IsDungeon
        {
            get
            {
                return IsCave || IsMine || IsFort || IsTemple;
            }
        }
        public bool IsHouse
        {
            get
            {
                return TileType == OverworldTileType.House;
            }
        }
        public bool IsWall
        {
            get
            {
                return TileType == OverworldTileType.Wall;
            }
        }
        public bool IsGate
        {
            get
            {
                return TileType == OverworldTileType.Gate;
            }
        }
        public bool IsWallOrGate
        {
            get
            {
                return IsWall || IsGate;
            }
        }
        public bool IsTravelable
        {
            get
            {
                return IsDungeon || IsGate;
            }
        }

        public override bool IsSolid
        {
            get
            {
                switch ( TileType )
                {
                    case OverworldTileType.Blank:
                        return false;
                    case OverworldTileType.Cave:
                        return true;
                    case OverworldTileType.EvergreenTree:
                        return true;
                    case OverworldTileType.Fort:
                        return true;
                    case OverworldTileType.Gate:
                        return true;
                    case OverworldTileType.House:
                        return true;
                    case OverworldTileType.Mine:
                        return true;
                    case OverworldTileType.Mountain:
                        return true;
                    case OverworldTileType.Path:
                        return false;
                    case OverworldTileType.RoundTree:
                        return true;
                    case OverworldTileType.Temple:
                        return true;
                    case OverworldTileType.Wall:
                        return true;
                    default:
                        return true;
                }
            }
        }

        public OverworldTile( int x, int y, OverworldMap map )
            : base( x, y )
        {
            myMap = map;
            TileType = OverworldTileType.Blank;
        }

        public override void PostWorldInitialize()
        {
            myNeighbours = new OverworldTile[]
            {
                Map.GetOverworldTile( X + 1, Y + 0 ) as OverworldTile,
                Map.GetOverworldTile( X + 0, Y + 1 ) as OverworldTile,
                Map.GetOverworldTile( X - 1, Y + 0 ) as OverworldTile,
                Map.GetOverworldTile( X + 0, Y - 1 ) as OverworldTile
            };

            if ( ChunksLoaded )
                foreach ( Chunk chunk in myChunks )
                    chunk.PostWorldInitialize();
        }

        public override float[] GetRawVertexData()
        {
            int x = X / Map.ChunkWidth * 16;
            int y = Y / Map.ChunkHeight * 16;
            int texID = 0;

            int hash;

            switch ( TileType )
            {
                case OverworldTileType.Blank:
                    texID = 0 + Alt % 4 * 16; break;
                case OverworldTileType.Water:
                    texID = 1 + Alt % 4 * 16; break;
                case OverworldTileType.Mountain:
                    texID = 2 + Alt % 4 * 16; break;
                case OverworldTileType.EvergreenTree:
                    texID = 3 + Alt % 4 * 16; break;
                case OverworldTileType.RoundTree:
                    texID = 4 + Alt % 4 * 16; break;
                case OverworldTileType.Path:
                    hash = ( myNeighbours[ 0 ].IsPath ? 1 << 3 : 0 ) |
                           ( myNeighbours[ 1 ].IsPath ? 1 << 2 : 0 ) |
                           ( myNeighbours[ 2 ].IsPath ? 1 << 1 : 0 ) |
                           ( myNeighbours[ 3 ].IsPath ? 1 << 0 : 0 );

                    texID = stPathIDs[ hash ] + Alt % 4 * 16; break;
                case OverworldTileType.Cave:
                    texID = 64 + Alt % 4; break;
                case OverworldTileType.Mine:
                    texID = 68 + Alt % 4; break;
                case OverworldTileType.Fort:
                    texID = 72 + Alt % 4; break;
                case OverworldTileType.Temple:
                    texID = 76 + Alt % 4; break;
                case OverworldTileType.House:
                    texID = 80 + ( Alt / 8 ) % 4 * 16 + Alt % 8; break;
                case OverworldTileType.Wall:
                    hash = ( myNeighbours[ 0 ].IsWallOrGate ? 1 << 3 : 0 ) |
                           ( myNeighbours[ 1 ].IsWallOrGate ? 1 << 2 : 0 ) |
                           ( myNeighbours[ 2 ].IsWallOrGate ? 1 << 1 : 0 ) |
                           ( myNeighbours[ 3 ].IsWallOrGate ? 1 << 0 : 0 );

                    texID = stWallIDs[ hash ] + Alt % 4 * 16; break;
                case OverworldTileType.Gate:
                    if ( myNeighbours[ 0 ].IsWallOrGate || myNeighbours[ 2 ].IsWallOrGate )
                        texID = 94 + Alt % 4 * 16;
                    else
                        texID = 95 + Alt % 4 * 16;
                    break;
                default:
                    texID = 0 + Alt % 4 * 16; break;
            }

            int texX = texID % 16;
            int texY = texID / 16;

            return new float[]
            {
                x, y,
                ( texX + 0 ) / 16.0f, ( texY + 0 ) / 16.0f,

                x + 16.0f, y,
                ( texX + 1 ) / 16.0f, ( texY + 0 ) / 16.0f,

                x + 16.0f, y + 16.0f,
                ( texX + 1 ) / 16.0f, ( texY + 1 ) / 16.0f,

                x, y + 16.0f,
                ( texX + 0 ) / 16.0f, ( texY + 1 ) / 16.0f
            };
        }

        public void LoadChunks()
        {
            if ( ChunksLoaded )
                return;

            int hChunks = Map.ChunkWidth / SubChunkSize;
            int vChunks = Map.ChunkHeight / SubChunkSize;

            myChunks = new Chunk[ hChunks, vChunks ];

            for ( int x = 0; x < hChunks; ++x )
            {
                for ( int y = 0; y < vChunks; ++y )
                {
                    myChunks[ x, y ] = new Chunk( X + x * SubChunkSize, Y + y * SubChunkSize, SubChunkSize, SubChunkSize, Map );
                    Map.GenerateChunk( myChunks[ x, y ] );
                }
            }

            Map.AddChunks( this );
        }

        public void LoadChunks( BinaryReader reader )
        {
            if ( ChunksLoaded )
                UnloadChunks();

            int hChunks = Map.ChunkWidth / SubChunkSize;
            int vChunks = Map.ChunkHeight / SubChunkSize;

            myChunks = new Chunk[ hChunks, vChunks ];

            for ( int x = 0; x < hChunks; ++x )
                for ( int y = 0; y < vChunks; ++y )
                    myChunks[ x, y ] = Chunk.Load( reader.BaseStream, Map );

            Map.AddChunks( this );
        }

        public void UnloadChunks()
        {
            if ( ChunksLoaded )
            {
                foreach ( Chunk chunk in myChunks )
                {
                    Map.RemoveChunk( chunk );
                    chunk.Dispose();
                }

                myChunks = null;
            }
        }

        public Chunk GetChunk( int x, int y )
        {
            if ( !ChunksLoaded )
                return null;
            else
                return myChunks[ ( x - X ) / SubChunkSize, ( y - Y ) / SubChunkSize ];
        }

        public Chunk GetChunk( double x, double y )
        {
            return GetChunk( (int) Math.Floor( x ), (int) Math.Floor( y ) );
        }

        public override void Save( System.IO.BinaryWriter writer )
        {
            writer.Write( (byte) ( (byte) TileType << 4 | Alt ) );
        }

        public override void Load( System.IO.BinaryReader reader )
        {
            byte data = reader.ReadByte();
            TileType = (OverworldTileType) ( data >> 4 );
            Alt = (byte) ( data & 0xF );
        }
    }
}
