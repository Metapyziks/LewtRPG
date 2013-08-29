using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Lewt.Shared.World
{
    public class GameTile : Tile
    {
        public static GameTile Default
        {
            get
            {
                return new GameTile();
            }
        }
        
        private Map myMap;
        private Chunk myChunk;

        private bool myUseHalfLighting;
        private bool myIsVisible;

        private readonly bool myIsDefault;

        internal MaterialInfo Info;

        public Chunk Chunk
        {
            get
            {
                return myChunk;
            }
        }

        public Map Map
        {
            get
            {
                return myMap;
            }
        }

        public bool IsDefault
        {
            get
            {
                return myIsDefault;
            }
        }

        public virtual bool IsWall
        {
            get
            {
                return Info.IsWall;
            }
            set
            {
                if ( !IsDefault && value != Info.IsWall )
                {
                    Info.IsWall = value;
                    Chunk.UpdateTiles();
                    Chunk.UpdateLighting();
                }
            }
        }

        public bool IsFloor
        {
            get
            {
                return !IsWall;
            }
            set
            {
                IsWall = !value;
            }
        }

        public virtual bool UseHalfLighting
        {
            get
            {
                return ( myUseHalfLighting || Map.GetTile( X - 1, Y ).myUseHalfLighting || Map.GetTile( X + 1, Y ).myUseHalfLighting );
            }
            protected set
            {
                myUseHalfLighting = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return myIsVisible;
            }
            protected set
            {
                myIsVisible = value;
            }
        }

        public byte Skin
        {
            get
            {
                return Info.Skin;
            }
            set
            {
                if ( !IsDefault && value != Info.Skin )
                {
                    Info.Skin = value;
                    Chunk.UpdateTiles();
                }
            }
        }

        public byte Alt
        {
            get
            {
                return Info.Alt;
            }
            set
            {
                if ( !IsDefault && value != Info.Alt )
                {
                    Info.Alt = value;
                    Chunk.UpdateTiles();
                }
            }
        }

        public override bool IsSolid
        {
            get
            {
                return IsWall;
            }
        }

        public GameTile()
        {
            myIsDefault = true;
        }

        public GameTile( int x, int y, Chunk chunk, Map map )
            : base( x, y )
        {
            myIsDefault = false;

            myChunk = chunk;
            myMap = map;
            myUseHalfLighting = false;
            myIsVisible = true;
        }

        public override void PostWorldInitialize()
        {
            Info.LearnNeighbours();
        }   

        public override void Save( BinaryWriter writer )
        {
            Info.Save( writer );
        }

        public override void Load( BinaryReader reader )
        {
            Info.Load( reader );
        }
    }
}
