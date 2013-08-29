using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Lewt.Shared.World
{
    internal class MaterialInfo
    {
        public static MaterialInfo Default
        {
            get
            {
                return new MaterialInfo
                {
                    IsWall = true
                };
            }
        }

        private readonly GameTile myTile;
        
        private MaterialInfo[] myNeighbours;

        protected MaterialInfo[] Neighbours
        {
            get
            {
                Monitor.Enter( this );
                if ( myNeighbours == null )
                {
                    int x = Tile.X;
                    int y = Tile.Y;

                    myNeighbours = new MaterialInfo[ 8 ];

                    Map map = Tile.Map;
                    myNeighbours[ 0 ] = map.GetTile( x + 1, y + 0 ).Info;
                    myNeighbours[ 1 ] = map.GetTile( x + 0, y + 1 ).Info;
                    myNeighbours[ 2 ] = map.GetTile( x - 1, y + 0 ).Info;
                    myNeighbours[ 3 ] = map.GetTile( x + 0, y - 1 ).Info;
                    myNeighbours[ 4 ] = map.GetTile( x - 1, y - 1 ).Info;
                    myNeighbours[ 5 ] = map.GetTile( x + 1, y - 1 ).Info;
                    myNeighbours[ 6 ] = map.GetTile( x - 1, y + 1 ).Info;
                    myNeighbours[ 7 ] = map.GetTile( x + 1, y + 1 ).Info;
                }

                MaterialInfo[] toReturn = myNeighbours;
                Monitor.Exit( this );

                return toReturn;
            }
        }

        public readonly bool IsDefault;

        public GameTile Tile
        {
            get
            {
                return myTile;
            }
        }

        public bool IsWall;
        public byte Alt;
        public byte Skin;

        public virtual bool IsVisible
        {
            get
            {
                if ( !IsWall )
                    return true;

                foreach ( MaterialInfo neighbour in Neighbours )
                    if ( !neighbour.IsWall )
                        return true;

                return false;
            }
        }

        protected MaterialInfo()
        {
            IsDefault = true;

            Skin = 0;
            IsWall = false;
            Alt = 0;
        }

        public MaterialInfo( GameTile tile )
            : this()
        {
            IsDefault = false;

            myTile = tile;
        }

        public void LearnNeighbours()
        {
            Monitor.Enter( this );
            myNeighbours = null;
            Monitor.Exit( this );
        }

        public virtual float[] GetRawVertexData()
        {
            return new float[ 0 ];
        }

        public virtual void Save( BinaryWriter writer )
        {
            writer.Write( (byte) ( ( ( IsWall ? 1 : 0 ) & 0x1 ) << 7 | ( Alt & 0x3 ) << 5 | ( Skin & 0x1F ) ) );
        }

        public virtual void Load( BinaryReader reader )
        {
            byte data = reader.ReadByte();
            IsWall = ( data >> 7 & 0x1 ) == 1;
            Alt = (byte)( data >> 5 & 0x3 );
            Skin = (byte)( data & 0x1F );
        }
    }
}
