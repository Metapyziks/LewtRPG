using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Lewt.Shared.Rendering;

namespace Lewt.Shared.World
{
    public class InteriorTile : GameTile
    {
        private static readonly InteriorTile stDefault = new InteriorTile();

        public static new InteriorTile Default
        {
            get
            {
                return stDefault;
            }
        }

        public override bool UseHalfLighting
        {
            get
            {
                return IsWall && base.UseHalfLighting;
            }
        }
        
        private InteriorTile()
        {
            Info = InteriorMaterialInfo.Default;
        }

        public InteriorTile( int x, int y, Chunk chunk, Map map )
            : base( x, y, chunk, map )
        {
            Info = new InteriorMaterialInfo( this );
        }

        public override float[] GetRawVertexData()
        {
            GameTile left =  Map.GetTile( X - 1, Y );
            GameTile right =  Map.GetTile( X - 1, Y );

            IsVisible = Info.IsVisible || !MapRenderer.CullHiddenFaces;
            UseHalfLighting = IsVisible && ( !IsDefault && IsWall ) && !( Map.GetTile( X, Y + 1 ).IsWall && Map.GetTile( X, Y - 1 ).IsWall );

            if( IsVisible )
                return Info.GetRawVertexData();
            else
                return new float[ 0 ];
        }
    }
}
