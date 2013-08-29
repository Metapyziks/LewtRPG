using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ResourceLib;

using Lewt.Shared.Rendering;
using Lewt.Shared.World;

namespace ChunkEditor
{
    class ConnectorArrow : Sprite
    {
        private static Texture stArrowTexture;

        private static Texture ArrowTexture
        {
            get
            {
                if( stArrowTexture == null )
                    stArrowTexture = Res.Get<Texture>( "images_gui_connector_arrow" );

                return stArrowTexture;
            }
        }

        public ChunkConnector ConnectorInfo;

        public ConnectorArrow( int x, int y, bool horizontal, int size = 1 )
            : base( ArrowTexture, MapRenderer.CameraScale )
        {
            ConnectorInfo = new ChunkConnector
            {
                X = x,
                Y = y,
                Size = size,
                Horizontal = horizontal,
                BottomOrRight = x != 0 && y != 0
            };

            UseCentreAsOrigin = true;

            if ( horizontal )
            {
                if ( x == 0 )
                    Rotation = 3.0f * (float) Math.PI / 2.0f;
                else
                    Rotation = (float) Math.PI / 2.0f;
            }
            else
            {
                if ( y == 0 )
                    Rotation = 0.0f;
                else
                    Rotation = (float) Math.PI;
            }
        }

        public override void Render()
        {
            X = MapRenderer.CameraScale * ( ( ConnectorInfo.X - MapRenderer.CameraX ) * 16.0f + 8.0f ) + MapRenderer.ScreenWidth / 2.0f;
            Y = MapRenderer.CameraScale * ( ( ConnectorInfo.Y - MapRenderer.CameraY ) * 16.0f + 8.0f ) + MapRenderer.ScreenHeight / 2.0f;
            for ( int i = 0; i < ConnectorInfo.Size; ++i )
            {
                base.Render();
                if ( ConnectorInfo.Horizontal )
                    Y += 16.0f * MapRenderer.CameraScale;
                else
                    X += 16.0f * MapRenderer.CameraScale;
            }
        }
    }
}
