using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.World
{
    internal enum EdgeInfo : byte
    {
        None = 0,

        Right = 1,                          // 1
        Bottom = 2,                         // 2
        BottomRight = Right | Bottom,       // 3
        Left = 4,                           // 4
        Vertical = Right | Left,            // 5
        BottomLeft = Bottom | Left,         // 6
        BottomEnd = Right | Bottom | Left,  // 7
        Top = 8,                            // 8
        TopRight = Right | Top,             // 9
        Horizontal = Bottom | Top,          // 10
        RightEnd = Right | Bottom | Top,    // 11
        TopLeft = Left | Top,               // 12
        TopEnd = Right | Left | Top,        // 13
        LeftEnd = Bottom | Left | Top,      // 14
        All = Right | Bottom | Left | Top,  // 15
    }

    internal class InteriorMaterialInfo : MaterialInfo
    {
        public static new InteriorMaterialInfo Default
        {
            get
            {
                return new InteriorMaterialInfo
                {
                    IsWall = true
                };
            }
        }

        protected InteriorMaterialInfo()
        {

        }

        public InteriorMaterialInfo( InteriorTile tile )
            : base( tile )
        {

        }

        private int FindTextureID( EdgeInfo edge = 0 )
        {
            if ( !IsWall )
                return 1 + Alt % 4;

            bool bLeft = !Neighbours[ 6 ].IsWall;
            bool bRight = !Neighbours[ 7 ].IsWall;

            if ( edge == 0 )
            {
                if ( bLeft || bRight )
                    return 20 + ( bLeft && !bRight ? 4 : 0 ) + ( bLeft && bRight ? 8 : 0 );

                return 0;
            }

            if ( ( edge & EdgeInfo.Bottom ) == 0 && ( bLeft || bRight ) )
            {
                bool top = ( edge & EdgeInfo.Top ) != 0;
                bool left = ( edge & EdgeInfo.Left ) != 0;
                bool right = ( edge & EdgeInfo.Right ) != 0;

                if ( !left && !right && bLeft && bRight )
                    return 28 + ( top ? 1 : 0 );

                if ( ( !left && bLeft ) || ( !right && bRight ) )
                    return 20 + ( ( bLeft && !left ) ? 4 : 0 ) + ( left || right ? 1 : 0 ) + ( top ? 2 : 0 );
            }

            return 4 + (int) edge;
        }

        private EdgeInfo FindEdgeInfo()
        {
            return
                ( !Neighbours[ 0 ].IsWall || ( Neighbours[ 0 ].Skin != Skin && !Neighbours[ 0 ].IsDefault ) ? EdgeInfo.Right : 0 ) |
                ( !Neighbours[ 1 ].IsWall || ( Neighbours[ 1 ].Skin != Skin && !Neighbours[ 1 ].IsDefault ) ? EdgeInfo.Bottom : 0 ) |
                ( !Neighbours[ 2 ].IsWall || ( Neighbours[ 2 ].Skin != Skin && !Neighbours[ 2 ].IsDefault ) ? EdgeInfo.Left : 0 ) |
                ( !Neighbours[ 3 ].IsWall || ( Neighbours[ 3 ].Skin != Skin && !Neighbours[ 3 ].IsDefault ) ? EdgeInfo.Top : 0 );
        }

        public override float[] GetRawVertexData()
        {
            int x = Tile.X * 16;
            int y = Tile.Y * 16;
            EdgeInfo edge = FindEdgeInfo();
            bool isWallEdge = edge > 0;
            int texID = FindTextureID( edge );
            int texX = texID % 16;
            int texY = texID / 16;

            if ( Tile.IsDefault )
                return new float[ 0 ];

            int skinOffset = Skin * 2;

            return new float[]
            {
                x, y,
                ( texX + 0 ) / 16.0f, ( texY + skinOffset + 0 ) / 16.0f,

                x + 16.0f, y,
                ( texX + 1 ) / 16.0f, ( texY + skinOffset + 0 ) / 16.0f,

                x + 16.0f, y + 16.0f,
                ( texX + 1 ) / 16.0f, ( texY + skinOffset + 1 ) / 16.0f,

                x, y + 16.0f,
                ( texX + 0 ) / 16.0f, ( texY + skinOffset + 1 ) / 16.0f
            };
        }
    }
}
