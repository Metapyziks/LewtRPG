using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Client.UI;
using Lewt.Shared.World;
using Lewt.Client.Networking;
using OpenTK;
using OpenTK.Graphics;
using Lewt.Shared.Rendering;
using OpenTK.Input;

namespace Lewt
{
    public class DungeonSelectedEventArgs : EventArgs
    {
        public readonly Dungeon Dungeon;

        public DungeonSelectedEventArgs( Dungeon dungeon )
        {
            Dungeon = dungeon;
        }
    }

    public delegate void DungeonSelectedEventHandler( object sender, DungeonSelectedEventArgs e );

    class WorldMap : UIObject
    {
        private OverworldMap myMap;

        public event DungeonSelectedEventHandler DungeonSelected;

        public WorldMap( Vector2 size, OverworldMap map )
            : base( size )
        {
            myMap = map;

            Dictionary<UIButton, Dungeon> dungeonDict = new Dictionary<UIButton, Dungeon>();

            foreach ( Dungeon dungeon in GameClient.Overworld.Dungeons )
            {
                UIButton btn = new UIButton( new Vector2( 20.0f, 20.0f ) )
                {
                    Position = new Vector2( dungeon.X / map.ChunkWidth * 16.0f + 62.0f, dungeon.Y / map.ChunkHeight * 16.0f - 2.0f ),
                    Colour = new Color4( 241, 217, 169, 127 ),
                    DisabledColour = new Color4( 99, 46, 14, 127 )
                };

                btn.AddChild( new UISprite( dungeon.IconSprite ) { Position = new Vector2( -2.0f, -2.0f ) } );

                if ( dungeon.DungeonClass.Name != "fort" && dungeon.DungeonClass.Name != "temple" )
                    btn.Disable();

                dungeonDict.Add( btn, dungeon );

                btn.Click += delegate( object sender, MouseButtonEventArgs e )
                {
                    if ( DungeonSelected != null )
                        DungeonSelected( this, new DungeonSelectedEventArgs( dungeonDict[ btn ] ) );
                };

                AddChild( btn );
            }
        }

        protected override void OnRender( Vector2 renderPosition = new Vector2() )
        {
            SpriteRenderer.End();

            MapRenderer.CameraX = 16.0f;
            MapRenderer.CameraY = 16.0f;

            myMap.RenderTiles();

            SpriteRenderer.Begin();
            
            base.OnRender( renderPosition );
        }
    }
}
