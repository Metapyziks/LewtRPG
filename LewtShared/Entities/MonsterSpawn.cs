using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Lewt.Shared.Rendering;
using Lewt.Shared.World;

using ResourceLib;

namespace Lewt.Shared.Entities
{
    [PlaceableInEditor]
    public class MonsterSpawn : Entity
    {
        private static Texture stTex;

        private Sprite mySprite;

        public MonsterSpawn()
        {

        }

        public MonsterSpawn( MonsterSpawn copy )
            : base( copy )
        {

        }

        public MonsterSpawn( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {

        }

        protected override void InitializeGraphics()
        {
            if ( stTex == null )
                stTex = Res.Get<Texture>( "images_gui_skull" );

            mySprite = new Sprite( stTex, MapRenderer.CameraScale )
            {
                UseCentreAsOrigin = true
            };

            base.InitializeGraphics();
        }

        public override void PostWorldInitialize( bool editor = false )
        {
            if ( editor )
                return;

            if ( IsServer )
            {
                Character chara = Scripts.CreateInstance( "Scripts.Entities.Skeleton" ) as Character;
                chara.OriginX = OriginX;
                chara.OriginY = OriginY;

                Map.AddEntity( chara );
            }

            Remove();
        }

        public override void EditorRender()
        {
            base.EditorRender();

            if ( Selected )
                SpriteRenderer.DrawRect(
                    ScreenX - (float) Width * MapRenderer.CameraScale * 8.0f,
                    ScreenY - (float) Height * MapRenderer.CameraScale * 8.0f,
                    (float) Width * MapRenderer.CameraScale * 16.0f,
                    (float) Height * MapRenderer.CameraScale * 16.0f,
                    new Color4( 0, 255, 0, 63 ) );

            mySprite.X = ScreenX;
            mySprite.Y = ScreenY;

            mySprite.Render();
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );
        }
    }
}
