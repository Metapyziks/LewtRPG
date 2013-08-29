using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Rendering;

namespace Lewt.Shared.World
{
    public class ExteriorTile : GameTile
    {
        private static readonly ExteriorTile stDefault = new ExteriorTile();

        public static new ExteriorTile Default
        {
            get
            {
                return stDefault;
            }
        }

        public int WallHeight
        {
            get
            {
                return ( Info as ExteriorMaterialInfo ).WallHeight;
            }
            set
            {
                ( Info as ExteriorMaterialInfo ).WallHeight = value;
            }
        }

        private ExteriorTile()
        {
            Info = ExteriorMaterialInfo.Default;
        }

        public ExteriorTile( int x, int y, Chunk chunk, Map map )
            : base( x, y, chunk, map )
        {
            Info = new ExteriorMaterialInfo( this );
        }

        public override float[] GetRawVertexData()
        {
            IsVisible = Info.IsVisible || !MapRenderer.CullHiddenFaces;
            UseHalfLighting = false;

            if ( IsVisible )
                return Info.GetRawVertexData();
            else
                return new float[ 0 ];
        }
    }
}
