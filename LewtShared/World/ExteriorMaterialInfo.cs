using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.World
{
    internal enum TransitionInfo : byte
    {
        None = 0,

        Left        = 1,   // 00000001
        TopLeft     = 2,   // 00000010
        Top         = 4,   // 00000100
        TopRight    = 8,   // 00001000
        Right       = 16,  // 00010000
        BottomRight = 32,  // 00100000
        Bottom      = 64,  // 01000000
        BottomLeft  = 128  // 10000000
    }

    internal class ExteriorMaterialInfo : MaterialInfo
    {
        public static new ExteriorMaterialInfo Default
        {
            get
            {
                return new ExteriorMaterialInfo
                {
                    IsWall = false
                };
            }
        }

        private int myWallHeight;

        public int WallHeight
        {
            get
            {
                if ( !IsWall )
                    return 0;
                else
                    return Math.Max( 1, myWallHeight );
            }
            set
            {
                myWallHeight = value;

                if ( value > 0 )
                    IsWall = true;
                else
                    IsWall = false;
            }
        }

        public override bool IsVisible
        {
            get
            {
                return true;
            }
        }

        protected ExteriorMaterialInfo()
        {

        }

        public ExteriorMaterialInfo( ExteriorTile tile )
            : base( tile )
        {

        }

        private TransitionInfo NeighbourIDToTransitionInfo( int id )
        {
            switch ( id )
            {
                case 0:
                    return TransitionInfo.Right;
                case 1:
                    return TransitionInfo.Bottom;
                case 2:
                    return TransitionInfo.Left;
                case 3:
                    return TransitionInfo.Top;
                case 4:
                    return TransitionInfo.TopLeft;
                case 5:
                    return TransitionInfo.TopRight;
                case 6:
                    return TransitionInfo.BottomLeft;
                case 7:
                    return TransitionInfo.BottomRight;
                default:
                    return TransitionInfo.None;
            }
        }

        private bool CheckIsWallEdge( out int wallID )
        {
            wallID = -1;

            if ( WallHeight == 0 )
                return false;

            bool[] w = new bool[ 8 ];
            bool cont = false;

            for ( int i = 0; i < 8; ++i )
            {
                w[ i ] = ( Neighbours[ i ] as ExteriorMaterialInfo ).WallHeight < WallHeight;
                if ( w[ i ] )
                    cont = true;
            }

            if ( cont )
            {
                if ( !( ( !w[ 0 ] && !w[ 1 ] && !w[ 7 ] ) ||
                    ( !w[ 1 ] && !w[ 2 ] && !w[ 6 ] ) ||
                    ( !w[ 2 ] && !w[ 3 ] && !w[ 4 ] ) ||
                    ( !w[ 3 ] && !w[ 0 ] && !w[ 5 ] ) ) )
                    wallID = 14;

                else if ( ( w[ 2 ] || ( w[ 4 ] && w[ 6 ] ) ) && !w[ 3 ] && !w[ 1 ] && !w[ 0 ] && !w[ 5 ] && !w[ 7 ] )
                    wallID = 0;
                else if ( ( w[ 3 ] || ( w[ 4 ] && w[ 5 ] ) ) && !w[ 0 ] && !w[ 2 ] && !w[ 1 ] && !w[ 6 ] && !w[ 7 ] )
                    wallID = 2;
                else if ( ( w[ 0 ] || ( w[ 5 ] && w[ 7 ] ) ) && !w[ 1 ] && !w[ 3 ] && !w[ 2 ] && !w[ 4 ] && !w[ 6 ] )
                    wallID = 4;
                else if ( ( w[ 1 ] || ( w[ 7 ] && w[ 6 ] ) ) && !w[ 2 ] && !w[ 0 ] && !w[ 3 ] && !w[ 4 ] && !w[ 5 ] )
                    wallID = 6;

                else if ( !w[ 0 ] && !w[ 1 ] && !w[ 7 ] && ( w[ 2 ] || w[ 3 ] || ( w[ 4 ] && w[ 5 ] && w[ 6 ] ) ) )
                    wallID = 1;
                else if ( !w[ 1 ] && !w[ 2 ] && !w[ 6 ] && ( w[ 3 ] || w[ 0 ] || ( w[ 4 ] && w[ 5 ] && w[ 7 ] ) ) )
                    wallID = 3;
                else if ( !w[ 2 ] && !w[ 3 ] && !w[ 4 ] && ( w[ 0 ] || w[ 1 ] || ( w[ 5 ] && w[ 6 ] && w[ 7 ] ) ) )
                    wallID = 5;
                else if ( !w[ 3 ] && !w[ 0 ] && !w[ 5 ] && ( w[ 1 ] || w[ 2 ] || ( w[ 4 ] && w[ 6 ] && w[ 7 ] ) ) )
                    wallID = 7;

                else if ( w[ 4 ] && !w[ 2 ] && !w[ 3 ] && !w[ 7 ] )
                    wallID = 8;
                else if ( w[ 5 ] && !w[ 3 ] && !w[ 0 ] && !w[ 6 ] )
                    wallID = 9;
                else if ( w[ 7 ] && !w[ 0 ] && !w[ 1 ] && !w[ 4 ] )
                    wallID = 10;
                else if ( w[ 6 ] && !w[ 1 ] && !w[ 2 ] && !w[ 5 ] )
                    wallID = 11;

                else if ( w[ 4 ] && w[ 7 ] )
                    wallID = 12;
                else if( w[ 5 ] && w[ 6 ] )
                    wallID = 13;

                return true;
            }
            else
                return false;
        }

        public override float[] GetRawVertexData()
        {
            if ( Tile.IsDefault )
                return new float[ 0 ];

            int x = Tile.X * 16;
            int y = Tile.Y * 16;

            Dictionary<byte, TransitionInfo> layers = new Dictionary<byte,TransitionInfo>();

            bool wallCover = false;

            if ( IsWall )
            {
                int wallID;
                if ( !CheckIsWallEdge( out wallID ) || WallHeight > 1 )
                {
                    layers.Add( (byte) ( 15 + 32 ), TransitionInfo.None );
                    wallCover = true;
                }
                else
                    layers.Add( (byte) ( wallID + 32 ), TransitionInfo.None );
            }

            if ( !wallCover )
            {
                layers.Add( Skin, TransitionInfo.None );

                for ( int i = 0; i < 8; ++i )
                    if ( Neighbours[ i ].Skin > Skin )
                    {
                        if ( layers.ContainsKey( Neighbours[ i ].Skin ) )
                            layers[ Neighbours[ i ].Skin ] |= NeighbourIDToTransitionInfo( i );
                        else
                            layers.Add( Neighbours[ i ].Skin, NeighbourIDToTransitionInfo( i ) );
                    }
            }

            float[] verts = new float[ layers.Keys.Count * 7 * 4 ];

            int t = 0;

            foreach ( KeyValuePair<byte, TransitionInfo> keyVal in layers.OrderBy( kv => kv.Key ) )
            {
                int texX = keyVal.Key % 8;
                int texY = keyVal.Key / 8;

                int transX = 1;
                int transY = 0;

                for ( int i = 0; i < 4; ++i )
                {
                    int xi = ( i == 0 || i == 3 ? 0 : 1 );
                    int yi = ( i < 2 ? 0 : 1 );

                    verts[ t++ ] = x + xi * 16.0f;
                    verts[ t++ ] = y + yi * 16.0f;

                    verts[ t++ ] = ( texX + xi ) / 8.0f;
                    verts[ t++ ] = ( texY + yi ) / 8.0f;

                    verts[ t++ ] = (float) keyVal.Value;

                    if ( keyVal.Value != TransitionInfo.None )
                    {
                        verts[ t++ ] = ( transX + xi ) / 2.0f;
                        verts[ t++ ] = ( transY + yi ) / 2.0f;
                    }
                    else
                        t += 2;
                }
            }

            return verts;
        }

        public override void Save( System.IO.BinaryWriter writer )
        {
            base.Save( writer );

            if ( IsWall )
                writer.Write( (byte) WallHeight );
        }

        public override void Load( System.IO.BinaryReader reader )
        {
            base.Load( reader );

            if ( IsWall )
                myWallHeight = reader.ReadByte();
        }
    }
}
